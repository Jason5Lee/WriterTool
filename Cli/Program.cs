using System.CommandLine;
using System.Text;
using CsToml;
using Jason5Lee.WriterTool.Cli;
using Jason5Lee.WriterTool.Core;

var rootCommand = new RootCommand("Writer tool for generating content using AI models");
var writeCommand = new Command("write", "Write content");
var configOption = new Option<string>("--config")
{
    Required = true,
    Description = "Path to config file"
};
configOption.Aliases.Add("-c");
writeCommand.Options.Add(configOption);

var outputOption = new Option<string>("--output")
{
    Required = true,
    Description = "Path to output file"
};
outputOption.Aliases.Add("-o");
writeCommand.Options.Add(outputOption);

var writerTranslatedOption = new Option<string?>("--translated")
{
    Description = "Path to translation output file"
};
writerTranslatedOption.Aliases.Add("-ot");
writeCommand.Options.Add(writerTranslatedOption);

rootCommand.Add(writeCommand);

var translateCommand = new Command("translate", "Translate content");
translateCommand.Options.Add(configOption);

var translationInputOption = new Option<string?>("--input")
{
    Required = true,
    Description = "Path to translation input file"
};
translationInputOption.Aliases.Add("-i");
translateCommand.Options.Add(translationInputOption);

translateCommand.Options.Add(outputOption);
rootCommand.Add(translateCommand);

var parseResult = rootCommand.Parse(args);
if (parseResult.Errors.Count > 0)
{
    foreach (var error in parseResult.Errors)
    {
        Console.Error.WriteLine(error.Message);
    }

    return 1;
}

if (parseResult.CommandResult.Command == writeCommand)
{
    var configPath = parseResult.GetValue(configOption);
    if (string.IsNullOrEmpty(configPath))
    {
        Console.Error.WriteLine("Error: Config file not specified");
        return 1;
    }

    var config = await CommandEntry.GetConfig(File.OpenRead(configPath));

    var outputPath = parseResult.GetValue(outputOption);
    if (string.IsNullOrEmpty(outputPath))
    {
        Console.Error.WriteLine("Error: Output path not specified");
        return 1;
    }

    var translatedPath = parseResult.GetValue(writerTranslatedOption);
    Func<HttpClient, string, IAsyncEnumerable<string>>? translate = null;
    if (!string.IsNullOrEmpty(translatedPath))
    {
        translate = CommandEntry.GetTranslation(CliLogger.Instance, config);
    }

    var content = await CommandEntry.RunWriter(CliLogger.Instance, config);
    await File.WriteAllTextAsync(outputPath, content);
    if (translate != null && translatedPath != null)
    {
        using var translatedStream = new StreamWriter(translatedPath);
        using var httpClient = new HttpClient();
        await foreach (var translatedSegment in translate(httpClient, content))
        {
            await translatedStream.WriteLineAsync(translatedSegment);
        }
    }
}
else if (parseResult.CommandResult.Command == translateCommand)
{
    var configPath = parseResult.GetValue(configOption);
    if (string.IsNullOrEmpty(configPath))
    {
        Console.Error.WriteLine("Error: Config file not specified");
        return 1;
    }

    var config = await CommandEntry.GetConfig(File.OpenRead(configPath));
    var inputPath = parseResult.GetValue(translationInputOption);
    if (string.IsNullOrEmpty(inputPath))
    {
        Console.Error.WriteLine("Error: Input path not specified");
        return 1;
    }

    var outputPath = parseResult.GetValue(outputOption);
    if (string.IsNullOrEmpty(outputPath))
    {
        Console.Error.WriteLine("Error: Output path not specified");
        return 1;
    }

    var input = await File.ReadAllTextAsync(inputPath);
    var translate = CommandEntry.GetTranslation(CliLogger.Instance, config);
    using var translatedStream = new StreamWriter(outputPath);
    using var httpClient = new HttpClient();
    await foreach (var translatedSegment in translate(httpClient, input))
    {
        await translatedStream.WriteLineAsync(translatedSegment);
    }
}
else
{
    Console.Error.WriteLine("Invalid command");
    return 1;
}

return 0;
