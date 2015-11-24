#region usings
using System;
using System.IO;
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
	[PluginInfo(Name = "Delete", Category = "File", Version = "Directory", Help = "Basic template with one string in/out", Tags = "", AutoEvaluate = false)]
	#endregion PluginInfo
	public class DirectoryFileDeleteNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultString = "hello c#", StringType = StringType.Directory)]
		public ISpread<string> FInput;
		
		[Input("Do", IsBang = true)]
		public ISpread<bool> FInDo;

		[Output("Success")]
		public ISpread<bool> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FInDo[0]) {
				FOutput.SliceCount = SpreadMax;
				
				for(int i=0; i<SpreadMax; i++) {
					try {
						Directory.Delete(FInput[i], true);
						FOutput[i] = true;
					} catch {
						FOutput[i] = false;
					}
				}
			}
		}
	}
}
