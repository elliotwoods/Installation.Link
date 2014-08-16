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
	[PluginInfo(Name = "FillRecordingGaps", Category = "File", AutoEvaluate = true)]
	#endregion PluginInfo
	public class FileFillRecordingGapsNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Folder", StringType=StringType.Directory)]
		public ISpread<string> FInFolder;

		[Input("Recording Length", MinValue = 0)]
		public ISpread<int> FInRecordingLength;

		[Input("Filename Length", MinValue = 0)]
		public ISpread<int> FInFilenameLength;
		
		[Input("Extension", DefaultString = "png")]
		public ISpread<string> FInExtension;
		
		[Input("Do", IsBang = true)]
		public ISpread<bool> FInDo;

		[Output("Status")]
		public ISpread<string> FOutStatus;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		string makeString(int index, int sliceIndex)
		{
			string result = index.ToString();
			while (result.Length < FInFilenameLength[sliceIndex])
			{
				result = "0" + result;
			}
			
			return FInFolder[sliceIndex] + "\\" + result + FInExtension[sliceIndex];;
		}
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutStatus.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				if (FInDo[i]) {
					try
					{
						FOutStatus[i] = "";
						for(int fileIndex = 0; fileIndex < FInRecordingLength[i]; fileIndex++)
						{
							var filename = makeString(fileIndex, i);
							if (File.Exists(filename)) {
								continue;
							}
							
							bool found = false;
							
							for(int searchForMatch = -10; searchForMatch < 10; searchForMatch++)
							{
								var searchFilename = makeString(searchForMatch, i);
								if (File.Exists(searchFilename))
								{
									File.Copy(searchFilename, filename);
									found = true;
									break;
								}
							}
							if (!found) {
								FOutStatus[i] += "No match found for file [" + filename + "]\n";
							}
						}
						if (FOutStatus[i].Length == 0) {
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
}
