using Media.Jpeg;
using Media.Tiff;
using System.CommandLine;
using System.IO;
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
            default:
                Console.WriteLine($"{fileInfo.Name} is not supported.");
                break;
        }
    }
}, fileInfoArray);

await rootCommand.InvokeAsync(args);
