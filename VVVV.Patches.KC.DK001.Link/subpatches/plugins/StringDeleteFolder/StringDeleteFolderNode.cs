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
	[PluginInfo(Name = "DeleteFolder", Category = "String", Help = "Basic template with one string in/out", Tags = "")]
	#endregion PluginInfo
	public class StringDeleteFolderNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Folder Name", StringType = StringType.Directory)]
		public ISpread<string> FInFoldername;

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
			bool doAnything = false;
			foreach(var doSomething in FInDo)
			{
				if (doSomething) {
					doAnything = true;
				}
			}
			FOutStatus.SliceCount = SpreadMax;
			
			for (int i=0; i<SpreadMax; i++)
			{
				if (FInDo[i])
				{
					try
					{
						Directory.Delete(FInFoldername[i], true);
						FOutStatus[i] = "OK";
					}
					catch (Exception e)
					{
						FOutStatus[i] = e.Message;
					}
				}
			}
		}
	}
}
