#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "CalcualateBrightnessForVanity", Category = "Value", Help = "Basic template with one value in/out", Tags = "c#")]
	#endregion PluginInfo
	public class ValueCalcualateBrightnessForVanityNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Distance", DefaultValue = 1.0)]
		public ISpread<double> FInDistance;
		
		[Input("RecordingDecay", DefaultValue = 1.0)]
		public ISpread<double> FInRecordingDecay;

		[Output("Output")]
		public ISpread<double> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				if(FInRecordingDecay[i] == 1) {
					FOutput[i] = 1.0;
				}
				else if(FInRecordingDecay[i] == 0) {
					FOutput[i] = FInDistance[i] < 1e-7 ? 1.0 : 0.0;
				}
				else {
					var t = FInRecordingDecay[i];
					var d = FInDistance[i];
					
					var length = t * 20;
					var brightness = 1.0 - d / length;	
					
					//apply guassian at the end
					var c = 0.1;
					var x = d - length;
					brightness += 2.0 * Math.Exp(-x * x / (2 * c * c));
					FOutput[i] = brightness;
				}
			}
		}
	}
}
