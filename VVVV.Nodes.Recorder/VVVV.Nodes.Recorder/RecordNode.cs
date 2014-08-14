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
		class Instance
		{
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
			Surface FSurfaceOffscreen;

			byte[] FData = null;

			public Instance()
			{
				Mogre.DDSCodec.Startup();
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

				this.FSurfaceOffscreen = Surface.CreateOffscreenPlainEx(FDevice, FWidth, FHeight, Format.A8R8G8B8, Pool.SystemMemory, Usage.None);

				this.FData = new byte[FWidth * FHeight * 4];

				this.FInitialised = true;
			}

			void Deallocate()
			{
				if (this.FDevice != null)
				{
					this.FDevice.Dispose();
					this.FDevice = null;
				}

				if (this.FSurfaceOffscreen != null)
				{
					this.FSurfaceOffscreen.Dispose();
					this.FSurfaceOffscreen = null;
				}

				FInitialised = false;
				FData = null;
			}

			public byte[] ReadBack()
			{
				if (FInitialised)
				{
					Surface.FromSurface(FSurfaceOffscreen, FTextureShared.GetSurfaceLevel(0), Filter.None, 0xFFFFFF);

					bool success = false;
					var rect = FSurfaceOffscreen.LockRectangle(LockFlags.ReadOnly);
					try
					{
						rect.Data.Read(FData, 0, FData.Length);
						success = true;
					}
					catch(Exception e)
					{

					}
					finally
					{
						FSurfaceOffscreen.UnlockRectangle();
					}

					if (success)
					{
						return FData;
					}
				}

				return null;
			}

			public unsafe void WriteImage(string filename)
			{
				var data = ReadBack();
				if (data == null)
				{
					throw(new Exception("No data"));
				}
				var codec = Mogre.Codec.GetCodec("dds");
				fixed(byte * dataFixed = &data[0])
				{
					var memory = new Mogre.MemoryDataStream((void*) dataFixed, (uint) data.Length);
					var memoryPtr = new Mogre.MemoryDataStreamPtr(memory);
					var codecData = new Mogre.Codec.CodecData();
					var codecDataPtr = new Mogre.Codec.CodecDataPtr(codecData);
					codec.CodeToFile(memoryPtr, filename, codecDataPtr);
				}
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
			FHDEHost.MainLoop.OnRender -= MainLoop_OnRender;
			FHDEHost.MainLoop.OnPresent -= MainLoop_OnPresent;
		}
	}
}
