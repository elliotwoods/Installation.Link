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
    [PluginInfo(Name = "ListRecordDevices", Category = "NAudio", Version="WASAPI", Help = "List WASAPI recording devices", Tags = "")]
    #endregion PluginInfo
    public class NAudioListRecordDevicesNode : IListDevicesBase
    {
        public override DataFlow getEndType()
        {
            return DataFlow.Capture;
        }
    }
}
