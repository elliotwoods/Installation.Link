#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Drawing.Imaging;
using System.Diagnostics;

#endregion usings

namespace VVVV.Nodes.Recorder
{
	#region PluginInfo
	[PluginInfo(Name = "Recorder",
				Category = "",
				Version = "EX9.Texture",
				Help = "Captures texture to disk in background thread",
				Tags = "",
				AutoEvaluate = true)]
	#endregion PluginInfo
	public class RecordNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
	{
		class Instance : IDisposable
		{
            class Saver : IDisposable
            {
                public enum State
                {
                    Available,
                    Saving
                }

                byte[] FData = null;
                Bitmap FBitmap = null;
                Thread FThread;
                string FFilename;
                Surface FBackGPUSurface;
                Surface FOffscreenSurface;

                int FWidth;
                int FHeight;
                int FLength;

                public State CurrentState {get; private set; }

                public bool Available
                {
                    get
                    {
                        return CurrentState == State.Available;
                    }
                }   

                public Saver()
                {
                    CurrentState = State.Available;
                }

                public void Save(Surface surface, string filename)
                {
                    CurrentState = State.Saving;

                    var device = surface.Device as DeviceEx;
                    var format = surface.Description.Format;

                    FWidth = surface.Description.Width;
                    FHeight = surface.Description.Height;
                    FLength = FWidth * FHeight * 4;

                    if (FBitmap == null || FBitmap.Width != FWidth || FBitmap.Height != FHeight)
                    {
                        FBackGPUSurface = Surface.CreateOffscreenPlainEx(device, FWidth, FHeight, format, Pool.Default, Usage.None);
                        FOffscreenSurface = Surface.CreateOffscreenPlainEx(device, FWidth, FHeight, format, Pool.SystemMemory, Usage.None);
                        FBitmap = new Bitmap(FWidth, FHeight);
                    }

                    device.StretchRectangle(surface, FBackGPUSurface, TextureFilter.None);

                    FFilename = filename;
                    FThread = new Thread(ThreadedFunction);
                    FThread.Start();
                }

                void ThreadedFunction()
                {
                    try
                    {
                        if (FData == null || FData.Length != FLength)
                        {
                            FData = new byte[FLength];
                        }

                        Surface.FromSurface(FOffscreenSurface, FBackGPUSurface, Filter.None, 0);

                        var bitmapData = FBitmap.LockBits(new Rectangle(0, 0, FWidth, FHeight),
                            ImageLockMode.WriteOnly,
                            PixelFormat.Format32bppRgb);
                        try
                        {
                            var rect = FOffscreenSurface.LockRectangle(LockFlags.ReadOnly);
                            try
                            {
                                rect.Data.Read(FData, 0, FLength);
                            }
                            finally
                            {
                                FOffscreenSurface.UnlockRectangle();
                            }

                            Marshal.Copy(FData, 0, bitmapData.Scan0, FLength);
                        }
                        finally
                        {
                            FBitmap.UnlockBits(bitmapData);
                        }
                        FBitmap.Save(FFilename);
                    }
                    catch (Exception e)
                    {
                        Debug.Print(e.Message);
                    }
                    finally
                    {
                        CurrentState = State.Available;
                    }
                }

                public void Dispose()
                {
                    if (FThread != null)
                    {
                        FThread.Join();
                    }
                    if (FBitmap != null)
                    {
                        FBitmap.Dispose();
                        FBitmap = null;
                    }
                    if (FBackGPUSurface != null)
                    {
                        FBackGPUSurface.Dispose();
                        FBackGPUSurface = null;
                    }
                    if (FOffscreenSurface != null)
                    {
                        FOffscreenSurface.Dispose();
                        FOffscreenSurface = null;
                    }
                }
            }
			public enum TextureType
			{
				None,
				RenderTarget,
				DepthStencil,
				Dynamic
			}
			
			Direct3DEx FContext;
			DeviceEx FDevice;
			Control FHiddenControl;
			bool FInitialised = false;

			int FWidth;
			int FHeight;
			IntPtr FHandle;
			Format FFormat;
			Usage FUsage;

			SlimDX.Direct3D9.Texture FTextureShared = null;

            List<Saver> FSavers = new List<Saver>();

			public Instance()
			{
				this.FContext = new Direct3DEx();
				this.FHiddenControl = new Control();
			}

			public bool Ready
			{
				get
				{
					return this.FInitialised;
				}
			}

			public int Width
			{
				get
				{
					return this.FWidth;
				}
			}
			public int Height
			{
				get
				{
					return this.FHeight;
				}
			}

			Format enumToFormat(EnumEntry formatEnum)
			{
				Format format;
				if (formatEnum.Name == "INTZ")
					format = D3DX.MakeFourCC((byte)'I', (byte)'N', (byte)'T', (byte)'Z');
				else if (formatEnum.Name == "RAWZ")
					format = D3DX.MakeFourCC((byte)'R', (byte)'A', (byte)'W', (byte)'Z');
				else if (formatEnum.Name == "RESZ")
					format = D3DX.MakeFourCC((byte)'R', (byte)'E', (byte)'S', (byte)'Z');
				else if (formatEnum.Name == "No Specific")
					throw (new Exception("Texture mode not supported"));
				else
					format = (Format)Enum.Parse(typeof(Format), formatEnum, true);
				return format;
			}

