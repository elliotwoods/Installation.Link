#define MONO_WRITE
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.SlimDX;
using SlimDX.Direct3D9;

using SlimDX;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

using CLEyeMulticam;

using NAudio;
using NAudio.CoreAudioApi;

namespace VVVV.Nodes.CLEye
{
    [PluginInfo(Name = "DK-Recorder",
                Category = "CLEye",
                Author = "sugokuGENKI",
                Version = "",
                Help = "Save and play images from camera devices.",
                Credits = "vux, Eric D. Burdo",
                Tags = "")]
	public unsafe class DKRecorderNode : DXTextureOutPluginBase, IPluginEvaluate
    {
		#region Fields

        //inputs
        [Input("Record", IsSingle=true)]
        IDiffSpread<bool> FPinInRecord;

        [Input("Preview", IsSingle=true)]
        IDiffSpread<bool> FPinInPreview;

        [Input("Save", IsSingle=true, IsBang=true)]
        IDiffSpread<bool> FPinInSave;

        [Input("fps", IsSingle=true, MinValue=1, MaxValue=120, DefaultValue=30)]
        IDiffSpread<int> FPinInFPS;

        [Input("Duration", IsSingle = true, MinValue = 0, MaxValue = 360, DefaultValue = 4)]
        IDiffSpread<double> FPinInDuration;

        [Input("Path")]
        IDiffSpread<string> FPinInPath;

        [Input("CameraGUID")]
        IDiffSpread<string> FPinInGUID;

        [Input("Resolution", MinValue=0, MaxValue=2048, DefaultValue=256)]
        IDiffSpread<int> FPinInResolution;

#if MONO_WRITE
        [Input("Audio Device", IsSingle = true)]
        ISpread<int> FPinInAudioDevice;
#else
        [Input("Audio Device", IsSingle = true)]
        ISpread<MMDevice> FPinInAudioDevice;
#endif
        //outputs
        // texture output is automatic
        [Output("Progress", DefaultValue=0)]
        ISpread<double> FPinOutProgress;

        [Output("Audio Level", DefaultValue = 0, IsSingle = true)]
        ISpread<float> FPinOutAudioLevel;

        [Output("Recorded", DefaultValue=0)]
        ISpread<bool> FPinOutRecorded;

        [Output("Saved", DefaultValue=0)]
        ISpread<bool> FPinOutSaved;

        IPluginHost FHost;

        //audio
        AudioRecord FAudio;

        bool firstRun = true;
		private Dictionary<int, Texture> FTextures = new Dictionary<int, Texture>();

        //recorded images
        private List<IntPtr>     FRecordedData = new List<IntPtr>();
        private int              resolution;
        private double           FRecordProgress = 0;

        //camera
        CLEyeCameraDevice FCam = new CLEyeCameraDevice(CLEyeCameraResolution.CLEYE_QVGA,
            CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED, 30);
        Guid        FGuid;
        bool        FCameraCreated = false;
        IntPtr      FCamData = new IntPtr();
        IntPtr      FCamDataResized = new IntPtr();

        //threads
        Object      FLockPixels = new Object();
        Thread      FCaptureThread;
        Thread      FSaveThread;
        bool        FIsClosing = false;

        //current values
        int iRecordFrame=0;
        double iPlayFrame = 0;
        bool isRecorded = false;
        bool isSaved = false;
        DateTime    timeStartRecord;
        DateTime    timeLastFrame = DateTime.Now;

        // settings
        int countTarget;
        double Ffps = 30;
        string FPath;
        string FPatchPath;
        int FInputWidth = 320;
        int FInputHeight = 240;

        enum DK_state { Preview, Recording, Playing };
        DK_state currentState = DK_state.Preview;
        
        //debug
        string FDebugString = "";

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		#endregion

		#region Auto Evaluate
		public bool AutoEvaluate
		{
			get { return true; }
		}
		#endregion

        #region Constructor
        [ImportingConstructor()]
        public DKRecorderNode(IPluginHost host)
            : base(host)
        {
            resolution = 256;
            //assign host
            this.FHost = host;

            FHost.GetHostPath(out FPatchPath);
            FPatchPath = Path.GetDirectoryName(FPatchPath);

            FAudio = new AudioRecord();
        }

