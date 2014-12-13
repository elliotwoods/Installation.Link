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
	[PluginInfo(Name = "CreateDirectory", Category = "File",Tags = "", AutoEvaluate=true)]
	#endregion PluginInfo
	public class FileCreateDirectoryNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Directory", StringType = StringType.Directory)]
		public ISpread<string> FInDirectory;

		[Input("Do", IsBang = true)]
		public ISpread<bool> FInDo;
		
		[Output("Status")]
		public ISpread<string> FOutStatus;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutStatus.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				try
				{
					if (FInDo[i])
					{
						Directory.CreateDirectory(FInDirectory[i]);
						FOutStatus[i] = "OK";
					}
				}
				catch(Exception e)
				{
					FOutStatus[i] = e.Message;
				}
				
			}
		}
	}
}
