
#define WRITE_MONO

using System;
using System.ComponentModel.Composition;

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Collections.Generic;


namespace VVVV.Nodes.CLEye
{
    class AudioRecord
    {
#region fields

		static HashSet<int> UsedDevices = new HashSet<int>();

        public string FStatus;
        float FLevel;

        float FPosition = 0;
        bool FIsRecording = false;
        private string FFilename = "";

        public float Amplification = 1.0f;
        public float CompressionAttack = 0.5f;
        public float Gate = 0.0f;
        public float GateDecay = 0.0f;

        float FClipCompression = 1.0f;
        float FGateCompression = 1.0f;

        string FTempFilename = "";
#if WRITE_MONO
        int FDeviceID = -1;
        WaveIn FWaveIn;
#else
        MMDevice FDevice = null;
        WasapiCapture FWaveIn;
#endif
        WaveFileWriter writer;
        EventHandler<WaveInEventArgs> handleData;
#endregion

        public void setTempPath(string temporaryFile)
        {
			FTempFilename = temporaryFile;
        }

        void Dispose()
        {
            closeDevice();
            closeRecorder();
        }

        public bool setFilename(string filename)
        {
            if (validFilename(filename))
            {
                FFilename = filename;
                return true;
            }
            else
                return false;
        }
#if WRITE_MONO
		public int getDeviceID()
		{
			return FDeviceID;
		}

        public void setDevice(int deviceID)
        {
            if (FIsRecording)
                return;

            closeDevice();

			if (UsedDevices.Contains(deviceID))
			{
				FStatus = "Device in use";
				return;
			}

            FDeviceID = deviceID;

			try
			{

				FWaveIn = new WaveIn(WaveCallbackInfo.FunctionCallback());
				FWaveIn.DeviceNumber = deviceID;

				if (FWaveIn.WaveFormat.Channels > 2)
				{
					WaveFormat stereoFormat = new WaveFormat(FWaveIn.WaveFormat.SampleRate, FWaveIn.WaveFormat.BitsPerSample, 2);
					FWaveIn.WaveFormat = stereoFormat;
				}

				FWaveIn.StartRecording();
				handleData = new EventHandler<WaveInEventArgs>(waveInStream_DataAvailable);
				FWaveIn.DataAvailable += handleData;

				UsedDevices.Add(deviceID);
				FStatus = "Device opened";
			}
			catch
			{
				FWaveIn = null;
				FStatus = "Audio open failed";
			}
        }

		private void closeDevice()
		{
			if (FWaveIn != null)
			{
				try
				{
					FWaveIn.StopRecording();
				}
				catch
				{
				}
				FWaveIn.Dispose();
				FWaveIn = null;
				UsedDevices.Remove(FDeviceID);
			}
		}

#else
        public void setDevice(MMDevice dev)
        {
            if (dev == FDevice)
                return;

            if (FIsRecording)
                return;

            FDevice = dev;
            closeDevice();

            FWaveIn = new WasapiCapture(dev);

            //WaveFormat monoFormat = new WaveFormat(44100, 1);
            //FWaveIn.WaveFormat = monoFormat;

            FWaveIn.StartRecording();
            handleData = new EventHandler<WaveInEventArgs>(waveInStream_DataAvailable);
            FWaveIn.DataAvailable += handleData;

            FStatus = "Device opened";
        }
#endif

        public void StartRecording()
        {
            if (System.IO.File.Exists(FTempFilename))
            {
                if (isFileLocked(FTempFilename))
                {
                    FStatus = "Cannot open file for writing (perhaps locked by other application?)";
                    FIsRecording = false;
                    return;
                }
                System.IO.File.Delete(FTempFilename);
                FStatus = "Replacing existing file.";
            }
            else
                FStatus = "Creating new file";

            if (!validFilename(FTempFilename))
            {
                FStatus = "Filename invalid";
                return;
            }
            writer = new WaveFileWriter(FTempFilename, FWaveIn.WaveFormat);

            FPosition = 0.0f;
            FIsRecording = true;
        }

        public void StopRecording()
        {
            closeRecorder();

            FStatus = "Recording saved to temporary location";
        }

        public void SaveRecording()
        {
			if (System.IO.File.Exists(FFilename))
				System.IO.File.Delete(FFilename);

            System.IO.File.Move(FTempFilename, FFilename);

        }

        public float GetLevel()
        {
            return FLevel;
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
            FClipCompression = lerp(FClipCompression, 1.0f, CompressionAttack);
            if (Gate > 0.0f)
                FGateCompression = lerp(FGateCompression, 0.0f, GateDecay);
            else
                FGateCompression = 1.0f;

            for (int i = 0; i < e.BytesRecorded / 2; i++)
            {

                current = BitConverter.ToInt16(e.Buffer, i * 2);
                amplified = (float)current * Amplification;
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
                Buffer.BlockCopy(amplifiedBytes, 0, e.Buffer, i * 2, 2);

                level += Math.Abs(amplified);
            }

            // calc levels
            levelUncompressed /= (float)e.BytesRecorded * Int16.MaxValue;
            level /= (float)e.BytesRecorded * Int16.MaxValue;
            FLevel = level;


            //if high level, open gate
            if (Gate != 0)
            {
                if (levelUncompressed > Gate)
                    FGateCompression = 1.0f;
            }

            //if high level, clip
            if (maxval > 0.0f)
                FClipCompression = lerp(FClipCompression, (float)(maxval / Int16.MaxValue), CompressionAttack);


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
