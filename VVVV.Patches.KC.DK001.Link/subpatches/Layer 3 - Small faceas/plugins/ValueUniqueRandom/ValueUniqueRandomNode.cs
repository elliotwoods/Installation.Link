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
	[PluginInfo(Name = "UniqueRandom", Category = "Value", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class ValueUniqueRandomNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Max", DefaultValue = 1.0)]
		ISpread<int> FMax;
		
		[Input("Randomise", DefaultValue = 0)]
		ISpread<bool> FRandomise;

		[Output("Output")]
		ISpread<int> FOutput;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			int nSlices = FRandomise.SliceCount;
			int max = FMax[0];
			
			Random rnd = new Random();
			FOutput.SliceCount = nSlices;
			
			for (int iSlice=0; iSlice<nSlices; iSlice++)
			{
				if (FRandomise[iSlice])
				{
					int newValue = rnd.Next(0, max);
					
					int attempts = 0;
					while (alreadyHave(newValue))
					{
						newValue++;
						newValue %= max;
						
						attempts++;
						if (attempts > max)
						{
							FLogger.Log(LogType.Error, "We ran out of free slots");
							break;
						}
					}
					
					FOutput[iSlice] = newValue;
				}
			}

		}
		
		private bool alreadyHave(int iValue)
		{
			for (int i=0; i<FOutput.SliceCount; i++)
			{
				if (FOutput[i] == iValue)
					return true;
			}
			return false;
		}
	}
}
