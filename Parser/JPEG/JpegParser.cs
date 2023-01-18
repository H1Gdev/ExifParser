using System;
using System.IO;

namespace Media.Jpeg
{
    public static class JpegParser
    {
        public static void Parse(Stream stream, Func<Stream, MarkerCode, uint, bool> markerSegmentHandler, Action<Stream> scanHandler)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new InvalidOperationException("'stream' can not read.");

            try
            {
                var first = true;
                while (stream.Position < stream.Length)
                {
                    // Marker Code
                    var buffer = new byte[2];
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read < buffer.Length)
                        throw new EndOfStreamException("'Marker Code' is not enough.");
                    if ((MarkerCode)buffer[0] != MarkerCode.Prefix)
                        throw new IOException("'Marker Prefix' is invalid.");
                    var markerCode = (MarkerCode)buffer[1];

                    var size = 0U;
                    if (markerCode.HasData())
                        // Size
                        size = ParseSize(stream);

                    if (first)
                    {
                        if (markerCode != MarkerCode.SOI)
                            throw new IOException("SOI not exist.");
                        first = false;
                    }

                    var bytesRemaining = stream.Length - stream.Position;
                    if (size > bytesRemaining)
                        throw new EndOfStreamException("'Data' is not enough.");

                    if (!markerSegmentHandler(stream, markerCode, size))
                    {
                        if (markerCode.HasData())
                            // Data
                            Utility.SkipStream(stream, size - 2U);
                    }

                    if (markerCode == MarkerCode.SOS)
                    {
                        scanHandler(stream);
                        break;
                    }
                    if (markerCode == MarkerCode.EOI)
                        break;
                }
            }
            catch (ParseAbortException)
            {
            }
        }

        public static void Parse(Stream stream, Func<Stream, MarkerCode, uint, bool> markerSegmentHandler)
        {
            Parse(stream, (stream, markerCode, size) => markerSegmentHandler(stream, markerCode, size),
                stream =>
                {
                    // Scan
                    var foundPrefix = false;
                    while (stream.Position < stream.Length)
                    {
                        var read = stream.ReadByte();
                        if (read < 0)
                            throw new EndOfStreamException("'Scan' is not enough.");

                        // Marker Code
                        var markerCode = (MarkerCode)(byte)read;
                        if (markerCode == MarkerCode.Prefix)
                        {
                            foundPrefix = true;
                            continue;
                        }

                        if (foundPrefix && markerCode != 0x0)
                        {
                            var size = 0U;
                            if (markerCode.HasData())
                                // Size
                                size = ParseSize(stream);

                            if (!markerSegmentHandler(stream, markerCode, size))
                            {
                                if (markerCode.HasData())
                                    // Data
                                    Utility.SkipStream(stream, size - 2U);
                            }

                            if (markerCode == MarkerCode.EOI)
                                break;

                        }
                        foundPrefix = false;
                    }
                });
        }

        private static uint ParseSize(Stream stream)
        {
            var buffer = new byte[2];
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read < buffer.Length)
                throw new EndOfStreamException("'Size' is not enough.");
            Utility.ConvertArchitectureByte(buffer, false);
            var size = BitConverter.ToUInt16(buffer, 0);
            if (size < 2U)
                throw new IOException("'Size' is invalid.");

            return size;
        }
    }
}
