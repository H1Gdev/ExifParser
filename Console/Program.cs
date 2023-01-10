using Media.Jpeg;
using System.CommandLine;

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
                        return false;
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
