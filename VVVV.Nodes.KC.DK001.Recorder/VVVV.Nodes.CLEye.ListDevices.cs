using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V1;
using SlimDX.Direct3D9;
using System.Runtime.InteropServices;
using SlimDX;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using CLEyeMulticam;

namespace VVVV.Nodes.CLEye
{

    public unsafe class ListDevicesNode : IPlugin, IDisposable
    {

        #region Plugin Info
        public static IPluginInfo PluginInfo
        {
            get
            {
                IPluginInfo Info = new PluginInfo();
                Info.Name = "ListDevices";							//use CamelCaps and no spaces
                Info.Category = "CLEye";						//try to use an existing one
                Info.Version = "";						//versions are optional. leave blank if not needed
                Info.Help = "List available devices for CLEye Multicamera driver";
                Info.Bugs = "";
                Info.Credits = "";								//give credits to thirdparty code used
                Info.Warnings = "";
                Info.Author = "sugokuGENKI (Elliot Woods)";
                Info.Credits = "";

                //leave below as is
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
                System.Diagnostics.StackFrame sf = st.GetFrame(0);
                System.Reflection.MethodBase method = sf.GetMethod();
                Info.Namespace = method.DeclaringType.Namespace;
                Info.Class = method.DeclaringType.Name;
                return Info;
                //leave above as is
            }
        }
        #endregion

        #region Fields
        private IPluginHost FHost;

        //inputs
        private IValueIn FPinInUpdate;

        //outputs
        private IStringOut FPinOutDevices;

        bool firstRun = true;
        #endregion

        #region Auto Evaluate
        public bool AutoEvaluate
        {
            get { return true; }
        }
        #endregion

        #region Set Plugin Host
        public void SetPluginHost(IPluginHost Host)
        {
            //assign host
            this.FHost = Host;

            //inputs
            this.FHost.CreateValueInput("Update", 1, null, TSliceMode.Single, TPinVisibility.True, out FPinInUpdate);
            FPinInUpdate.SetSubType(0, 1, 1, 0, true, false, false);

            //outputs
            this.FHost.CreateStringOutput("GUID", TSliceMode.Dynamic, TPinVisibility.True, out this.FPinOutDevices);
        }
        #endregion

        #region Constructor
        #endregion

        #region Configurate
        public void Configurate(IPluginConfig Input)
        {

        }
        #endregion

        #region Evaluate
        public void Evaluate(int SpreadMax)
        {
            if (firstRun)
            {
                FillList();
                firstRun = false;
            }

            if (this.FPinInUpdate.PinIsChanged)
            {
                double dblUpdate;
                FPinInUpdate.GetValue(0, out dblUpdate);
                if (dblUpdate > 0.5)
                    FillList();
            }
        }

        private void FillList()
        {
            int nCams = CLEyeCameraDevice.CLEyeGetCameraCount();
            FPinOutDevices.SliceCount = nCams;

            for (int i = 0; i < nCams; i++)
                FPinOutDevices.SetString(i, CLEyeCameraDevice.CLEyeGetCameraUUID(i).ToString());
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
        }
        #endregion
    }
}
