using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Media.Tiff
{
    public static class TiffParser
    {
        public static void Parse(Stream stream, Action<int, ushort, Type, byte[]> fieldEntryHandler)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new InvalidOperationException("'stream' can not read.");
            if (!stream.CanSeek)
                throw new InvalidOperationException("'stream' can not seek.");

            try
            {
                byte[] buffer;
                int read;

                // TIFF Header
                var tiffPosition = stream.Position;
                var isLittleEndian = true;

                // Byte Order
                buffer = new byte[2];
                read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length)
                    throw new EndOfStreamException("'Byte Order' is not enough.");
                if (Enumerable.SequenceEqual(buffer, Encoding.ASCII.GetBytes(new char[] { 'I', 'I' })))
                    isLittleEndian = true;
                else if (Enumerable.SequenceEqual(buffer, Encoding.ASCII.GetBytes(new char[] { 'M', 'M' })))
                    isLittleEndian = false;
                else
                    throw new IOException("'Byte Order' is invalid.");

                // Tag Mark
                read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length)
                    throw new EndOfStreamException("'Tag Mark' is not enough.");
                Utility.ConvertArchitectureByte(buffer, isLittleEndian);
                if (BitConverter.ToUInt16(buffer, 0) != 0x002aU)
                    throw new IOException("'Tag Mark' is invalid.");

                // Offset of IFD
                buffer = new byte[4];
                read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length)
                    throw new EndOfStreamException("'Offset of IFD' is not enough.");
                Utility.ConvertArchitectureByte(buffer, isLittleEndian);
                var offset = BitConverter.ToUInt32(buffer, 0);
                var number = 0;
                while (offset > 0U)
                {
                    // IFDs
                    // - 0th IFD
                    // - 1st IFD
                    // - 2nd IFD
                    // - ...
                    stream.Seek(tiffPosition + offset, SeekOrigin.Begin);
                    offset = ParseImageFileDirectory(stream, number++, tiffPosition, isLittleEndian, fieldEntryHandler);
                }
            }
            catch (ParseAbortException)
            {
            }
        }

        private static uint ParseImageFileDirectory(Stream stream, int number, long tiffPosition, bool isLittleEndian, Action<int, ushort, Type, byte[]> fieldEntryHandler)
        {
            byte[] buffer;
            int read;

            // Number of Fields
            buffer = new byte[2];
            read = stream.Read(buffer, 0, buffer.Length);
            if (read < buffer.Length)
                throw new EndOfStreamException("'Number of Fields' is not enough.");
            Utility.ConvertArchitectureByte(buffer, isLittleEndian);
            var fields = BitConverter.ToUInt16(buffer, 0);

            // Field Entry
            for (var i = 0; i < fields; ++i)
            {
                // Tag
                buffer = new byte[2];
                read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length)
                    throw new EndOfStreamException("'Tag' is not enough.");
                Utility.ConvertArchitectureByte(buffer, isLittleEndian);
                var tag = BitConverter.ToUInt16(buffer, 0);

                // Type
                read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length)
                    throw new EndOfStreamException("'Type' is not enough.");
                Utility.ConvertArchitectureByte(buffer, isLittleEndian);
                var type = (Type)BitConverter.ToUInt16(buffer, 0);

                // Count
                buffer = new byte[4];
                read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length)
                    throw new EndOfStreamException("'Count' is not enough.");
                Utility.ConvertArchitectureByte(buffer, isLittleEndian);
                var count = BitConverter.ToUInt32(buffer, 0);

                // Value Offset
                read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length)
                    throw new EndOfStreamException("'Value Offset' is not enough.");
                var size = type.GetBytesPerType() * count;
                if (size < 4)
                    Array.Resize(ref buffer, (int)size);
                else if (size > 4)
                {
                    // Value
                    var current = stream.Position;
                    try
                    {
                        Utility.ConvertArchitectureByte(buffer, isLittleEndian);
                        var valueOffset = BitConverter.ToUInt32(buffer, 0);
                        stream.Seek(tiffPosition + valueOffset, SeekOrigin.Begin);
                        buffer = new byte[size];
                        read = stream.Read(buffer, 0, buffer.Length);
                        if (read < buffer.Length)
                            throw new EndOfStreamException("'Value' is not enough.");
                    }
                    finally
                    {
                        stream.Position = current;
                    }
                }

                switch (type)
                {
                    case Type.SHORT:
                    case Type.SSHORT:
                        Utility.ConvertArchitectureByte(buffer, isLittleEndian, 2);
                        break;
                    case Type.LONG:
                    case Type.SLONG:
                    case Type.RATIONAL:
                    case Type.SRATIONAL:
                    case Type.FLOAT:
                        Utility.ConvertArchitectureByte(buffer, isLittleEndian, 4);
                        break;
                    case Type.DOUBLE:
                        Utility.ConvertArchitectureByte(buffer, isLittleEndian, 8);
                        break;
                    case Type.BYTE:
                    case Type.ASCII:
                    case Type.SBYTE:
                    case Type.UNDEFINED:
                    default:
                        break;
                }

                fieldEntryHandler(number, tag, type, buffer);
            }
            // Offset of Next IFD
            buffer = new byte[4];
            read = stream.Read(buffer, 0, buffer.Length);
            if (read < buffer.Length)
                throw new EndOfStreamException("'Offset of Next IFD' is not enough.");
            Utility.ConvertArchitectureByte(buffer, isLittleEndian);
            return BitConverter.ToUInt32(buffer, 0);
        }
    }
}
