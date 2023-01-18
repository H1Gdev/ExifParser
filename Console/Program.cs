using Media;
using Media.IsoBmff;
using Media.Jpeg;
using Media.Tiff;
using System.Buffers.Binary;
using System.CommandLine;
using System.Text;

var fileInfoArray = new Argument<FileInfo[]>();

var rootCommand = new RootCommand();
rootCommand.AddArgument(fileInfoArray);

rootCommand.SetHandler(fileInfoArray =>
{
    foreach (var fileInfo in fileInfoArray)
    {
        Console.WriteLine($"{fileInfo.Name}");
        switch (fileInfo.Extension.ToLower())
        {
            case ".jpg":
            case ".jpeg":
                {
                    using var stream = fileInfo.OpenRead();
                    JpegParser.Parse(stream, (stream, markerCode, size) =>
                    {
                        Console.WriteLine($" {markerCode}({size})");
#if true
                        var endPos = stream.Position + (size - 2U);

                        if (markerCode == MarkerCode.APP1)
                        {
                            var buffer = new byte[6];
                            var read = stream.Read(buffer, 0, buffer.Length);
                            if (read < buffer.Length)
                                throw new EndOfStreamException("'Exif Identifier' is not enough.");
                            if (!Enumerable.SequenceEqual(buffer, Encoding.ASCII.GetBytes(new char[] { 'E', 'x', 'i', 'f', '\0', '\0' })))
                                throw new IOException("'Exif Identifier' is invalid.");

                            TiffParser.Parse(stream, (number, tag, type, value) =>
                            {
                                switch (type)
                                {
                                    case Media.Tiff.Type.ASCII:
                                        Console.WriteLine($"  {number} {tag} {type} {value.Length} {Encoding.ASCII.GetString(value)}");
                                        break;
                                    default:
                                        Console.WriteLine($"  {number} {tag} {type} {value.Length}");
                                        break;
                                }
                            });
                            stream.Seek(endPos, SeekOrigin.Begin);
                            return true;
                        }
#endif
                        return false;
                    });
                }
                break;
            case ".tiff":
            case ".tif":
            case ".nef":
            case ".nrw":
                {
                    using var stream = fileInfo.OpenRead();
                    TiffParser.Parse(stream, (number, tag, type, value) =>
                    {
                        switch (type)
                        {
                            case Media.Tiff.Type.ASCII:
                                Console.WriteLine($" {number} {tag} {type} {value.Length} {Encoding.ASCII.GetString(value)}");
                                break;
                            default:
                                Console.WriteLine($" {number} {tag} {type} {value.Length}");
                                break;
                        }
                    });
                }
                break;
            case ".heif":
            case ".heic":
                {
                    var exifItemIdList = new List<uint>();
                    var extentOffsetMap = new Dictionary<uint, ulong>();
                    var extentLengthMap = new Dictionary<uint, ulong>();

                    using var stream = fileInfo.OpenRead();
                    IsoBmffParser.Parse(stream, (stream, size, type, longSize, extendedType, container, dataSize) =>
                    {
                        Console.WriteLine($" {type}({size}, {longSize}, {dataSize})");
#if true
                        if (type.Equals("ftyp"))
                        {
                            if (dataSize %4 != 0UL)
                                throw new IOException("'ftyp' is invalid.");

                            // File Type Box
                            var buffer = new byte[dataSize];
                            var span = new Span<byte>(buffer);
                            var read = stream.Read(span);
                            if (read < span.Length)
                                throw new EndOfStreamException("'ftyp' is not enough.");

                            var majorBrand = Encoding.ASCII.GetString(span[..4]);
                            var minorVersion = BinaryPrimitives.ReadUInt32BigEndian(span[4..8]);
                            var compatibleBrands = new List<string>();
                            for (var i = 8; i < span.Length; i += 4)
                                compatibleBrands.Add(Encoding.ASCII.GetString(span.Slice(i, 4)));

                            Console.WriteLine($" -> {majorBrand} {minorVersion} ({string.Join(",", compatibleBrands)})");
                            return true;
                        }
                        else if (type.Equals("meta"))
                        {
                            // Meta Box
                            IsoBmffParser.ParseFullBox(stream, size, type, longSize, extendedType, container, dataSize, (stream, size, type, longSize, extendedType, container, version, flags, dataSize) =>
                            {
                                var buffer = new byte[dataSize];
                                var read = stream.Read(buffer, 0, buffer.Length);
                                if (read < buffer.Length)
                                    throw new EndOfStreamException("'meta' is not enough.");

                                using var dataStream = new MemoryStream(buffer);
                                while (dataStream.Position < dataStream.Length)
                                    IsoBmffParser.ParseBox(dataStream, type, (stream, size, type, longSize, extendedType, container, dataSize) =>
                                    {
                                        Console.WriteLine($"  {type}({size}, {longSize}, {dataSize}) {container}");
                                        var endPos = stream.Position + (long)dataSize;

                                        if (type.Equals("iloc"))
                                        {
                                            // Item Location Box
                                            IsoBmffParser.ParseFullBox(stream, size, type, longSize, extendedType, container, dataSize, (stream, size, type, longSize, extendedType, container, version, flags, dataSize) =>
                                            {
                                                var buffer = new byte[2];
                                                var read = stream.Read(buffer, 0, buffer.Length);
                                                if (read < buffer.Length)
                                                    throw new EndOfStreamException("'offset_size', 'length_size', 'base_offset_size', 'index_size' are not enough.");
                                                var offsetSize = (uint)(buffer[0] >> 4 & 0xf);
                                                var lengthSize = (uint)(buffer[0] & 0xf);
                                                var baseOffsetSize = (uint)(buffer[1] >> 4 & 0xf);
                                                var indexSize = (uint)(buffer[1] & 0xf);

                                                if (offsetSize != 0U && offsetSize != 4U && offsetSize != 8U)
                                                    throw new IOException("'offset_size' is invalid.");
                                                if (lengthSize != 0U && lengthSize != 4U && lengthSize != 8U)
                                                    throw new IOException("'length_size' is invalid.");
                                                if (baseOffsetSize != 0U && baseOffsetSize != 4U && baseOffsetSize != 8U)
                                                    throw new IOException("'base_offset_size' is invalid.");
                                                if (indexSize != 0U && indexSize != 4U && indexSize != 8U)
                                                    throw new IOException("'index_size' is invalid.");

                                                buffer = new byte[version < 2 ? 2 : 4];
                                                read = stream.Read(buffer, 0, buffer.Length);
                                                if (read < buffer.Length)
                                                    throw new EndOfStreamException("'item_count' is not enough.");
                                                Utility.ConvertArchitectureByte(buffer, false);
                                                var itemCount = version < 2 ? BitConverter.ToUInt16(buffer, 0) : BitConverter.ToUInt32(buffer, 0);

                                                for (var i = 0; i < itemCount; ++i)
                                                {
                                                    buffer = new byte[version < 2 ? 2 : 4];
                                                    read = stream.Read(buffer, 0, buffer.Length);
                                                    if (read < buffer.Length)
                                                        throw new EndOfStreamException("'item_ID' is not enough.");
                                                    Utility.ConvertArchitectureByte(buffer, false);
                                                    var itemId = version < 2 ? BitConverter.ToUInt16(buffer, 0) : BitConverter.ToUInt32(buffer, 0);

                                                    var constructionMethod = 0;
                                                    if (version == 1 || version == 2)
                                                    {
                                                        buffer = new byte[2];
                                                        read = stream.Read(buffer, 0, buffer.Length);
                                                        if (read < buffer.Length)
                                                            throw new EndOfStreamException("'construction_method' is not enough.");

                                                        constructionMethod = buffer[1] & 0xf;
                                                        if (constructionMethod != 0 && constructionMethod != 1 && constructionMethod != 2)
                                                            throw new IOException("'construction_method' is invalid.");
                                                    }

                                                    buffer = new byte[2];
                                                    read = stream.Read(buffer, 0, buffer.Length);
                                                    if (read < buffer.Length)
                                                        throw new EndOfStreamException("'data_reference_index' is not enough.");
                                                    Utility.ConvertArchitectureByte(buffer, false);
                                                    var dataReferenceIndex = BitConverter.ToUInt16(buffer, 0);

                                                    var baseOffset = 0UL;
                                                    if (baseOffsetSize > 0U)
                                                    {
                                                        buffer = new byte[baseOffsetSize];
                                                        read = stream.Read(buffer, 0, buffer.Length);
                                                        if (read < buffer.Length)
                                                            throw new EndOfStreamException("'base_offset' is not enough.");
                                                        Utility.ConvertArchitectureByte(buffer, false);
                                                        baseOffset = baseOffsetSize == 4U ? BitConverter.ToUInt32(buffer, 0) : BitConverter.ToUInt64(buffer, 0);
                                                    }

                                                    buffer = new byte[2];
                                                    read = stream.Read(buffer, 0, buffer.Length);
                                                    if (read < buffer.Length)
                                                        throw new EndOfStreamException("'extent_count' is not enough.");
                                                    Utility.ConvertArchitectureByte(buffer, false);
                                                    var extentCount = BitConverter.ToUInt16(buffer, 0);
                                                    if (extentCount < 1)
                                                        throw new IOException("'extent_count' is invalid.");

                                                    Console.WriteLine($"  -> {itemId} {(constructionMethod == 0 ? "file" : constructionMethod == 1 ? "idat" : "item")}[{dataReferenceIndex}] {baseOffset}");

                                                    for (var j = 0; j < extentCount; ++j)
                                                    {
                                                        var extentIndex = 0UL;
                                                        if (version == 1 || version == 2)
                                                        {
                                                            if (indexSize > 0U)
                                                            {
                                                                buffer = new byte[indexSize];
                                                                read = stream.Read(buffer, 0, buffer.Length);
                                                                if (read < buffer.Length)
                                                                    throw new EndOfStreamException("'extent_index' is not enough.");
                                                                Utility.ConvertArchitectureByte(buffer, false);
                                                                extentIndex = indexSize == 4U ? BitConverter.ToUInt32(buffer, 0) : BitConverter.ToUInt64(buffer, 0);
                                                            }
                                                        }

                                                        var extentOffset = 0UL;
                                                        if (offsetSize > 0U)
                                                        {
                                                            buffer = new byte[offsetSize];
                                                            read = stream.Read(buffer, 0, buffer.Length);
                                                            if (read < buffer.Length)
                                                                throw new EndOfStreamException("'extent_offset' is not enough.");
                                                            Utility.ConvertArchitectureByte(buffer, false);
                                                            extentOffset = offsetSize == 4U ? BitConverter.ToUInt32(buffer, 0) : BitConverter.ToUInt64(buffer, 0);
                                                        }

                                                        var extentLength = 0UL;
                                                        if (lengthSize > 0U)
                                                        {
                                                            buffer = new byte[lengthSize];
                                                            read = stream.Read(buffer, 0, buffer.Length);
                                                            if (read < buffer.Length)
                                                                throw new EndOfStreamException("'extent_length' is not enough.");
                                                            Utility.ConvertArchitectureByte(buffer, false);
                                                            extentLength = lengthSize == 4U ? BitConverter.ToUInt32(buffer, 0) : BitConverter.ToUInt64(buffer, 0);
                                                        }

                                                        extentOffsetMap.Add(itemId, baseOffset + extentOffset);
                                                        extentLengthMap.Add(itemId, extentLength);

                                                        Console.WriteLine($"   -> {extentIndex} {extentOffset} {extentLength}");
                                                    }
                                                }

                                                return true;
                                            });
                                            return true;
                                        }
                                        else if (type.Equals("iinf"))
                                        {
                                            // Item Info Box
                                            IsoBmffParser.ParseFullBox(stream, size, type, longSize, extendedType, container, dataSize, (stream, size, type, longSize, extendedType, container, version, flags, dataSize) =>
                                            {
                                                var buffer = new byte[version == 0 ? 2 : 4];
                                                var read = stream.Read(buffer, 0, buffer.Length);
                                                if (read < buffer.Length)
                                                    throw new EndOfStreamException("'entry_count' is not enough.");
                                                Utility.ConvertArchitectureByte(buffer, false);
                                                var entryCount = version == 0 ? BitConverter.ToUInt16(buffer, 0) : BitConverter.ToUInt32(buffer, 0);

                                                for (var i = 0; i < entryCount; ++i)
                                                {
                                                    // ItemInfoEntry
                                                    IsoBmffParser.ParseFullBox(stream, type, (stream, size, type, longSize, extendedType, container, version, flags, dataSize) =>
                                                    {
                                                        Console.WriteLine($"   {type}({size}, {longSize}, {dataSize}) {container}");

                                                        if (!type.Equals("infe"))
                                                            throw new IOException("'infe' is invalid.");

                                                        if (version == 0 || version == 1)
                                                        {
                                                            // TODO
                                                        }
                                                        else
                                                        {
                                                            var buffer = new byte[version == 2 ? 2 : 4];
                                                            var read = stream.Read(buffer, 0, buffer.Length);
                                                            if (read < buffer.Length)
                                                                throw new EndOfStreamException("'item_ID' is not enough.");
                                                            dataSize -= (ulong)buffer.Length;
                                                            Utility.ConvertArchitectureByte(buffer, false);
                                                            var itemId = version == 2 ? BitConverter.ToUInt16(buffer, 0) : BitConverter.ToUInt32(buffer, 0);

                                                            buffer = new byte[2];
                                                            read = stream.Read(buffer, 0, buffer.Length);
                                                            if (read < buffer.Length)
                                                                throw new EndOfStreamException("'item_protection_index' is not enough.");
                                                            dataSize -= (ulong)buffer.Length;
                                                            Utility.ConvertArchitectureByte(buffer, false);
                                                            var itemProtectionIndex = BitConverter.ToUInt16(buffer, 0);

                                                            buffer = new byte[4];
                                                            read = stream.Read(buffer, 0, buffer.Length);
                                                            if (read < buffer.Length)
                                                                throw new EndOfStreamException("'item_type' is not enough.");
                                                            dataSize -= (ulong)buffer.Length;
                                                            var itemType = Encoding.ASCII.GetString(buffer);
                                                            if (itemType.ToLower().Equals("exif"))
                                                                exifItemIdList.Add(itemId);

                                                            var buffer2 = new List<byte>();
                                                            while (true)
                                                            {
                                                                read = stream.ReadByte();
                                                                if (read < 0)
                                                                    throw new EndOfStreamException("'item_name' is not enough.");
                                                                dataSize -= 1UL;
                                                                if (read == '\0')
                                                                    break;
                                                                buffer2.Add((byte)read);
                                                            }
                                                            var itemName = Encoding.UTF8.GetString(buffer2.ToArray());

                                                            var additional = new StringBuilder();
                                                            if (itemType.ToLower().Equals("mime"))
                                                            {
                                                                buffer2 = new List<byte>();
                                                                while (true)
                                                                {
                                                                    read = stream.ReadByte();
                                                                    if (read < 0)
                                                                        throw new EndOfStreamException("'content_type' is not enough.");
                                                                    dataSize -= 1UL;
                                                                    if (read == '\0')
                                                                        break;
                                                                    buffer2.Add((byte)read);
                                                                }
                                                                var contentType = Encoding.UTF8.GetString(buffer2.ToArray());

                                                                var contentEncoding = string.Empty;
                                                                if (dataSize > 0UL)
                                                                {
                                                                    buffer2 = new List<byte>();
                                                                    while (true)
                                                                    {
                                                                        read = stream.ReadByte();
                                                                        if (read < 0)
                                                                            throw new EndOfStreamException("'content_encoding' is not enough.");
                                                                        dataSize -= 1UL;
                                                                        if (read == '\0')
                                                                            break;
                                                                        buffer2.Add((byte)read);
                                                                    }
                                                                    contentEncoding = Encoding.UTF8.GetString(buffer2.ToArray());
                                                                }

                                                                additional.Append(contentType);
                                                                if (!string.IsNullOrEmpty(contentEncoding))
                                                                    additional.Append(" ").Append(contentEncoding);
                                                            }
                                                            else if (itemType.ToLower().Equals("uri "))
                                                            {
                                                                buffer2 = new List<byte>();
                                                                while (true)
                                                                {
                                                                    read = stream.ReadByte();
                                                                    if (read < 0)
                                                                        throw new EndOfStreamException("'item_uri_type' is not enough.");
                                                                    dataSize -= 1UL;
                                                                    if (read == '\0')
                                                                        break;
                                                                    buffer2.Add((byte)read);
                                                                }
                                                                var itemUriType = Encoding.ASCII.GetString(buffer2.ToArray());

                                                                additional.Append(itemUriType);
                                                            }

                                                            Console.WriteLine($"   -> {itemId} {itemProtectionIndex} {itemType}({itemName}) {(additional.Length != 0 ? $"({additional})" : string.Empty)}");
                                                            return true;
                                                        }

                                                        return false;
                                                    });
                                                }
                                                return true;
                                            });
                                            return true;
                                        }
                                        return false;
                                    });
                                return true;
                            });
                            return true;
                        }
#endif
                        return false;
                    });

                    foreach (var exifItemId in exifItemIdList)
                    {
                        if (!extentOffsetMap.ContainsKey(exifItemId) || !extentLengthMap.ContainsKey(exifItemId))
                            continue;

                        var exifOffset = extentOffsetMap[exifItemId];
                        stream.Seek((long)exifOffset, SeekOrigin.Begin);

                        var buffer = new byte[4 + 6];
                        var span = buffer.AsSpan();
                        var read = stream.Read(span);
                        if (read < span.Length)
                            throw new EndOfStreamException("'Exif Identifier' is not enough.");

                        //if (Enumerable.SequenceEqual(buffer[..6], Encoding.ASCII.GetBytes(new char[] { 'E', 'x', 'i', 'f', '\0', '\0' })))
                        if (span[..6].SequenceEqual(Encoding.ASCII.GetBytes(new char[] { 'E', 'x', 'i', 'f', '\0', '\0' })))
                        {
                            // - "Exif\0\0"
                            // - `Byte Order` + `Tag Mark`
                            exifOffset += 6UL;
                        }
                        else if (BinaryPrimitives.ReadUInt32BigEndian(span[..4]) == 6U && span[4..10].SequenceEqual(Encoding.ASCII.GetBytes(new char[] { 'E', 'x', 'i', 'f', '\0', '\0' })))
                        {
                            // - `Header Length` (== 6)
                            // - "Exif\0\0"
                            // - `Byte Order` + `Tag Mark`
                            exifOffset += 4UL + 6UL;
                        }
                        else
                        {
                            // - `Byte Order` + `Tag Mark`
                        }
                        stream.Seek((long)exifOffset, SeekOrigin.Begin);

                        TiffParser.Parse(stream, (number, tag, type, value) =>
                        {
                            switch (type)
                            {
                                case Media.Tiff.Type.ASCII:
                                    Console.WriteLine($" {number} {tag} {type} {value.Length} {Encoding.ASCII.GetString(value)}");
                                    break;
                                default:
                                    Console.WriteLine($" {number} {tag} {type} {value.Length}");
                                    break;
                            }
                        });
                    }
                }
                break;
            default:
                Console.WriteLine($"{fileInfo.Name} is not supported.");
                break;
        }
    }
}, fileInfoArray);

await rootCommand.InvokeAsync(args);