        ~DKRecorderNode()
        {
            FIsClosing = true;
            if (FCaptureThread != null)
                FCaptureThread.Join(200);
            
            closeCamera();

            if (FSaveThread != null)
                FSaveThread.Join(600);

        }
        #endregion

        #region Configurate
        public void Configurate(IPluginConfig Input)
		{

		}
		#endregion

		#region Evaluate
		public void Evaluate(int SpreadMax)
		{
            if (FPinInAudioDevice[0] >= 0 && FPinInAudioDevice.SliceCount == 1)
            {
				if (FPinInAudioDevice[0] != FAudio.getDeviceID())
				{
					FAudio.setDevice(FPinInAudioDevice[0]);
					FAudio.setTempPath(FPatchPath + "\\" + FPinInAudioDevice[0].ToString() + ".wav");
				}
            }

            if (this.FPinInResolution.IsChanged || firstRun)
            {
                //
                //CHANGE RESOLUTION
                //

                lock (FLockPixels)
                {
                    //read in the resolution
                    resolution = FPinInResolution[0];

                    //clear all existing images
                    clearImages();

                    //grow array with new resolution
                    while (countTarget > FRecordedData.Count)
                    {
                        FRecordedData.Add(new IntPtr());
                        FRecordedData[FRecordedData.Count - 1] = Marshal.AllocCoTaskMem(resolution * resolution * 4);
                    }
                    Reinitialize();
                }
            }

            if (this.FPinInDuration.IsChanged || firstRun)
            {
                //
                //CHANGE COUNT
                //

                //read in the count
                countTarget = (int)(FPinInDuration[0] * FPinInFPS[0]);
                
                //change the amount of data that we have stored
                //
                //grow array
                while (countTarget > FRecordedData.Count)
                {
                    FRecordedData.Add(new IntPtr());
                    FRecordedData[FRecordedData.Count - 1] = Marshal.AllocCoTaskMem(resolution * resolution * 4);
                }
                //
                //shrink array
                while (countTarget < FRecordedData.Count)
                {
                    Marshal.FreeCoTaskMem(FRecordedData[FRecordedData.Count - 1]);
                    FRecordedData.RemoveAt(FRecordedData.Count - 1);
                }
            }

            if (this.FPinInFPS.IsChanged || firstRun)
            {
                //
                // SET FPS
                //
                Ffps = FPinInFPS[0];
                countTarget = (int)(FPinInDuration[0] * FPinInFPS[0]);
            }

            if (this.FPinInPath.IsChanged || firstRun)
            {
                //
                // CHANGE PATH
                //

                //read in path
                FPath = FPinInPath[0];

                //check if folder exists
                //if not create it
                if (!Directory.Exists(getFullPath()) && FPath != null)
                    Directory.CreateDirectory(getFullPath());

                FAudio.setFilename(getFullPath() + "\\audio.wav");
            }

            if (this.FPinInGUID.IsChanged && this.FPinInGUID.SliceCount > 0 || firstRun)
            {
                //
                // CHANGE GUID
                //

                //read in the GUID
                string strGUID = FPinInGUID[0];

                if (strGUID != null && strGUID.Length > 5)
                {
                    FGuid = new Guid(strGUID);
                    initCamera();
                }
            }

            if (this.FPinInRecord.IsChanged || firstRun)
            {
                //
                // DO RECORD
                //
                bool doRecord = FPinInRecord[0];

                if (doRecord)
                {
                    if (currentState != DK_state.Recording)
                    {
                        FAudio.StartRecording();
                        iRecordFrame = 0;
                        isRecorded = false;
                        isSaved = false;
                        currentState = DK_state.Recording;
                        timeStartRecord = DateTime.Now;
                        FPinOutRecorded[0] = false;
                        FPinOutProgress[0] = 0;
                        FDebugString = "";

                        FCam.setLED(true);
                    }
                    else
                        FDebugString = "Can't start recording, already recording";
                }
            }

            if (FPinInPreview.IsChanged || firstRun)
            {
                bool doPreview = FPinInPreview[0];

                if (doPreview)
                {
                    currentState = DK_state.Preview;
                    isRecorded = false;
                    isSaved = false;
                }
            }

            if (FPinInSave.IsChanged || firstRun)
            {
                bool doSave = FPinInSave[0];
                if (doSave && isRecorded && currentState == DK_state.Playing && FSaveThread == null)
                {
                    isSaved = false; //change here
                    FSaveThread = new Thread(fnSaveThread);
                    FSaveThread.Start();
                }
            }

            //check save thread is finished
            if (FSaveThread != null)
            {
                if (FSaveThread.IsAlive && isSaved)
                {
                    FSaveThread.Join();
                    FSaveThread = null;
                } else if (!FSaveThread.IsAlive) //change here
                    FSaveThread = null;
            }

            //output currents
            FPinOutProgress[0] = FRecordProgress;
            FPinOutRecorded[0] = isRecorded;
            FPinOutSaved[0] = isSaved;
            FPinOutAudioLevel[0] = FAudio.GetLevel();

            //output debug
            if (FDebugString.Length > 0)
                FHost.Log(TLogType.Message, FDebugString);
            FDebugString = "";

            //progess the play head
            iPlayFrame += (DateTime.Now - timeLastFrame).TotalSeconds * Ffps;
            timeLastFrame = DateTime.Now;
            if (countTarget > 0)
            {
                if (iPlayFrame > countTarget - 1)
                    iPlayFrame = 0;
            } else
                iPlayFrame = 0;

            Update();

            firstRun = false;
		}
        #endregion

