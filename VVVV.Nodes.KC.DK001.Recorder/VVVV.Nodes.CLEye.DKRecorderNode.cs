using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V1;
using SlimDX.Direct3D9;
using System.Runtime.InteropServices;
using SlimDX;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using CLEyeMulticam;

namespace VVVV.Nodes.CLEye
{
	
	public unsafe class DKRecorderNode : IPlugin, IDisposable, IPluginDXTexture
	{

		#region Plugin Info
		public static IPluginInfo PluginInfo
		{
			get
			{
				IPluginInfo Info = new PluginInfo();
				Info.Name = "DK-Recorder";							//use CamelCaps and no spaces
                Info.Category = "CLEye";						//try to use an existing one
				Info.Version = "";						//versions are optional. leave blank if not needed
				Info.Help = "Save and play images from camera devices.";
				Info.Bugs = "";
				Info.Credits = "vux, Eric D. Burdo";								//give credits to thirdparty code used
				Info.Warnings = "";
                Info.Author = "sugokuGENKI";
				Info.Credits = "";

				//leave below as is
				System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
				System.Diagnostics.StackFrame sf = st.GetFrame(0);
				System.Reflection.MethodBase method = sf.GetMethod();
				Info.Namespace = method.DeclaringType.Namespace;
				Info.Class = method.DeclaringType.Name;
				return Info;
				//leave above as is
			}
		}
		#endregion

		#region Fields
		private IPluginHost FHost;

        //inputs
		private IValueIn FPinInRecord;
        private IValueIn FPinInPreview;
        private IValueIn FPinInSave;
        private IValueIn FPinInFPS;
        private IValueIn FPinInCount;
        //
        private IStringIn FPinInPath;
        private IStringIn FPinInGUID;
        private IValueIn FPinInResolution;

        //outputs
        private IDXTextureOut FPinOutTexture;
        //
        private IValueOut FPinOutProgress;
        private IValueOut FPinOutRecorded;
        private IValueOut FPinOutSaved;

		private Dictionary<int, Texture> FTextures = new Dictionary<int, Texture>();

        //recorded images
        private List<IntPtr>     FRecordedData = new List<IntPtr>();
        private int              resolution;
        private double           FRecordProgress = 0;

        //camera
        CLEyeCameraDevice FCam = new CLEyeCameraDevice();
        Guid        FGuid;
        bool        FCameraCreated = false;
        IntPtr      FCamData = new IntPtr();
        IntPtr      FCamDataResized = new IntPtr();

        //threads
        Thread      FCaptureThread;
        Thread      FSaveThread;

        //current values
        int iRecordFrame=0;
        double iPlayFrame = 0;
        bool isRecorded = false;
        bool isSaved = false;
        DateTime    timeStartRecord;
        DateTime    timeLastFrame = DateTime.Now;

        // settings
        int countTarget;
        double Ffps = 30.0;
        string FPath;
        string FPatchPath;

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

