#region usings
using System;
using System.ComponentModel.Composition;
using System.IO;

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
    [PluginInfo(Name = "PlayWaveFile", Category = "NAudio", Help = "Play wav files through WASAPI devices", Tags = "")]
    #endregion PluginInfo
    public class NAudioPlayWaveFileNode : IPluginEvaluate
    {
        #region fields

        [Input("Device", IsSingle = true)]
        ISpread<MMDevice> FPinInDevice;

        [Input("Play", IsSingle = true)]
        ISpread<bool> FPinInPlay;

        [Input("Pause", IsSingle = true)]
        ISpread<bool> FPinInPause;

        [Input("Loop", IsSingle = true)]
        ISpread<bool> FPinInLoop;

        [Input("Filename", IsSingle=true, StringType = StringType.Filename, FileMask="*.wav")]
        IDiffSpread<String> FPinInFilename;

        //

        [Output("Playing")]
        ISpread<bool> FPinOutPlaying;

        [Output("Position", DimensionNames=new string[] {"s"})]
        ISpread<double> FPinOutPosition;

        [Output("Status")]
        ISpread<string> FPinOutStatus;

        IPluginHost FHost;

        MMDevice FDevice;
        WasapiOut FWaveOut;
        WaveStream FWaveStream;
        IWaveProvider FDownMix;

        string FFilename;
        string FPatchPath;

        double FPosition = 0;
        bool FIsPlaying = false;
        bool FIsPaused = false;
        bool FIsFileOpen = false;

        bool FIsInitialised = false;

        #endregion
        
        #region Constructor
        [ImportingConstructor()]
        public NAudioPlayWaveFileNode(IPluginHost host)
        {
            //assign host
            this.FHost = host;

            FHost.GetHostPath(out FPatchPath);
            FPatchPath = Path.GetDirectoryName(FPatchPath);
        }
        #endregion

        public void Evaluate(int SpreadMax)
        {
            if (FPinInDevice[0] != FDevice)
                setDevice(FPinInDevice[0]);

            if (FPinInFilename[0] != FFilename)
            {
                StopPlayback();
                setFilename(FPinInFilename[0]);
                StartPlayback();
            }

            if (FPinInPlay[0] != FIsPlaying)
            {
                if (FPinInPlay[0])
                    StartPlayback();
                else
                    StopPlayback();
            }

            if (FPinInPause[0] != FIsPaused)
            {
                if (FPinInPause[0])
                    PausePlayback();
                else
                    ResumePlayback();
            }

            //repeat
            if (FPinInLoop[0])
                if (FIsPlaying && !FIsPaused)
                {
                    if (FWaveStream.Position == FWaveStream.Length / FWaveStream.WaveFormat.Channels)
                        StartPlayback();
                }

            FPinOutPlaying[0] = FIsPlaying && !FIsPaused;
            FPinOutPosition[0] = (FIsPlaying ? (double)FWaveStream.Position / (double)FWaveStream.WaveFormat.AverageBytesPerSecond : 0) ;
        }

        private void setFilename(string filename)
        {
            FFilename = filename;
            OpenFile(FFilename);
        }

        private void setDevice(MMDevice dev)
        {
            FDevice = dev;

            closeDevice();

            if (FDevice == null)
                return;

            if (FIsInitialised)
                ReinitaliseWaveOut();

            if (FIsPlaying)
                StartPlayback();

            FPinOutStatus[0] = "Device opened";
        }

        private void closeDevice()
        {
            StopPlayback();
            if (FWaveOut != null)
            {
                FWaveOut.Dispose();
                FWaveOut = null;
            }
            FPinOutStatus[0] = "Device closed";
        }

        private void CreateInputStream(string fileName)
        {
            WaveStream readerStream = new WaveFileReader(fileName);
            if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                readerStream = new BlockAlignReductionStream(readerStream);
            }

            FWaveStream = readerStream;
        }

        private bool OpenFile(string filename)
        {
            CloseFile();

            if (filename == "")
            {
                FPinOutStatus[0] = "Waiting for valid filename";
                return false;
            }

            try
            {
                CreateInputStream(FFilename);
                ReinitaliseWaveOut();
                FPinOutStatus[0] = "Succesfully opened file";
            }
            catch
            {
                FPinOutStatus[0] = "Cannot open file";
                return false;
            }

            StopPlayback();

            FIsFileOpen = true;

            return true;
        }

        private void ReinitaliseWaveOut()
        {
            if (FIsInitialised && FWaveOut != null)
            {
                FWaveOut.Dispose();
            }

            FWaveOut = new WasapiOut(FPinInDevice[0], AudioClientShareMode.Shared, false, 100);
            FWaveOut.Init(FWaveStream);

            FIsInitialised = true;
        }

        private void CloseFile()
        {
            if (FIsFileOpen)
            {
                StopPlayback();
                FWaveStream.Close();
                FWaveStream = null;
                FIsFileOpen = false;
            }
        }

        private void StartPlayback()
        {
            if (!FIsFileOpen)
                if (!OpenFile(FFilename))
                {
                    FIsPlaying = false;
                    return;
                }

            FWaveStream.Position = 0;
            FWaveOut.Play();
            FPinOutStatus[0] = "Playing";
            FIsPlaying = true;
            FIsPaused = false;
        }

        private void PausePlayback()
        {
            FWaveOut.Pause();
            FIsPaused = true;
        }

        private void ResumePlayback()
        {
            FWaveOut.Play();
            FIsPaused = false;
        }

        private void StopPlayback()
        {
            if (FIsPlaying)
            {
                FWaveOut.Stop();
                FIsPlaying = false;
                FIsPaused = false;
            }
        }
    }
}