        #region Dispose
        public void Dispose()
		{
            closeCamera();
		}
		#endregion

        #region Texture
		//this method gets called, when Reinitialize() was called in evaluate,
		//or a graphics device asks for its data
		protected override Texture CreateTexture(int Slice, Device device)
		{
			//FLogger.Log(LogType.Debug, "Creating new texture at slice: " + Slice);
            return TextureUtils.CreateTexture(device, resolution, resolution);
		}

        //this method gets called, when Update() was called in evaluate,
		//or a graphics device asks for its texture, here you fill the texture with the actual data
		//this is called for each renderer, careful here with multiscreen setups, in that case
		//calculate the pixels in evaluate and just copy the data to the device texture here
		protected unsafe override void UpdateTexture(int Slice, Texture texture)
		{
            if (FCameraCreated)
            {
                Surface srf = texture.GetSurfaceLevel(0);
                DataRectangle rect = srf.LockRectangle(LockFlags.Discard);

                //make sure we're not going over
                //should never reach this anyway
                while (iPlayFrame > countTarget - 1)
                    iPlayFrame = 0;

                if (currentState == DK_state.Playing)
                    rect.Data.WriteRange(FRecordedData[System.Convert.ToInt32(iPlayFrame)], resolution * resolution * 4);
                else
                    rect.Data.WriteRange(this.FCamDataResized, resolution * resolution * 4);

                srf.UnlockRectangle();
            }
		}

        #endregion

        private bool isImageFile(string filename)
        {
            if (filename == null)
                return false;
            bool hasRightExtension = Path.GetExtension(filename).Equals(".jpg", StringComparison.InvariantCultureIgnoreCase)
                 || Path.GetExtension(filename).Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase)
                 || Path.GetExtension(filename).Equals(".png", StringComparison.InvariantCultureIgnoreCase)
                 || Path.GetExtension(filename).Equals(".bmp", StringComparison.InvariantCultureIgnoreCase)
                 || Path.GetExtension(filename).Equals(".gif", StringComparison.InvariantCultureIgnoreCase);

            //we've definitely got a dud, so let's quit before we annoy TryParse
            if (!hasRightExtension)
                return false;

            string trunkname = Path.GetFileNameWithoutExtension(filename);
            int testIsNum;
            bool isSequence = int.TryParse(trunkname.Substring(trunkname.Length - 1, 1), out testIsNum);

            return hasRightExtension && File.Exists(filename) && isSequence;
        }

        private void clearImages()
        {
            int lastIndex;
            while (FRecordedData.Count > 0)
            {
                lastIndex = FRecordedData.Count - 1;
                Marshal.FreeCoTaskMem(FRecordedData[lastIndex]);
                FRecordedData.RemoveAt(lastIndex);
            }
            isRecorded = false;
            GC.Collect();
        }

