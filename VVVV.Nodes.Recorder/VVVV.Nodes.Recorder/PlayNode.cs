#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using FeralTic.DX11.Resources;
using VVVV.DX11;
using FeralTic.DX11;
using SlimDX.Direct3D11;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo	
	[PluginInfo(Name = "Play", Category = "EX9.Texture", Version = "Link", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class PlayerNode : IPluginEvaluate, IDX11ResourceProvider, IDisposable
	{
		class Instance : IDisposable
		{
			class Image
			{
				public string Filename;
				public byte[] Data = null;
				public bool LoadedCPU = false;
			}

			class ResourcePool : IDisposable
			{
				public List<DX11DynamicTexture2D> FTextures = new List<DX11DynamicTexture2D>();
				DX11DynamicTexture2D FFrontTexture;
				DX11RenderContext FContext;

				public ResourcePool(DX11RenderContext context, int width, int height)
				{
					FFrontTexture = new DX11DynamicTexture2D(context, width, height, SlimDX.DXGI.Format.R8_UNorm);
					byte[] black = new byte[width * height];
					for(int i=0; i<width * height; i++)
					{
						black[i] = 0;
					}
					FFrontTexture.WriteData(black);
					FContext = context;
				}

				public DX11DynamicTexture2D GetTexture(int index)
				{
					if (FTextures.Count != 0)
					{
						index = VMath.Clamp(index, 0, FTextures.Count - 1);
						FContext.CurrentDeviceContext.CopyResource(FTextures[index].Resource, FFrontTexture.Resource);
					}
					return FFrontTexture;
				}

				public void Dispose()
				{
					FFrontTexture.Dispose();
					foreach(var texture in FTextures)
					{
						texture.Dispose();
					}
				}
			}

			int FWidth = 320;
			int FHeight = 240;

			ILogger FLogger;

			static Object FLockCreation = new Object();

			public DX11Resource<DX11DynamicTexture2D> Resource = new DX11Resource<DX11DynamicTexture2D>();
			public int Index = 0;

			Dictionary<DX11RenderContext, ResourcePool> FTexturePool = new Dictionary<DX11RenderContext, ResourcePool>();
			Object FTexturePoolLock = new Object();

			List<Image> FMemoryPool = new List<Image>();
			Object FMemoryPoolLock = new Object();

			bool FRunningThread = true;
			Thread FThread;

			bool FNeedsRelist = false;
			string FPath;
			string FMask;

			public Instance(int width, int height, ILogger logger)
			{
				FWidth = width;
				FHeight = height;
				FLogger = logger;

				FThread = new Thread(ThreadedFunction);
				FThread.Name = "Player";
				FThread.Start();
			}

			public void LoadPath(string path, string mask)
			{
				if (FPath == path)
				{
					return;
				}
				
				lock (FTexturePoolLock)
				{
					FTexturePool.Clear();
				}

				FPath = path;
				FMask = mask;
				FNeedsRelist = true;
			}

			public void Update(DX11RenderContext context)
			{
				ResourcePool resourcePool;
				lock (FTexturePoolLock)
				{
					if (!FTexturePool.ContainsKey(context))
					{
						resourcePool = new ResourcePool(context, FWidth, FHeight);
						FTexturePool.Add(context, resourcePool);
					}
					else
					{
						resourcePool = FTexturePool[context];
					}
					Resource[context] = resourcePool.GetTexture(Index);
				}
			}

			unsafe void ThreadedFunction()
			{
				Random random = new Random();
				Thread.Sleep((int)(random.NextDouble() * 1000.0));

				while(FRunningThread)
				{
					try
					{
						if (FNeedsRelist)
						{
							try
							{
								var filenames = Directory.GetFiles(FPath, FMask);
								lock (FMemoryPoolLock)
								{
									FMemoryPool.Clear();
									foreach (var filename in filenames)
									{
										var image = new Image();
										image.Filename = filename;
										FMemoryPool.Add(image);
									}
								}
							}
							catch (Exception e)
							{
								FLogger.Log(e);
							}
							FNeedsRelist = false;
						}

						lock (FMemoryPoolLock)
						{
							int imageIndex = 0;
							foreach (var image in FMemoryPool)
							{
								try
								{
									if (!image.LoadedCPU)
									{
										var bitmap = new Bitmap(image.Filename);
										image.Data = new byte[FWidth * FHeight];
										var bitmapData = bitmap.LockBits(new Rectangle(0, 0, FWidth, FHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
										Exception e3 = null;
										try
										{
											byte* input = (byte*)bitmapData.Scan0;
											fixed (byte* outputFixed = &image.Data[0])
											{
												byte* output = outputFixed;
												for (int i = 0; i < FWidth * FHeight; i++)
												{
													input++; //a

													int pixelData = 0;
													pixelData += *input++;
													pixelData += *input++;
													pixelData += *input++;

													*output++ = (byte)(pixelData / 3);
												}
											}

											image.LoadedCPU = true;
										}
										catch (Exception e2)
										{
											e3 = e2;
										}
										finally
										{
											bitmap.UnlockBits(bitmapData);
										}
										if (e3 != null)
										{
											throw (e3);
										}
										//we've done enough work for this cycle
										break;
									}

									lock (FTexturePoolLock)
									{
										foreach (var resourcePool in FTexturePool)
										{
											lock(FLockCreation)// make sure all threads don't go crazy at the same time
											{
												var context = resourcePool.Key;
												var texturePool = resourcePool.Value.FTextures;
												if (texturePool.Count <= imageIndex)
												{
													var texture = new DX11DynamicTexture2D(context, FWidth, FHeight, SlimDX.DXGI.Format.R8_UNorm);

													GCHandle pinnedArray = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
													IntPtr pointer = pinnedArray.AddrOfPinnedObject();
													texture.WriteDataPitch(pointer, image.Data.Length, FWidth);
													pinnedArray.Free();

													texturePool.Add(texture);
												}
											}
											//we've done enough work for this cycle
											break;
										}
									}
								}
								catch (Exception e)
								{
									FLogger.Log(e);
								}
								imageIndex++;
							}
						}
					}
					catch(Exception e)
					{

					}
					Thread.Sleep(10);
				}
			}

			public void Destroy(DX11RenderContext context)
			{
				if (FTexturePool.ContainsKey(context))
				{
					FTexturePool[context].Dispose();
					FTexturePool.Remove(context);
				}
			}

			public List<int> LoadedFramesPerContext
			{
				get
				{
					var result = new List<int>();
					lock (FTexturePoolLock)
					{
						foreach(var context in FTexturePool)
						{
							result.Add(context.Value.FTextures.Count);
						}
					}
					return result;
				}
			}	

			public void Dispose()
			{
				FRunningThread = false;
				FThread.Join();

				FMemoryPool.Clear();
				foreach(var texture in FTexturePool)
				{
					texture.Value.Dispose();
				}
				FTexturePool.Clear();
			}
		}

		#region fields & pins
		[Input("Path", StringType = StringType.Directory)]
		public IDiffSpread<string> FInPath;

		[Input("Image Width", DefaultValue = 320, IsSingle = true)]
		public ISpread<int> FInWidth;

		[Input("Image Height", DefaultValue = 240, IsSingle = true)]
		public ISpread<int> FInHeight;

		[Input("Index")]
		public IDiffSpread<int> FInIndex;

		[Output("Texture Out")]
		Pin<DX11Resource<DX11DynamicTexture2D>> FOutTexture;

		[Output("Loaded Frames Per Context")]
		public ISpread<ISpread<int>> FOutLoadedFrames;

		[Output("Status")]
		public ISpread<string> FOutStatus;

		[Import()]
		public ILogger FLogger;

		List<Instance> FInstances = new List<Instance>();
		#endregion fields & pins

		public PlayerNode()
		{
			
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			try
			{
				if (FInstances.Count != SpreadMax)
				{
					FOutTexture.SliceCount = SpreadMax;
					while (FInstances.Count < SpreadMax)
					{
						var instance = new Instance(FInWidth[0], FInHeight[0], FLogger);
						FInstances.Add(instance);
						FOutTexture[FInstances.Count - 1] = instance.Resource;
					}
					while (FInstances.Count > SpreadMax)
					{
						FInstances[FInstances.Count - 1].Dispose();
						FInstances.RemoveAt(FInstances.Count - 1);
					}
				}
				
				if (FInPath.IsChanged)
				{
					for (int i = 0; i < SpreadMax; i++)
					{
						FInstances[i].LoadPath(FInPath[i], "*.png");
					}
				}
				if (FInIndex.IsChanged)
				{
					for (int i = 0; i < SpreadMax; i++)
					{
						FInstances[i].Index = FInIndex[i];
					}
				}
				FOutLoadedFrames.SliceCount = FInstances.Count;
				for (int i = 0; i < SpreadMax; i++)
				{
					FOutLoadedFrames[i].AssignFrom(FInstances[i].LoadedFramesPerContext);
				}
			}
			catch(Exception e)
			{
				FOutStatus[0] = e.Message;
			}
		}

		public void Destroy(IPluginIO pin, FeralTic.DX11.DX11RenderContext context, bool force)
		{
			foreach(var instance in FInstances)
			{
				instance.Destroy(context);
			}
		}

		public void Update(IPluginIO pin, FeralTic.DX11.DX11RenderContext context)
		{
			foreach (var instance in FInstances)
			{
				instance.Update(context);
			}
		}

		public void Dispose()
		{
			foreach (var instance in FInstances)
			{
				instance.Dispose();
			}
		}
	}
}
