// Copyright (c) 2018-present, Facebook, Inc. 


using System.IO;

namespace Facebook.Audio
{
    /// <summary>
    /// A utility to write a simple wave header to a binary stream.
    /// Typical usage:
    /// <code>
    /// WavSimpleHeader header = new WavSimpleHeader();
    /// // Start of file
    /// header.WriteHeader(writer, sampleRate, numOfChannels, numBitsPerSample, WavSimpleHeader.WavFileFormat.FloatPcm);
    /// // write audio samples to the binary writer..
    /// // and then once done, update the header with the duration
    /// header.UpdateDuration(writer);
    /// </code> 
    /// </summary>
    public class WavSimpleHeader
    {
        public enum WavFileFormat
        {
            IntegerPcm = 1,
            FloatPcm = 3
        };

        private class WavFmtChunk
        {
            public readonly char[] fmtId = {'f', 'm', 't', ' '}; // 4 bytes, "fmt "
            public uint chunkSize; // 4 bytes, usually 16 (size of total elements after this)
            public ushort formatTag; // 2 bytes. 1 = PCM. 3 = float
            public ushort numChannels; // 2 bytes
            public uint sampleRate; // 4 bytes
            public uint bytesPerSecond; // 4 bytes
            public ushort blockAlign; // 2 bytes
            public ushort bitsPerSample; // 2 bytes

            public void set(uint fileSampleRate, ushort numOfChannels, ushort numBitsPerSample, WavFileFormat format)
            {
                var bytesPerSample = numBitsPerSample / 8;
                chunkSize = 16;
                formatTag = (ushort) format;
                numChannels = numOfChannels;
                sampleRate = fileSampleRate;
                bytesPerSecond = (uint) (numChannels * sampleRate * bytesPerSample);
                blockAlign = (ushort) ((ushort) bytesPerSample * numChannels);
                bitsPerSample = numBitsPerSample;
            }
        }

        private struct WavChunk
        {
            public char[] chunkId;
            public uint chunkSize;

            public static WavChunk MakeDataChunk(uint durationSamples, uint numOfChannels, uint numBitsPerSample)
            {
                var bytesPerSample = numBitsPerSample / 8;
                var res = new WavChunk
                {
                    chunkId = new[] {'d', 'a', 't', 'a'}, chunkSize = durationSamples * bytesPerSample * numOfChannels
                };
                return res;
            }
        };

        private static readonly char[] RiffId = {'R', 'I', 'F', 'F'}; // 4 bytes, "RIFF"
        private uint size_ = 0; // 4 bytes, size of file
        private static readonly char[] WaveId = {'W', 'A', 'V', 'E'}; // 4 bytes, "WAVE"
        private readonly WavFmtChunk fmt_ = new WavFmtChunk(); // see WavFmtChunk

        private long fileSizeEndMarker_;
        private long dataChunkStartMarker_;
        private long dataChunkEndMarker_;

        /// <summary>
        /// Write the header information to a stream. Typically called first before any data is written to the stream
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="fileSampleRate"></param>
        /// <param name="numOfChannels"></param>
        /// <param name="numBitsPerSample"></param>
        /// <param name="format"></param>
        public void WriteHeader(BinaryWriter writer, uint fileSampleRate, ushort numOfChannels, ushort numBitsPerSample,
            WavFileFormat format)
        {
            fmt_.set(fileSampleRate, numOfChannels, numBitsPerSample, format);

            writer.Write(RiffId);
            writer.Write(size_);
            fileSizeEndMarker_ = writer.BaseStream.Position;
            writer.Write(WaveId);
            writer.Write(fmt_.fmtId);
            writer.Write(fmt_.chunkSize);
            writer.Write(fmt_.formatTag);
            writer.Write(fmt_.numChannels);
            writer.Write(fmt_.sampleRate);
            writer.Write(fmt_.bytesPerSecond);
            writer.Write(fmt_.blockAlign);
            writer.Write(fmt_.bitsPerSample);

            dataChunkStartMarker_ = writer.BaseStream.Position;
            var dataChunk = WavChunk.MakeDataChunk(0 /* duration is unknown */, numOfChannels, numBitsPerSample);
            writer.Write(dataChunk.chunkId);
            writer.Write(dataChunk.chunkSize);
            dataChunkEndMarker_ = writer.BaseStream.Position;
        }

        /// <summary>
        /// Update the duration metadata. Must be called after all audio samples have been written.
        /// </summary>
        /// <param name="writer"></param>
        public void UpdateDuration(BinaryWriter writer)
        {
            var fileSize = (uint) writer.BaseStream.Length;
            
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write(fileSize - (uint) (fileSizeEndMarker_));
            writer.Seek((int) dataChunkStartMarker_, SeekOrigin.Begin);

            var dataChunk = new WavChunk
            {
                chunkId = new[] {'d', 'a', 't', 'a'}, chunkSize = (uint) (fileSize - dataChunkEndMarker_)
            };
            writer.Write(dataChunk.chunkId);
            writer.Write(dataChunk.chunkSize);
        }
    };
}
