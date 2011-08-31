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

namespace VVVV.Nodes.ImageCache
{
	class ImageCacheInstance : IDisposable
	{
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		#region fields

		//image data
		public List<IntPtr> Data = new List<IntPtr>();
		public int Width, Height;
		public string Status;

		//preallocation
		bool FPreallocated = false;

		//load thread
		string FFilenameFirst = "";
		string FFilenameTrunk = "";
		Thread FThreadLoad = null;
		bool FIsLoading = false;
		bool FStopLoading = false;
		#endregion

		public bool isLoaded
		{
			get {
				return !FIsLoading && Data.Count > 0;
			}
		}

		public int Count
		{
			get{
				return Data.Count;
			}
		}

		public void preAllocate(int count)
		{
			FPreallocated = true;
			Allocate(1, 1, count);
		}

		public bool isDifferentFilename(string f)
		{
			return f != FFilenameFirst;
		}

		public void load(string filenameFirst)
		{
			if (!System.IO.File.Exists(filenameFirst))
			{
				Status = "File doesn't exist";
				deAllocate();
			}
			else
			{
				closeThread();
				FFilenameFirst = filenameFirst;
				FThreadLoad = new Thread(fnLoadThread);
				Status = "Loading sequence";
				FThreadLoad.Start();
			}
		}

		private void closeThread()
		{
			if (FThreadLoad != null)
			{
				if (FThreadLoad.ThreadState == ThreadState.Running)
				{
					FStopLoading = true;
					FThreadLoad.Join(10);
					FThreadLoad = null;
				}
			}
		}

		private void fnLoadThread()
		{
			FIsLoading = true;
			FStopLoading = false;


			//build trunk name
			FFilenameTrunk = Path.GetFileNameWithoutExtension(FFilenameFirst);
			int iSequenceFilename = System.Convert.ToInt32(FFilenameTrunk.Substring(FFilenameTrunk.Length - 1, 1));

			string sequenceNumber;

			string fnameCurrentFrame = FFilenameFirst;
			string extension = Path.GetExtension(FFilenameFirst);
			string pathName = Path.GetDirectoryName(FFilenameFirst);

			int iImage = 0;

			Bitmap currentImage;

			while (System.IO.File.Exists(fnameCurrentFrame) && !FStopLoading && !(FPreallocated && iImage+1 > Data.Count))
			{

				try
				{
					currentImage = new Bitmap(fnameCurrentFrame);

					//reallocation
					if (iImage == 0)
					{
						
						if (FPreallocated)
						{
							Allocate(currentImage.Width, currentImage.Height, Data.Count);
						}
						else
						{
							Width = currentImage.Width;
							Height = currentImage.Height;
						}
					}

					if (FPreallocated)
					{
						setBitmap(iImage, currentImage);
					}
					else
					{
						addBitmap(currentImage);
					}
				}
				catch
				{
					Status = "Image load failed on " + fnameCurrentFrame;
					break;
				}


				//increase sequence number
				iSequenceFilename++;
				sequenceNumber = System.Convert.ToString(iSequenceFilename);
				string paddedTrunk;

				//perhaps files are like 8,9,10.
				//i.e. higher numbers have longer names
				paddedTrunk = FFilenameTrunk;
				while (FFilenameTrunk.Length < sequenceNumber.Length)
					paddedTrunk = "0" + paddedTrunk;


				iImage++;
				fnameCurrentFrame = pathName + "\\" + paddedTrunk.Substring(0, paddedTrunk.Length - sequenceNumber.Length) + sequenceNumber + extension;
			}

			Status = "Image loading complete";
			FIsLoading = false;
		}

		private void addBitmap(Bitmap b)
		{
			//check if same format as trunk image
			if (b.Height != Height || b.Width != Width)
				throw (new Exception("Image dimensions dont match"));

			BitmapData ImageData = b.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			IntPtr add = Marshal.AllocCoTaskMem(Width * Height * 4);
			CopyMemory(add, ImageData.Scan0, System.Convert.ToUInt32(Width * Height * 4));
			Data.Add(add);
		}

		private void setBitmap(int index, Bitmap b)
		{
			//check if same format as trunk image
			if (b.Height != Height || b.Width != Width)
				throw (new Exception("Image dimensions dont match"));

			BitmapData ImageData = b.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			IntPtr add = Marshal.AllocCoTaskMem(Width * Height * 4);
			CopyMemory(add, ImageData.Scan0, System.Convert.ToUInt32(Width * Height * 4));
			Data[index] = add;
		}

		private void Allocate(int w, int h, int count)
		{
			//preallocation

			//check if we've alreadt got right size allocated
			if (w == Width && h == Height && count == Data.Count)
				return;

			deAllocate();

			//grow list
			IntPtr newDataArea;
			for (int i = 0; i < count; i++)
			{
				newDataArea = Marshal.AllocCoTaskMem(w * h * 4);
				Data.Add(newDataArea);
			}

			Width = w;
			Height = h;
		}

		private void deAllocate()
		{
			//otherwise, delete if already allocated
			foreach (IntPtr p in Data)
				Marshal.FreeCoTaskMem(p);

			Data.Clear();

			Width = 0;
			Height = 0;
		}

