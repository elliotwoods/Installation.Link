#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "ListRecordDevices", Category = "NAudio", Help = "List WASAPI recording devices", Tags = "")]
    #endregion PluginInfo
    public class NAudioListRecordDevicesNode : IPluginEvaluate
    {
        #region fields & pins

        [Input("Refresh", IsBang = true)]
        ISpread<bool> FPinInRefresh;

        [Output("Device")]
        ISpread<MMDevice> FPinOutDevices;

        [Output("Device name")]
        ISpread<string> FPinOutDeviceNames;

        [Output("Device ID")]
        ISpread<string> FPinOutDeviceIDs;

        [Import()]
        ILogger FLogger;

        bool FFirstRun = true;
        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FPinInRefresh[0] == true || FFirstRun)
            {
                FFirstRun = false;

                MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
                MMDeviceCollection deviceCol = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

                int count = deviceCol.Count;
                FPinOutDevices.SliceCount = count;
                FPinOutDeviceNames.SliceCount = count;
                FPinOutDeviceIDs.SliceCount = count;

                for (int i = 0; i < count; i++)
                {
                    FPinOutDevices[i] = deviceCol[i];
                    FPinOutDeviceNames[i] = deviceCol[i].DeviceFriendlyName;
                    FPinOutDeviceIDs[i] = deviceCol[i].ID;
                }
            }
        }
    }
}