        #region camera
        private void initCamera()
        {
            //close camera if it's already created
            closeCamera();

            //start camera
            FCam.Start(FGuid);

            //create memory for image
            FCamData = Marshal.AllocCoTaskMem(FInputWidth * FInputHeight * 4);
            FCamDataResized = Marshal.AllocCoTaskMem(resolution * resolution * 4);
            FCameraCreated = true;

            FCaptureThread = new Thread(getFrameThread);
            FCaptureThread.Start();
        }

        private void closeCamera()
        {
            if (FCameraCreated)
            {
                FCameraCreated = false;
                FCaptureThread.Join(100);

                Marshal.FreeCoTaskMem(FCamData);
                Marshal.FreeCoTaskMem(FCamDataResized);
                FCam.Stop();
            }
        }
        #endregion

        private void getFrameThread()
        {
            while (FCameraCreated && !FIsClosing)
            {
                if (!FCam.getPixels(FCamData, 100))
                    FDebugString = "Can't pull frame from camera";

                lock (FLockPixels)
                {
                    resizeImage(FCamData, FCamDataResized, FInputWidth, FInputHeight, resolution, resolution);

                    if (currentState == DK_state.Recording)
                    {
                        int calcTimeFrame = System.Convert.ToInt32((DateTime.Now - timeStartRecord).TotalSeconds * Ffps);

                        while (iRecordFrame < calcTimeFrame && iRecordFrame < countTarget)
                        {

                            CopyMemory(FRecordedData[iRecordFrame], FCamDataResized, System.Convert.ToUInt32(resolution * resolution * 4));
                            FDebugString += iRecordFrame.ToString() + "\n";
                            iRecordFrame++;
                        }

                        //update progress
                        FRecordProgress = System.Convert.ToDouble(iRecordFrame) / System.Convert.ToDouble(countTarget);


                        if (iRecordFrame == countTarget)
                        {
                            //we've got to the end of recording
                            isRecorded = true;
                            FAudio.StopRecording();
                            FRecordProgress = 1.0;
                            currentState = DK_state.Playing;
                            FCam.setLED(false);
                            iPlayFrame = 0;
                        }
                    }
                }
            }
        }

        private unsafe void resizeImage(IntPtr source, IntPtr destination, int sw, int sh, int destw, int desth)
        {
            //crappy nearest neighbour resizing
            int sourcex, sourcey;

            byte* byteSource = (byte*)source.ToPointer();
            byte* byteDestination= (byte*)destination.ToPointer();

            int idxsource, idxdest;
            for (int y = 0; y < desth; y++)
                for (int x = 0; x < destw; x++)
                {
                    sourcex = System.Convert.ToInt32(System.Convert.ToSingle(x * sw) / System.Convert.ToSingle(destw));
                    sourcey = System.Convert.ToInt32(System.Convert.ToSingle(y * sh) / System.Convert.ToSingle(desth));

                    idxsource = sourcex*4 + sourcey*4*sw;
                    idxdest = x * 4 + y * 4 * destw;

                    byteDestination[idxdest + 0] = byteSource[idxsource + 0];
                    byteDestination[idxdest + 1] = byteSource[idxsource + 1];
                    byteDestination[idxdest + 2] = byteSource[idxsource + 2];
                    byteDestination[idxdest + 3] = 255; // alpha
                }
        }

        string getFullPath()
        {
            return FPatchPath + "\\" + FPath; 
        }

        private void fnSaveThread()
        {
            //check if we haven't recorded (then cant save!)
            if (!isRecorded || FPath == null)
                return;

            String fullPath = getFullPath();

            /*
             * Let's overwrite existing stuff
             * everything should have a unique path anyway
             * 
            //check if folder exists
            //if not create it
            while (Directory.Exists(fullPath))
                fullPath = fullPath + "0";

            Directory.CreateDirectory(fullPath);
            */
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
            

            Bitmap bmpSaver;
            //loop images
            string padString;
            for (int i = 0; i < countTarget; i++)
            {
                padString = i.ToString();
                while (padString.Length < 4)
                    padString = "0" + padString;

                bmpSaver = new Bitmap(resolution, resolution, resolution*4, PixelFormat.Format32bppRgb, FRecordedData[i]);
                bmpSaver.Save(fullPath + "\\" + padString + ".png");
                bmpSaver.Dispose();
                Thread.Sleep(10);
            }

            FAudio.SaveRecording();

            GC.Collect();

            isSaved = true;
            
            
        }
    }
        
}
