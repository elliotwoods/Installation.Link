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
    [PluginInfo(Name = "Recorder", Category = "NAudio", Version="Wave", Help = "Record mono wav files from WAVE devices", Tags = "")]
    #endregion PluginInfo
    public class RecordWaveNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Device ID")]
        ISpread<int> FPinInDeviceID;

        [Input("Filename", StringType = StringType.Filename, IsSingle = true)]
        ISpread<string> FPinInFilename;

        [Input("Amplification", DefaultValue = 1.0, IsSingle = true)]
        ISpread<float> FPinInAmplification;

        [Input("Compression attack", DefaultValue = 0.5, IsSingle = true)]
        ISpread<float> FPinInCompressionAttack;

        [Input("Gate", DefaultValue = 0.0, MinValue=0.0,  IsSingle = true)]
        ISpread<float> FPinInGate;

        [Input("Gate Decay", DefaultValue = 0.0, MinValue = 0.0, IsSingle = true)]
        ISpread<float> FPinInGateDecay;

		[Input("Record", IsSingle = true)]
		ISpread<bool> FPinInRecord;

        [Output("Recording")]
        ISpread<bool> FPinOutRecording;

        [Output("Audio Level")]
        ISpread<float> FPinOutLevel;

        [Output("Compression")]
        ISpread<float> FPinOutCompression;

        [Output("Position")]
        ISpread<float> FPinOutPosition;

        [Output("Status")]
        ISpread<string> FPinOutStatus;

        [Import()]
        ILogger FLogger;

        int FDeviceID = -1;
        float FPosition = 0;
        bool FIsRecording = false;
        string FFilename = "";
        string FPatchPath;
        float FAmplification = 1.0f;
        float FClipCompression = 1.0f;
        float FGateCompression = 1.0f;
        float FCompressionAttack = 0.5f;
        float FGate = 0.0f;
        float FGateDecay = 0.0f;

        WaveIn FWaveIn;
        WaveFileWriter writer;
        EventHandler<WaveInEventArgs> handleData;
        #endregion fields & pins

        void Dispose()
        {
            closeDevice();
            closeRecorder();
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FPinInDeviceID[0] != FDeviceID)
                setDevice(FPinInDeviceID[0]);

            if (FPinInFilename[0] != FFilename)
                setFilename(FPinInFilename[0]);

            if (FPinInRecord[0] != FIsRecording)
            {
                if (FPinInRecord[0])
                    StartRecording();
                else
                    StopRecording();
            }

            if (FPinInAmplification[0] != FAmplification)
                FAmplification = FPinInAmplification[0];

            if (FPinInCompressionAttack[0] != FCompressionAttack)
                FCompressionAttack = FPinInCompressionAttack[0];

            if (FPinInGate[0] != FGate)
                FGate = FPinInGate[0];

            if (FPinInGateDecay[0] != FGateDecay)
                FGateDecay = FPinInGateDecay[0]; 

            FPinOutRecording[0] = FIsRecording;
            FPinOutPosition[0] = FPosition;
            FPinOutCompression[0] = FClipCompression * FGateCompression;
        }

        private void setFilename(string filename)
        {
            FFilename = filename;
        }

        private void setDevice(int deviceID)
        {
            if (FIsRecording)
                return;

            closeDevice();
            FDeviceID = deviceID;

            FWaveIn = new WaveIn(WaveCallbackInfo.FunctionCallback());
            FWaveIn.DeviceNumber = deviceID;

            if (FWaveIn.WaveFormat.Channels > 2)
            {
                WaveFormat stereoFormat = new WaveFormat(FWaveIn.WaveFormat.SampleRate, FWaveIn.WaveFormat.BitsPerSample, 2);
                FWaveIn.WaveFormat = stereoFormat;
            }


            try
            {
                FWaveIn.StartRecording();
                handleData = new EventHandler<WaveInEventArgs>(waveInStream_DataAvailable);
                FWaveIn.DataAvailable += handleData;
                FPinOutStatus[0] = "Device opened";
            }
            catch
            {
                FWaveIn = null;
                FPinOutStatus[0] = "Failed to open device";
            }
            
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
            }
            else
                FPinOutStatus[0] = "Creating new file";

            if (System.IO.File.Exists(FFilename))
                System.IO.File.Delete(FFilename);

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

        private float lerp(float v1, float v2, float position)
        {
            return v1 * (1.0f - position) + v2 * position;
        }
        
        void waveInStream_DataAvailable(object sender, WaveInEventArgs e)
        {
            float level = 0;
            float levelUncompressed = 0;
            Int16 current;
            float amplified;
            float absAmplified;
            byte[] amplifiedBytes;

            float compression = FClipCompression * FGateCompression;
            float maxval = 0;

            //damp compression back to 0
            FClipCompression = lerp(FClipCompression, 1.0f, FCompressionAttack);
            if (FGate > 0.0f)
                FGateCompression = lerp(FGateCompression, 0.0f, FGateDecay);
            else
                FGateCompression = 1.0f;

            for (int i = 0; i < e.BytesRecorded/2; i ++)
            {

                current = BitConverter.ToInt16(e.Buffer, i*2);
                amplified = (float)current * FAmplification;
                absAmplified = Math.Abs(amplified);

                //check if apply compression from over volume
                if (absAmplified > Int16.MaxValue)
                    maxval = Math.Max(maxval, (float)absAmplified);

                levelUncompressed += Math.Abs(amplified);

                amplified *= compression;

                //if still over volume, let's clip
                if (Math.Abs(amplified) > Int16.MaxValue)
                    amplified = amplified > 0 ? Int16.MinValue : Int16.MaxValue;

                amplifiedBytes = BitConverter.GetBytes((Int16)amplified);
                Buffer.BlockCopy(amplifiedBytes, 0, e.Buffer, i*2, 2);

                level += Math.Abs(amplified);
            }

            // calc levels
            levelUncompressed /= (float)e.BytesRecorded * Int16.MaxValue;
            level /= (float)e.BytesRecorded * Int16.MaxValue;
            FPinOutLevel[0] = level;


            //if high level, open gate
            if (FGate != 0)
            {
                if (levelUncompressed > FGate)
                    FGateCompression= 1.0f;
            }

            //if high level, clip
            if (maxval > 0.0f)
                FClipCompression = lerp(FClipCompression, (float)(maxval / Int16.MaxValue), FCompressionAttack);
            

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
            try
            {
                file = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
            }
            catch
            {
                return true;
            }
            file.Close();
            return false;
        }
    }
}