		#region Set Plugin Host
		public void SetPluginHost(IPluginHost Host)
		{
			//assign host
			this.FHost = Host;

            //inputs
            this.FHost.CreateValueInput("Record", 1, null, TSliceMode.Single, TPinVisibility.True, out FPinInRecord);
            FPinInRecord.SetSubType(0, 1, 1, 0, true, false, false);

            this.FHost.CreateValueInput("Preview", 1, null, TSliceMode.Single, TPinVisibility.True, out FPinInPreview);
            FPinInPreview.SetSubType(0, 1, 1, 0, true, false, false);

            this.FHost.CreateValueInput("Save", 1, null, TSliceMode.Single, TPinVisibility.True, out FPinInSave);
            FPinInSave.SetSubType(0, 1, 1, 0, true, false, false);

            this.FHost.CreateValueInput("fps", 1, null, TSliceMode.Single, TPinVisibility.True, out FPinInFPS);
            FPinInFPS.SetSubType(0, 120, 1, 30, false, false, true);

            this.FHost.CreateValueInput("Count", 1, null, TSliceMode.Single, TPinVisibility.True, out FPinInCount);
            FPinInCount.SetSubType(0, double.MaxValue, 1, 120, false, false, true);

            this.FHost.CreateStringInput("Path", TSliceMode.Single, TPinVisibility.True, out FPinInPath);
            FPinInPath.SetSubType(".", false);

            this.FHost.CreateStringInput("CameraGUID", TSliceMode.Single, TPinVisibility.True, out FPinInGUID);

            this.FHost.CreateValueInput("Resolution", 1, null, TSliceMode.Single, TPinVisibility.True, out FPinInResolution);
            FPinInResolution.SetSubType(0, 2048, 1, 256, false, false, true);

            //outputs
            this.FHost.CreateTextureOutput("Texture", TSliceMode.Single, TPinVisibility.True, out this.FPinOutTexture);

            this.FHost.CreateValueOutput("Progress", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinOutProgress);
            FPinOutProgress.SetSubType(0, 1 , 0.001, 0, false, false, false);

            this.FHost.CreateValueOutput("Recorded", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinOutRecorded);
            FPinOutRecorded.SetSubType(0, 1, 1, 0, false, true, false);

            this.FHost.CreateValueOutput("Saved", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinOutSaved);
            FPinOutSaved.SetSubType(0, 1, 1, 0, false, true, false);

            FHost.GetHostPath(out FPatchPath);
            FPatchPath = Path.GetDirectoryName(FPatchPath);
            

		}
		#endregion

        #region Constructor
        public DKRecorderNode()
        {
            resolution = 256;
        }

