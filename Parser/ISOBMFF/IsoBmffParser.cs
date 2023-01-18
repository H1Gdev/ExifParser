using System;
using System.IO;
using System.Text;

namespace Media.IsoBmff
{
    public static class IsoBmffParser
    {
        public static void Parse(Stream stream, Func<Stream, uint, string, ulong, byte[], string, ulong, bool> boxHandler)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new InvalidOperationException("'stream' can not read.");

            try
            {
                while (stream.Position < stream.Length)
                    ParseBox(stream, string.Empty, boxHandler);
            }
            catch (ParseAbortException)
            {
            }
        }

        public static void ParseBox(Stream stream, string container, Func<Stream, uint, string, ulong, byte[], string, ulong, bool> boxHandler)
        {
            byte[] buffer;
            int read;

            // - Box
            //   - Header
            //     - Size
            //     - Type
            //     - LongSize
            //     - ExtendedType
            //   - Data

            var headerSize = 0UL;

            // Size
            buffer = new byte[4];
            read = stream.Read(buffer, 0, buffer.Length);
            if (read < buffer.Length)
                throw new EndOfStreamException("'Size' is not enough.");
            headerSize += (ulong)buffer.Length;
            Utility.ConvertArchitectureByte(buffer, false);
            var size = BitConverter.ToUInt32(buffer, 0);
            if (size != 0 && size != 1 && size < 4 + 4)
                throw new IOException("'Size' is invalid.");

            // Type
            read = stream.Read(buffer, 0, buffer.Length);
            if (read < buffer.Length)
                throw new EndOfStreamException("'Type' is not enough.");
            headerSize += (ulong)buffer.Length;
            var type = Encoding.ASCII.GetString(buffer);

            var longSize = 0UL;
            if (size == 1)
            {
                // LongSize
                buffer = new byte[8];
                read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length)
                    throw new EndOfStreamException("'LongSize' is not enough.");
                headerSize += (ulong)buffer.Length;
                Utility.ConvertArchitectureByte(buffer, false);
                longSize = BitConverter.ToUInt64(buffer, 0);
                if (longSize != 0U && longSize < 4U + 4U)
                    throw new IOException("'LongSize' is invalid.");
            }

            var extendedType = Array.Empty<byte>();
            if (type.Equals("uuid"))
            {
                // ExtendedType
                buffer = new byte[16];
                read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length)
                    throw new EndOfStreamException("'ExtendedType' is not enough.");
                headerSize += (ulong)buffer.Length;
                extendedType = buffer;
            }

            var bytesRemaining = (ulong)(stream.Length - stream.Position);
            var dataSize = size == 1 ? longSize : size;
            if (dataSize == 0UL)
                dataSize = bytesRemaining;
            dataSize -= headerSize;
            if (dataSize > bytesRemaining)
                throw new EndOfStreamException("'Data' is not enough.");

            if (!boxHandler(stream, size, type, longSize, extendedType, container, dataSize))
                // Data
                Utility.SkipStream(stream, dataSize);
        }

        public static void ParseFullBox(Stream stream, uint size, string type, ulong longSize, byte[] extendedType, string container, ulong dataSize, Func<Stream, uint, string, ulong, byte[], string, byte, uint, ulong, bool> fullBoxHandler)
        {
            // - FullBox
            //   - Header
            //     - Size
            //     - Type
            //     - LongSize
            //     - ExtendedType
            //   - Version
            //   - Flags
            //   - Data

            // Version
            var buffer = new byte[4];
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read < buffer.Length)
                throw new EndOfStreamException("'Version', 'Flags' are not enough.");
            var version = buffer[0];
            // Flags
            Utility.ConvertArchitectureByte(buffer, false);
            var flags = BitConverter.ToUInt32(buffer, 0) & 0xffffff;

            dataSize -= 4UL;
            if (!fullBoxHandler(stream, size, type, longSize, extendedType, container, version, flags, dataSize))
                // Data
                Utility.SkipStream(stream, dataSize);
        }

        public static void ParseFullBox(Stream stream, string container, Func<Stream, uint, string, ulong, byte[], string, byte, uint, ulong, bool> fullBoxHandler)
        {
            ParseBox(stream, container, (stream, size, type, longSize, extendedType, container, dataSize) =>
            {
                ParseFullBox(stream, size, type, longSize, extendedType, container, dataSize, fullBoxHandler);
                return true;
            });
        }
    }
}
