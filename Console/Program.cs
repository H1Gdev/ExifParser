using System.CommandLine;

var fileInfo = new Argument<FileInfo>();

var rootCommand = new RootCommand();
rootCommand.AddArgument(fileInfo);

rootCommand.SetHandler((fileInfo) =>
{
    Console.WriteLine("Hello, World!");
}, fileInfo);

await rootCommand.InvokeAsync(args);