        ~DKRecorderNode()
        {
            closeCamera();
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
            if (this.FPinInResolution.PinIsChanged)
            {
                //
                //CHANGE RESOLUTION
                //

                //read in the resolution
                double dblResolution;
                FPinInResolution.GetValue(0, out dblResolution);
                resolution = System.Convert.ToInt32(dblResolution);

                //clear all existing images
                clearImages();

                //grow array with new resolution
                while (countTarget > FRecordedData.Count)
                {
                    FRecordedData.Add(new IntPtr());
                    FRecordedData[FRecordedData.Count - 1] = Marshal.AllocCoTaskMem(resolution * resolution * 4);
                }
            }

            if (this.FPinInCount.PinIsChanged)
            {
                //
                //CHANGE COUNT
                //

                //read in the count
                double dblCount;
                FPinInCount.GetValue(0, out dblCount);
                countTarget = System.Convert.ToInt32(dblCount);
                
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

            if (this.FPinInFPS.PinIsChanged)
            {
                //
                // SET FPS
                //
                FPinInFPS.GetValue(0, out Ffps);
            }

            if (this.FPinInPath.PinIsChanged)
            {
                //
                // CHANGE PATH
                //

                //read in path
                FPinInPath.GetString(0, out FPath);

                //check if folder exists
                //if not create it
                if (!Directory.Exists(FPath) && FPath != null)
                    Directory.CreateDirectory(FPath);
            }

            if (this.FPinInGUID.PinIsChanged)
            {
                //
                // CHANGE GUID
                //

                //read in the GUID
                string strGUID;
                FPinInGUID.GetString(0, out strGUID);

                if (strGUID != null && strGUID.Length > 5)
                    FGuid = new Guid(strGUID);

                initCamera();
            }

            if (this.FPinInRecord.PinIsChanged)
            {
                //
                // DO RECORD
                //

                double dblRecord;
                FPinInRecord.GetValue(0, out dblRecord);

                if (dblRecord > 0.5)
                {
                    if (currentState != DK_state.Recording)
                    {
                        iRecordFrame = 0;
                        isRecorded = false;
                        isSaved = false;
                        currentState = DK_state.Recording;
                        timeStartRecord = DateTime.Now;
                        FPinOutRecorded.SetValue(0, 0);
                        FPinOutProgress.SetValue(0, 0);
                        FDebugString="";
                    }
                }
            }

            if (FPinInPreview.PinIsChanged)
            {
                double dblPreview;
                FPinInPreview.GetValue(0, out dblPreview);
                if (dblPreview > 0.5)
                {
                    currentState = DK_state.Preview;
                    isRecorded = false;
                    isSaved = false;
                }
            }

            if (FPinInSave.PinIsChanged)
            {
                double dblSave;
                FPinInSave.GetValue(0, out dblSave);
                if (dblSave > 0.5 && isRecorded && currentState == DK_state.Playing && FSaveThread==null)
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
            FPinOutProgress.SetValue(0, FRecordProgress);
            FPinOutRecorded.SetValue(0, (isRecorded ? 1 : 0));
            FPinOutSaved.SetValue(0, (isSaved ? 1 : 0));

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

		}
        #endregion

        #region Dispose
        public void Dispose()
		{
            closeCamera();
		}
		#endregion

		public void GetTexture(IDXTextureOut ForPin, int OnDevice, out int tex)
		{
			tex = 0;
			if (ForPin == this.FPinOutTexture)
			{	
				if (this.FTextures.ContainsKey(OnDevice))
				{
					tex = this.FTextures[OnDevice].ComPointer.ToInt32();
				}
			}

		}

		public void DestroyResource(IPluginOut ForPin, int OnDevice, bool OnlyUnManaged)
		{
			if (this.FTextures.ContainsKey(OnDevice))
			{
				this.FTextures[OnDevice].Dispose();
				this.FTextures.Remove(OnDevice);
			}
		}

		public void UpdateResource(IPluginOut ForPin, int OnDevice)
		{
			Device dev = Device.FromPointer(new IntPtr(OnDevice));
			if (FCameraCreated)
			{
				if (!this.FTextures.ContainsKey(OnDevice))
				{
					Texture txt = new Texture(dev, resolution, resolution, 1, Usage.None, Format.X8R8G8B8, Pool.Managed);
					this.FTextures.Add(OnDevice, txt);
				}

				Texture tx = this.FTextures[OnDevice];
				
				Surface srf = tx.GetSurfaceLevel(0);
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

        private void initCamera()
        {
            //close camera if it's already created
            closeCamera();

            //start camera
            FCam.Start(FGuid);

            //create memory for image
            FCamData = Marshal.AllocCoTaskMem(640*480*4);
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

        private void getFrameThread()
        {
            while (FCameraCreated)
            {
                FCam.getPixels(FCamData, 100);
                resizeImage(FCamData, FCamDataResized, 640, 480, resolution, resolution);

                if (currentState == DK_state.Recording)
                {
                    int calcTimeFrame = System.Convert.ToInt32((DateTime.Now - timeStartRecord).TotalSeconds * Ffps);

                    while (iRecordFrame < calcTimeFrame && iRecordFrame < countTarget)
                    {
                        
                        CopyMemory(FRecordedData[iRecordFrame], FCamDataResized, System.Convert.ToUInt32(resolution*resolution*4));
                        FDebugString += iRecordFrame.ToString() + "\n";
                        iRecordFrame++;
                    }

                    //update progress
                    FRecordProgress = System.Convert.ToDouble(iRecordFrame) / System.Convert.ToDouble(countTarget);
                    

                    if (iRecordFrame == countTarget)
                    {
                        //we've got to the end of recording
                        isRecorded = true;
                        FRecordProgress = 1.0;
                        currentState = DK_state.Playing;
                        iPlayFrame = 0;
                    }
                }
            }
        }

        private void resizeImage(IntPtr source, IntPtr destination, int sw, int sh, int destw, int desth)
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
                    byteDestination[idxdest + 3] = byteSource[idxsource + 3];
                }
        }

        private void fnSaveThread()
        {
            //check if we haven't recorded
            //should never need this
            if (!isRecorded)
                return;

            Bitmap bmpSaver;
            //loop images
            for (int i = 0; i < countTarget; i++)
            {
                bmpSaver = new Bitmap(resolution, resolution, resolution*4, PixelFormat.Format32bppRgb, FRecordedData[i]);
                bmpSaver.Save(FPatchPath + "\\" + FPath + "\\" + i.ToString() + ".png");
                bmpSaver.Dispose();
            }

            isSaved = true;
            
            
        }
    }
        
}
