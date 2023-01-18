using System;
using System.IO;

namespace Media
{
    public static class Utility
    {
        public static void ConvertArchitectureByte(byte[] data, bool isLittleEndian)
        {
            if (BitConverter.IsLittleEndian != isLittleEndian)
                Array.Reverse(data);
        }

        public static void ConvertArchitectureByte(byte[] data, bool isLittleEndian, int bytes)
        {
            if (BitConverter.IsLittleEndian != isLittleEndian)
                for (var i = 0; i < data.Length / bytes; ++i)
                    Array.Reverse(data, i * bytes, bytes);
        }

        public static void SkipStream(Stream stream, ulong size)
        {
            if (stream.CanSeek)
                stream.Seek((long)size, SeekOrigin.Current);
            else
            {
                var buffer = new byte[size];
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length)
                    throw new EndOfStreamException("data is not enough.");
            }
        }
    }
}
