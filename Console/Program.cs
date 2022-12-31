using System.CommandLine;

var fileInfoArray = new Argument<FileInfo[]>();

var rootCommand = new RootCommand();
rootCommand.AddArgument(fileInfoArray);

rootCommand.SetHandler(fileInfoArray =>
{
    foreach (var fileInfo in fileInfoArray)
        Console.WriteLine($"Hello, World! {fileInfo.Name}");
}, fileInfoArray);

await rootCommand.InvokeAsync(args);
