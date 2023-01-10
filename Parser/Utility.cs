using System;

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
    }
}
