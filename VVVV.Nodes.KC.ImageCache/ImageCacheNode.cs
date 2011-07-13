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

namespace VVVV.Nodes.ImageCache
{
	
	public unsafe class ImageCacheNode : IPlugin, IDisposable, IPluginDXTexture
	{

		#region Plugin Info
		public static IPluginInfo PluginInfo
		{
			get
			{
				IPluginInfo Info = new PluginInfo();
				Info.Name = "ImageCache";							//use CamelCaps and no spaces
				Info.Category = "EX9.Texture";						//try to use an existing one
				Info.Version = "";						//versions are optional. leave blank if not needed
				Info.Help = "Cache images into RAM for quick GPU access.";
				Info.Bugs = "";
				Info.Credits = "";								//give credits to thirdparty code used
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

        private IValueConfig FConfigAllocated;

		private IValueIn FPinInIndex;
        private IStringIn FPinFilename;

        private IValueOut FPinOutCount;
        private IValueOut FPinOutLoaded;


		private IDXTextureOut FPinOutTexture;

		private Dictionary<int, Texture> FTextures = new Dictionary<int, Texture>();

        //loaded images
        private List<IntPtr>    FData = new List<IntPtr>();
        private int FWidth, FHeight;
        private int             FWidthAllocated=0, FHeightAllocated=0;
        private bool            isValidIamge = false;
        private int             nSlots=200;

        //filename properties
        private string  fnameTrunk;

        //load thread
        private Thread  loadThread;
        bool            isLoading = false;
        string          fnameFirst ="";

        //current values
        int idx=0;
        int count=0;

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

            //config
            this.FHost.CreateValueConfig("Allocated", 1, null, TSliceMode.Single, TPinVisibility.True, out FConfigAllocated);
            FConfigAllocated.SetSubType(1, 1e4, 1, 200, false, false, true);

			//inputs
			this.FHost.CreateValueInput("Index", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinInIndex);
            this.FPinInIndex.SetSubType(0, double.MaxValue, 1, 0, false, false, true);

            this.FHost.CreateStringInput("Filename", TSliceMode.Single, TPinVisibility.True, out FPinFilename) ;
            this.FPinFilename.SetSubType("", true);

            //outputs
            this.FHost.CreateTextureOutput("Texture", TSliceMode.Single, TPinVisibility.True, out this.FPinOutTexture);
            
            this.FHost.CreateValueOutput("Count", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinOutCount);
            FPinOutCount.SetSubType(0, double.MaxValue, 1, 0, false, false, true);

            this.FHost.CreateValueOutput("Loaded", 1, null, TSliceMode.Single, TPinVisibility.True, out this.FPinOutLoaded);
            FPinOutLoaded.SetSubType(0, 1, 1, 0, false, true, false);
		}
		#endregion

        #region Constructor
        public ImageCacheNode()
        {
            FWidth = 0;
            FHeight = 0;
        }
        ~ImageCacheNode()
        {
            nSlots = 0;
            Allocate();
        }
        #endregion

        #region Configurate
        public void Configurate(IPluginConfig Input)
		{
            double dblSlots;
            FConfigAllocated.GetValue(0, out dblSlots);
            nSlots = System.Convert.ToInt32(dblSlots);
            Allocate();
        }
        
		#endregion

        private void Allocate()
        {
            bool doGC = false;
            //check we've got right size allocated
            if (FWidth != FWidthAllocated || FHeight != FHeightAllocated)
                while (FData.Count > 0)
                {
                    Marshal.FreeCoTaskMem(FData[FData.Count - 1]);
                    FData.RemoveAt(FData.Count - 1);
                    doGC = true;
                }

            //shrink array
            while (FData.Count > nSlots)
            {
                Marshal.FreeCoTaskMem(FData[FData.Count - 1]);
                FData.RemoveAt(FData.Count - 1);
                doGC = true;
            }

            //grow array
            while (FData.Count < nSlots)
            {
                FData.Add(new IntPtr());
                FData[FData.Count - 1] = Marshal.AllocCoTaskMem(FWidth * FHeight * 4);
            }

            FWidthAllocated = FWidth;
            FHeightAllocated = FHeight;

            //do a collection if we deleted anything. necessary?
            if (doGC)
                GC.Collect();
        }

        
		#region Evaluate
		public void Evaluate(int SpreadMax)
		{

            if (this.FPinInIndex.PinIsChanged)
            {
                double dblIndex;
                FPinInIndex.GetValue(0, out dblIndex);
                if (dblIndex >= 0 && dblIndex < 1e6)
                {
                    idx = System.Convert.ToInt32(dblIndex);

                    while (idx < 0)
                        idx += count;

                    if (count == 0)
                        idx = 0;
                    else
                        idx %= count;
                }
            }


            if (this.FPinFilename.PinIsChanged)
            {
                string fnamein;
                FPinFilename.GetString(0, out fnamein);

                isLoading = true;
                fnameFirst = fnamein;

                if (isImageFile(fnameFirst))
                {
                    if (loadThread == null)
                    {
                        loadThread = new Thread(fnLoadThread);
                        loadThread.Start();
                    }
                    else
                    {
                        if (loadThread.IsAlive)
                            loadThread.Join();
                        loadThread = new Thread(fnLoadThread);
                        loadThread.Start();
                    }
                    
                    FPinOutLoaded.SetValue(0, 0);
                }
            }

            if (!isLoading && loadThread.IsAlive)
            {
                loadThread.Join();
            }

            double dblCount = System.Convert.ToInt32(count);
            FPinOutCount.SetValue(0, dblCount);

            FPinOutLoaded.SetValue(0, (isLoading ? 0 : 1));
		}
        #endregion

        #region Dispose
        public void Dispose()
		{
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
			if (isValidIamge && idx < count-1)
			{
				if (!this.FTextures.ContainsKey(OnDevice))
				{
					Texture txt = new Texture(dev, FWidth, FHeight, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
					this.FTextures.Add(OnDevice, txt);
				}

				Texture tx = this.FTextures[OnDevice];
				
				Surface srf = tx.GetSurfaceLevel(0);
				DataRectangle rect = srf.LockRectangle(LockFlags.Discard);
				rect.Data.WriteRange(this.FData[idx], FWidth*FHeight*4);
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

        private bool loadImage(int iFrame, string filename)
        {
            //check we've allocated enough. if not let's end here
            if (iFrame > FData.Count - 1)
                return false;

            Bitmap ImageLoader = new Bitmap(filename);
            
            //check if same format as trunk image
            if (ImageLoader.Height != FHeight || ImageLoader.Width != FWidth)
                return false;

            BitmapData ImageData = ImageLoader.LockBits(new Rectangle(0, 0, FWidth, FHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            CopyMemory(FData[iFrame], ImageData.Scan0, System.Convert.ToUInt32(FWidth * FHeight * 4));
            ImageLoader.Dispose();

            return true;
        }

        private void fnLoadThread(object input)
        {
            if (isImageFile(fnameFirst))
                {
                    Bitmap bmpCheck = new Bitmap(fnameFirst);
                    isValidIamge = (bmpCheck.Height > 0);

                    if (isValidIamge && isImageFile(fnameFirst))
                    {
                        FWidth = bmpCheck.Width;
                        FHeight = bmpCheck.Height;
                        bmpCheck.Dispose();

                        Allocate();

                        //build trunk name
                        fnameTrunk = Path.GetFileNameWithoutExtension(fnameFirst);
                        int iSequenceFilename = System.Convert.ToInt32(fnameTrunk.Substring(fnameTrunk.Length - 1, 1));
                        fnameTrunk.Substring(0,fnameTrunk.Length - 1);

                        int iImage = 0;
                        
                        count = 0;
                        bool hasntFailed = true;
                        string sequenceNumber;
                        
                        string fnameCurrentFrame = fnameFirst;
                        string extension = Path.GetExtension(fnameFirst);
                        string pathName = Path.GetDirectoryName(fnameFirst);

                        while (isImageFile(fnameCurrentFrame) && hasntFailed)
                        {

                            //load image
                            hasntFailed = loadImage(iImage, fnameCurrentFrame);
                            if (hasntFailed)
                                count++;

                            //increase sequence number
                            iImage++;
                            iSequenceFilename++;
                            sequenceNumber = System.Convert.ToString(iSequenceFilename);
                            string paddedTrunk;
                            
                            //perhaps files are like 8,9,10.
                            //i.e. higher numbers have longer names
                            paddedTrunk = fnameTrunk;
                            while (fnameTrunk.Length < sequenceNumber.Length)
                                paddedTrunk = "0" + paddedTrunk;

                            fnameCurrentFrame = pathName + "\\" + paddedTrunk.Substring(0, paddedTrunk.Length - sequenceNumber.Length) + sequenceNumber + extension;
                            
                        }
                    }
                }

            isLoading = false;
        }
	}
        


}