		public IntPtr getData(int i)
		{
			return Data[i % Data.Count];
		}

		public void Dispose()
		{
			closeThread();
			deAllocate();
		}

	}

    [PluginInfo(Name = "ImageCache",
                Category = "EX9.Texture",
                Author = "sugokuGENKI",
                Version = "",
                Help = "Cache images into RAM for quick GPU access.",
                Credits = "",
                Tags = "")]
	public class ImageCacheNode : DXTextureOutPluginBase, IPluginEvaluate
	{

		#region Fields
		private IPluginHost FHost;

		[Config("Preallocate slots (0=dont preallocate)", DefaultValue=0, MinValue=0)]
		ISpread<int> FConfigAllocated;

		[Input("Index")]
		IDiffSpread<int> FPinInIndex;

		[Input("Path", StringType=StringType.Filename)]
		IDiffSpread<string> FPinInPath;

		[Input("Load", IsSingle=true)]
		IDiffSpread<bool> FPinInLoad;


		[Output("Count")]
		ISpread<int> FPinOutCount;

		[Output("Loaded")]
		ISpread<bool> FPinOutLoaded;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

        //loaded images
		private Dictionary<int, ImageCacheInstance> FImageCaches = new Dictionary<int, ImageCacheInstance>();

		bool FPreallocate = false;
		int FPreallocateSlots = 0;
		#endregion

        #region Constructor
        [ImportingConstructor()]
        public ImageCacheNode(IPluginHost host)
            : base(host)
        {
            this.FHost = host;
        }

		~ImageCacheNode()
        {
			foreach(KeyValuePair<int, ImageCacheInstance> c in FImageCaches)
			{
				c.Value.Dispose();
				FImageCaches.Remove(c.Key);
			}
        }
        #endregion


        #region Configurate
        public void Configurate(IPluginConfig Input)
		{
			FPreallocate = FConfigAllocated[0] > 0;
			FPreallocateSlots = FConfigAllocated[0];
        }
        
		#endregion

        
		#region Evaluate
		public void Evaluate(int SpreadMax)
		{
			bool needsUpdate = false;

			if (FPinInPath.IsChanged)
			{
				for (int i=0; i<FPinInPath.SliceCount; i++)
				{
					if (i > FImageCaches.Count - 1)
					{
						ImageCacheInstance cache = new ImageCacheInstance();
						if (FPreallocate)
						{
							cache.preAllocate(FPreallocateSlots);
						}
						cache.load(FPinInPath[i]);
						FImageCaches.Add(i, cache);
					}

					if (FImageCaches[i].isDifferentFilename(FPinInPath[i]))
						FImageCaches[i].load(FPinInPath[i]);
				}

				//remove any excess caches
				for (int i=FPinInPath.SliceCount; i<FImageCaches.Count; i++)
				{
					FImageCaches[i].Dispose();
					FImageCaches.Remove(i);
				}

				Reinitialize();

				needsUpdate = true;
			}


            if (FPinInIndex.IsChanged)
            {
				needsUpdate = true;
            }

			if (needsUpdate)
				Update();


			//outputs
			FPinOutCount.SliceCount = FImageCaches.Count;
			FPinOutLoaded.SliceCount = FImageCaches.Count;
			FPinOutStatus.SliceCount = FImageCaches.Count;

			for (int i=0; i<FImageCaches.Count; i++)
			{
				FPinOutCount[i] = FImageCaches[i].Count;
				FPinOutLoaded[i] = FImageCaches[i].isLoaded;
				FPinOutStatus[i] = FImageCaches[i].Status;
			}


		}
        #endregion

        #region Dispose
        public void Dispose()
		{
		}
		#endregion

		#region Texture
		//this method gets called, when Reinitialize() was called in evaluate,
		//or a graphics device asks for its data
		protected override Texture CreateTexture(int Slice, Device device)
		{
			//FLogger.Log(LogType.Debug, "Creating new texture at slice: " + Slice);
			return TextureUtils.CreateTexture(device, FImageCaches[Slice % FImageCaches.Count].Width, FImageCaches[Slice % FImageCaches.Count].Height);
		}

		//this method gets called, when Update() was called in evaluate,
		//or a graphics device asks for its texture, here you fill the texture with the actual data
		//this is called for each renderer, careful here with multiscreen setups, in that case
		//calculate the pixels in evaluate and just copy the data to the device texture here
		protected unsafe override void UpdateTexture(int Slice, Texture texture)
		{
			if (FImageCaches.Count > 0)
			{
				ImageCacheInstance cache = FImageCaches[Slice % FImageCaches.Count];
				if (cache.isLoaded)
				{
					
					Surface srf = texture.GetSurfaceLevel(0);
					DataRectangle rect = srf.LockRectangle(LockFlags.Discard);

					rect.Data.WriteRange(cache.getData(FPinInIndex[Slice]), cache.Width * cache.Height * 4);

					srf.UnlockRectangle();
				}
			}
		}

		#endregion

	}
        


}
