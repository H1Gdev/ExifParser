using System.CommandLine;
using System.Drawing;
using System.Text;

var fileInfo = new Argument<FileInfo>();

var rootCommand = new RootCommand();
rootCommand.AddArgument(fileInfo);

rootCommand.SetHandler((fileInfo) =>
{
    using var image = new Bitmap(fileInfo.FullName);

    // Data size per Count.
    var BytesPerType = Array.AsReadOnly(new ushort[] { 0, 1, 1, 2, 4, 8, 0, 1, 0, 4, 8 });

    // Enumerate Exif information.
    // https://learn.microsoft.com/windows/win32/gdiplus/-gdiplus-constant-image-property-tag-type-constants
    foreach (var item in image.PropertyItems)
    {
        var type = (Type)item.Type;
        Console.WriteLine($"Tag=0x{item.Id:X4}({(Tag)item.Id}),Type={type},Len={item.Len}");

        if (item.Value is null)
        {
            Console.Write("Value=null");
        }
        else
        {
            // PropertyItem.Len unit is [Byte].
            Console.Write("Value=");
            var bytes = BytesPerType[(int)type];
            switch (type)
            {
                case Type.SHORT:
                    for (var cnt = 0; cnt < item.Len / bytes; ++cnt)
                    {
                        Console.Write($"0x{BitConverter.ToUInt16(item.Value, bytes * cnt):X4}, ");
                    }
                    break;
                case Type.LONG:
                    for (var cnt = 0; cnt < item.Len / bytes; ++cnt)
                    {
                        Console.Write($"0x{BitConverter.ToUInt32(item.Value, bytes * cnt):X8}, ");
                    }
                    break;
                case Type.SLONG:
                    for (var cnt = 0; cnt < item.Len / bytes; ++cnt)
                    {
                        Console.Write($"0x{BitConverter.ToInt32(item.Value, bytes * cnt):X8}, ");
                    }
                    break;
                case Type.ASCII:
                    {
                        var ascii = Encoding.ASCII.GetString(item.Value, 0, item.Len);
                        ascii = ascii.Trim(new char[] { '\0' });

                        Console.Write($"{ascii}");
                    }
                    break;
                case Type.RATIONAL:
                    for (var cnt = 0; cnt < item.Len / bytes; ++cnt)
                    {
                        var numerator = BitConverter.ToUInt32(item.Value, bytes * cnt);
                        var denominator = BitConverter.ToUInt32(item.Value, (bytes * cnt) + (bytes >> 1));
                        if ((numerator == 0) || (denominator == 0))
                        {
                            Console.Write($"0({numerator}/{denominator}), ");
                        }
                        else
                        {
                            Console.Write($"{numerator}/{denominator}, ");
                        }
                    }
                    break;
                case Type.SRATIONAL:
                    for (var cnt = 0; cnt < item.Len / bytes; ++cnt)
                    {
                        var numerator = BitConverter.ToInt32(item.Value, bytes * cnt);
                        var denominator = BitConverter.ToInt32(item.Value, (bytes * cnt) + (bytes >> 1));
                        if ((numerator == 0) || (denominator == 0))
                        {
                            Console.Write($"0({numerator}/{denominator}), ");
                        }
                        else
                        {
                            Console.Write($"{numerator}/{denominator}, ");
                        }
                    }
                    break;
                case Type.UNDEFINED:
                    if ((Tag)item.Id == Tag.ExifVersion)
                    {
                        // There is no NULL terminator.
                        goto case Type.ASCII;
                    }
                    else if ((Tag)item.Id == Tag.MakerNote)
                    {
                        // Data format is maker dependent.
                        goto case Type.BYTE;
                    }
                    else if ((Tag)item.Id == Tag.ComponentsConfiguration)
                    {
                        goto case Type.BYTE;
                    }
                    else if (((Tag)item.Id == Tag.FileSource) || ((Tag)item.Id == Tag.SceneType) || ((Tag)item.Id == Tag.CFAPatter))
                    {
                        goto case Type.BYTE;
                    }
                    else if ((Tag)item.Id == Tag.PrintImageMatchingIFDPointer)
                    {
                        goto case Type.BYTE;
                    }
                    else
                    {
                        goto case Type.ASCII;
                    }
                case Type.BYTE:
                default:
                    foreach (var data in item.Value)
                    {
                        Console.Write($"0x{data:X2}, ");
                    }
                    break;
            }
            Console.WriteLine();
        }
    }
}, fileInfo);

await rootCommand.InvokeAsync(args);

enum Tag : ushort
{
    ExifIFDPointer = 0x8769,
    GPSInfoIFDPointer = 0x8825,
    InteroperabilityIFDPointer = 0xa005,
    ExifVersion = 0x9000,
    ComponentsConfiguration = 0x9101,
    MakerNote = 0x927c,
    FileSource = 0xa300,
    SceneType = 0xa301,
    CFAPatter = 0xa302,
    PrintImageMatchingIFDPointer = 0xc4a5,
}

enum Type : ushort
{
    BYTE = 1,
    ASCII = 2,
    SHORT = 3,
    LONG = 4,
    RATIONAL = 5,
    UNDEFINED = 7,
    SLONG = 9,
    SRATIONAL = 10,
}