			Usage enumToUsage(EnumEntry usageEnum)
			{
				var usage = Usage.Dynamic;
				if (usageEnum.Index == (int)(TextureType.RenderTarget))
					usage = Usage.RenderTarget;
				else if (usageEnum.Index == (int)(TextureType.DepthStencil))
					usage = Usage.DepthStencil;
				return usage;
			}

			public void SetProperties(int width, int height, uint handle, EnumEntry formatEnum, EnumEntry usageEnum)
			{
				this.FHandle = (IntPtr)unchecked((int)handle);
				if (handle == 0)
				{
					throw (new Exception("No shared texture handle set"));
				}

				var format = enumToFormat(formatEnum);
				var usage = enumToUsage(usageEnum);

				if (width != this.FWidth || height != this.FHeight || format != this.FFormat || usage != this.FUsage)
				{
					Allocate(width, height);
				}

				this.FFormat = format;
				this.FUsage = usage;

				if (this.FTextureShared != null)
				{
					this.FTextureShared.Dispose();
					this.FTextureShared = null;
				}
				this.FTextureShared = new Texture(this.FDevice, this.FWidth, this.FHeight, 1, FUsage, FFormat, Pool.Default, ref this.FHandle);
			}

			void Allocate(int width, int height)
			{
				this.Deallocate();

				this.FWidth = width;
				this.FHeight = height;

				var flags = CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.PureDevice | CreateFlags.FpuPreserve;
				this.FDevice = new DeviceEx(FContext, 0, DeviceType.Hardware, this.FHiddenControl.Handle, flags, new PresentParameters()
				{
					BackBufferWidth = this.FWidth,
					BackBufferHeight = this.FHeight
				});

				this.FInitialised = true;
			}

			void Deallocate()
			{
				if (this.FDevice != null)
				{
					this.FDevice.Dispose();
					this.FDevice = null;
				}

                foreach(var saver in FSavers)
                {
                    saver.Dispose();
                }
                FSavers.Clear();

				FInitialised = false;
			}

			public void WriteImage(string filename)
			{
                if (FInitialised)
				{
                    var saver = GetAvailableSaver();
                    saver.Save(FTextureShared.GetSurfaceLevel(0), filename);
                }
            }

            Saver GetAvailableSaver()
            {
                foreach(var saver in FSavers)
                {
                    if (saver.Available)
                    {
                        return saver;
                    }
                }

                var newSaver = new Saver();
                FSavers.Add(newSaver);
                return newSaver;
            }

            public void CleanCompleteSavers(int minimumSavers)
            {
                HashSet<Saver> toRemove = new HashSet<Saver>();

                foreach(var saver in FSavers)
                {
                    if (FSavers.Count <= minimumSavers)
                    {
                        return;
                    }
                    if (saver.Available)
                    {
                        saver.Dispose();
                        toRemove.Add(saver);
                    }
                }

                FSavers.RemoveAll(saver => toRemove.Contains(saver));
            }

            public void Dispose()
            {
                Deallocate();
            }
        }
		#region fields & pins
#pragma warning disable 0649
		[Input("Width")]
		IDiffSpread<uint> FInWidth;

		[Input("Height")]
		IDiffSpread<uint> FInHeight;

		[Input("Format", EnumName = "TextureFormat")]
		IDiffSpread<EnumEntry> FInFormat;
		
		[Input("Usage", EnumName = "TextureUsage")]
		IDiffSpread<EnumEntry> FInUsage;

		[Input("Handle")]
		IDiffSpread<uint> FInHandle;

        [Input("Minimum Savers", DefaultValue=0)]
        ISpread<int> FInMinimumSavers;

		[Input("Filename", StringType=StringType.Filename)]
		ISpread<string> FInFilename;

		[Input("Write")]
		ISpread<bool> FInWrite;

		[Output("Status")]
		ISpread<string> FOutStatus;

		[Import]
		public ILogger FLogger;

		[Import]
		IHDEHost FHDEHost;

		Instance FInstance = new Instance();

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor()]
		public RecordNode(IPluginHost host)
		{
		}

		public void OnImportsSatisfied()
		{
			FHDEHost.MainLoop.OnRender += MainLoop_OnRender;
			FHDEHost.MainLoop.OnPresent += MainLoop_OnPresent;
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FInWidth.IsChanged || FInHeight.IsChanged || FInFormat.IsChanged || FInUsage.IsChanged || FInHandle.IsChanged)
			{
				Initialise();
			}
            FInstance.CleanCompleteSavers(FInMinimumSavers[0]);
		}

		void Initialise()
		{
			try 
			{
				FInstance.SetProperties((int) FInWidth[0],(int) FInHeight[0], FInHandle[0], FInFormat[0], FInUsage[0]);
				FOutStatus[0] = "OK";
			}
			catch (Exception e)
			{
				FOutStatus[0] = e.Message;
			}
		}

		void MainLoop_OnRender(object sender, EventArgs e)
		{
			
		}

		void MainLoop_OnPresent(object sender, EventArgs e)
		{
			if (FInWrite[0] && FInstance.Ready)
			{
				try
				{
					FInstance.WriteImage(FInFilename[0]);
					FOutStatus[0] = "OK";
				}
				catch(Exception ex)
				{
					FOutStatus[0] = ex.Message;
				}
			}
		}

		public void Dispose()
		{
            FInstance.Dispose();
			FHDEHost.MainLoop.OnRender -= MainLoop_OnRender;
			FHDEHost.MainLoop.OnPresent -= MainLoop_OnPresent;
		}
	}
}
