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
    [PluginInfo(Name = "Recorder", Category = "NAudio", Help = "Record wav files from WASAPI devices", Tags = "")]
    #endregion PluginInfo
    public class NAudioRecorderNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Device", IsSingle = true)]
        ISpread<MMDevice> FPinInDevice;

        [Input("Filename", StringType = StringType.Filename)]
        ISpread<string> FPinInFilename;

        [Input("+Record", IsSingle = true)]
        ISpread<bool> FPinInRecord;

        [Output("Recording")]
        ISpread<bool> FPinOutRecording;

        [Output("Audio Level")]
        ISpread<float> FPinOutLevel;

        [Output("Position")]
        ISpread<float> FPinOutPosition;

        [Output("Status")]
        ISpread<string> FPinOutStatus;

        [Import()]
        ILogger FLogger;

        float FPosition = 0;
        bool FIsRecording = false;
        string FFilename = "";

        MMDevice FDevice = null;
        WasapiCapture FWaveIn;
        WaveFileWriter writer;
        #endregion fields & pins

        EventHandler<WaveInEventArgs> handleData;

        void Dispose()
        {
            closeDevice(); 
            closeRecorder();
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FPinInDevice[0] != FDevice)
                setDevice(FPinInDevice[0]);

            if (FPinInFilename[0] != FFilename)
                FFilename = FPinInFilename[0];

            if (FPinInRecord[0] != FIsRecording)
            {
                if (validFilename(FFilename) && FDevice !=null)
                    if (FPinInRecord[0])
                        StartRecording();
                    else
                        StopRecording();
            }

            FPinOutRecording[0] = FIsRecording;
            FPinOutPosition[0] = FPosition;
        }

        private void setDevice(MMDevice dev)
        {
            if (FIsRecording)
                return;

            FDevice = dev;
            closeDevice();

            FWaveIn = new WasapiCapture(FPinInDevice[0]);

            //WaveFormat monoFormat = new WaveFormat(44100, 1);
            //FWaveIn.WaveFormat = monoFormat;

            FWaveIn.StartRecording();
            handleData = new EventHandler<WaveInEventArgs>(waveInStream_DataAvailable);
            FWaveIn.DataAvailable += handleData;

            FPinOutStatus[0] = "Device opened";
        }

        private void StartRecording()
        {
            if (System.IO.File.Exists(FFilename))
            {
                if (isFileLocked(FFilename))
                {
                    FPinOutStatus[0] = "Cannot open file for writing (perhaps locked by other application?)";
                    FIsRecording = false;
                    return;
                }
                System.IO.File.Delete(FFilename);
                FPinOutStatus[0] = "Replacing existing file.";
            } else
                FPinOutStatus[0] = "Creating new file";

            writer = new WaveFileWriter(FFilename, FWaveIn.WaveFormat);

            FPosition = 0.0f;
            FIsRecording = true;
        }

        private void StopRecording()
        {
            closeRecorder();

            FPinOutStatus[0] = "Recording saved";
        }

        private void closeDevice()
        {
            if (FWaveIn != null)
            {
                FWaveIn.StopRecording();
                FWaveIn.Dispose();
                FWaveIn = null;
            }
        }

        private void closeRecorder()
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;
                FIsRecording = false;
            }
        }

        void waveInStream_DataAvailable(object sender, WaveInEventArgs e)
        {
            float level = 0;

            for (int i = 1; i < e.BytesRecorded; i += 4)
                level += Math.Abs((float)e.Buffer[i]);
            level /= (float)e.BytesRecorded;

            FPinOutLevel[0] = level;

            if (FIsRecording)
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
                FPosition = (float)writer.Length / (float)FWaveIn.WaveFormat.AverageBytesPerSecond;
            }
        }

        bool validFilename(string filename)
        {
            bool bOk = false;
            try
            {
                new System.IO.FileInfo(filename);
                bOk = true;
            }
            catch (ArgumentException)
            {
            }
            catch (System.IO.PathTooLongException)
            {
            }
            catch (NotSupportedException)
            {
            }

            return bOk;
        }

        bool isFileLocked(string path)
        {
            System.IO.FileStream file;
            try {
                file = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
            }
            catch {
                return true;
            }
            file.Close();
            return false;
        }
    }
}
