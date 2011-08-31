#region usings
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using System.Drawing;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Screens", Category = "System", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class SystemScreensNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Update", IsSingle=true, IsBang=true)]
		ISpread<bool> FInput;
		
		[Output("Resolution")]
		ISpread<Vector2D> FResolution;		
		[Output("Position")]
		ISpread<Vector2D> FPosition;		
		[Output("BPP")]
		ISpread<int> FBPP;
		[Output("Device Name")]
		ISpread<string> FDeviceName;

		[Import()]
		ILogger FLogger;
		
		bool FFirstRun = true;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun || FInput[0])
			{
				Screen[] screens = System.Windows.Forms.Screen.AllScreens;
				FResolution.SliceCount = screens.Length;
				FPosition.SliceCount = screens.Length;
				FDeviceName.SliceCount = screens.Length;
				FBPP.SliceCount = screens.Length;
				
				System.Drawing.Rectangle bounds = new System.Drawing.Rectangle();
				int x,y,w,h;
				Vector2D xy, wh;
				for (int i=0; i<screens.Length; i++)
				{
					bounds = screens[i].Bounds;
					w = bounds.Width;
					h = bounds.Height;
					x = bounds.X;
					y = bounds.Y;
					xy = new Vector2D(x, y);
					wh = new Vector2D(w, h);

					FResolution[i] = wh;
					FPosition[i] = xy;
					FDeviceName[i] = screens[i].DeviceName;
					FBPP[i] = screens[i].BitsPerPixel;
				}
			}
		}
	}
}
