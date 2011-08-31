
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

        void waveInStream_DataAvailable(object sender, WaveInEventArgs e)
        {
			float level = 0;

            float levelInner;
            for (int i = 1; i < e.BytesRecorded; i += 4)
            {
                levelInner = Math.Abs((float)e.Buffer[i]);
				level += levelInner * levelInner;
            }
			level /= (float)e.BytesRecorded;
			level = (float)Math.Sqrt(FLevel) / 255.0f;

			if (level < 100) // i.e. not NaN
				FLevel = level;
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
