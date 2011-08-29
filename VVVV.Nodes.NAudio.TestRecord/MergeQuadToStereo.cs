using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Utils;
using NAudio;
using NAudio.CoreAudioApi;

namespace NAudio.Wave
{
    /// <summary>
    /// Takes a quad 32 bit input and turns it stereo, mixing channels 1,2 into 1' and 2,3  into 2'
    /// </summary>
    public class QuadToStereoStream32 : WaveStream
    {
        WaveFormat FOutputFormat;
        WasapiCapture FSource;
        WaveBuffer FBufferIn;
        int FBufferCount;

        /// <summary>
        /// Creates a new stereo waveprovider based on a quad input
        /// </summary>
        /// <param name="sourceProvider">Quad 32 bit PCM input</param>
        public QuadToStereoStream32(WasapiCapture sourceProvider)
        {
            //if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            //{
            //    throw new ArgumentException("Source must be PCM");
            //}
            if (sourceProvider.WaveFormat.Channels != 4)
            {
                throw new ArgumentException("Source must be quad");
            }
            if (sourceProvider.WaveFormat.BitsPerSample != 32)
            {
                throw new ArgumentException("Source must be 32 bit");
            }
            this.FSource = sourceProvider;
            FOutputFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 32, 2);
        }

        public override WaveFormat WaveFormat
        {
            get { return FOutputFormat; }
        }

        public void Write(byte[] buffer, int count)
        {
            FBufferIn = new WaveBuffer(buffer);
            FBufferCount = count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int sourceBytesRequired = count * 2;
            WaveBuffer destWaveBuffer = new WaveBuffer(buffer);

            int samplesRead = FBufferCount / 4;
            int destOffset = offset / 4;

            short ch1, ch2, ch3, ch4;
            short outSample1, outSample2;

            for (int sample = 0; sample < samplesRead; sample += 4)
            {
                ch1 = FBufferIn.ShortBuffer[sample];
                ch2 = FBufferIn.ShortBuffer[sample + 1];

                ch3 = FBufferIn.ShortBuffer[sample + 2];
                ch4 = FBufferIn.ShortBuffer[sample + 3];

                outSample1 = ch1;// (ch1 + ch2) / 2;
                outSample2 = 0;// *ch3;// (ch3 + ch4) / 2;

                destWaveBuffer.ShortBuffer[destOffset++] = outSample1;
                destWaveBuffer.ShortBuffer[destOffset++] = outSample2;
            }
            return FBufferCount / 2; // bytes returned
        }

        public override void Close()
        {
            base.Close();
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
