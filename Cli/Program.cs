using System.CommandLine;
using System.Text;
using CsToml;
using Jason5Lee.WriterTool.Cli;
using Jason5Lee.WriterTool.Core;

var rootCommand = new RootCommand("Writer tool for generating content using AI models");

var configOption = new Option<string>("--config")
{
    Required = true,
    Description = "Path to config file"
};
configOption.Aliases.Add("-c");
rootCommand.Options.Add(configOption);

var outputOption = new Option<string>("--output")
{
    Required = true,
    Description = "Path to output file"
};
outputOption.Aliases.Add("-o");
rootCommand.Options.Add(outputOption);

var translatedOption = new Option<string?>("--translated")
{
    Description = "Path to translation output file"
};
translatedOption.Aliases.Add("-ot");
rootCommand.Options.Add(translatedOption);

var parseResult = rootCommand.Parse(args);
if (parseResult.Errors.Count > 0)
{
    foreach (var error in parseResult.Errors)
    {
        Console.Error.WriteLine(error.Message);
    }

    return 1;
}

await RunWriter(parseResult.GetValue(configOption), parseResult.GetValue(outputOption), parseResult.GetValue(translatedOption));
return 0;

static async Task<int> RunWriter(string? configPath, string? outputPath, string? translatedPath)
{
    if (string.IsNullOrEmpty(configPath))
    {
        Console.WriteLine("Error: Config file not specified");
        return 1;
    }

    if (string.IsNullOrEmpty(outputPath))
    {
        Console.WriteLine("Error: Output path not specified");
        return 1;
    }

    if (!File.Exists(configPath))
    {
        Console.WriteLine($"Error: Config file not found at {configPath}");
        return 1;
    }

    var config = CsTomlSerializer.Deserialize<Config>(new BufferedStream(File.OpenRead(configPath)));
    if (config.Writer == null)
    {
        Console.WriteLine("Error: Writer section is missing from the config file");
        return 1;
    }

    if (config.Writer.Prompt == null)
    {
        Console.WriteLine("Error: Writer instruction is missing from the config file");
        return 1;
    }

    if (string.IsNullOrEmpty(config.Writer.Prompt.Instruction))
    {
        Console.WriteLine("Error: Writer prompt instruction is missing from the config file");
        return 1;
    }

    var writerActor = Utils.CreateAIActor("writer", config.Writer.ApiUrl, config.Writer.ApiKey, config.Writer.Model);

    RejectionDetection? rejectionDetection = null;
    if (config.RejectDetection?.Enable ?? false)
    {
        if (config.RejectDetection.Threshold == null)
        {
            throw new ArgumentException("`reject-detection` must have a threshold specified");
        }
        PromptSurrounding promptSurrounding = RejectionDetection.DefaultPromptSurrounding;
        if (config.RejectDetection.CustomPrompt?.Enable ?? false)
        {
            if (string.IsNullOrEmpty(config.RejectDetection.CustomPrompt.PromptBefore))
            {
                throw new ArgumentException("`reject-detection.custom-prompt` must have a prompt-before specified");
            }
            if (string.IsNullOrEmpty(config.RejectDetection.CustomPrompt.PromptAfter))
            {
                throw new ArgumentException("`reject-detection.custom-prompt` must have a prompt-after specified");
            }
            promptSurrounding = new PromptSurrounding(config.RejectDetection.CustomPrompt.PromptBefore, config.RejectDetection.CustomPrompt.PromptAfter);
        }

        var rejectDetectionActor = Utils.CreateAIActor("reject-detection", config.RejectDetection.ApiUrl, config.RejectDetection.ApiKey, config.RejectDetection.Model);
        rejectionDetection = new RejectionDetection(rejectDetectionActor, promptSurrounding, config.RejectDetection.SampleLength ?? int.MaxValue, config.RejectDetection.Threshold.Value);
    }

    Translation? translation = null;
    if (config.Translation?.Enable ?? false && translatedPath != null)
    {
        PromptSurrounding promptSurrounding;
        if (string.IsNullOrEmpty(config.Translation.LinesMismatchedDelimiter))
        {
            throw new ArgumentException("`translation` section of the config must have a lines-mismatched-delimiter specified");
        }

        if (config.Translation.DefaultPrompt?.Enable ?? false)
        {
            if (config.Translation.CustomPrompt?.Enable ?? false)
            {
                throw new ArgumentException("`translation` section of the config cannot have both `default-prompt` and `custom-prompt` enabled");
            }
            if (string.IsNullOrEmpty(config.Translation.DefaultPrompt.Language))
            {
                throw new ArgumentException("`translation.default-prompt` must have a language specified");
            }
            promptSurrounding = Translation.DefaultPromptSurrounding(config.Translation.DefaultPrompt.Language, config.Writer.Prompt.Instruction);
        }
        else
        {
            if (config.Translation.CustomPrompt == null)
            {
                throw new ArgumentException("`translation` section of the config must have either `default-prompt` or `custom-prompt` enabled");
            }
            if (string.IsNullOrEmpty(config.Translation.CustomPrompt.PromptBefore))
            {
                throw new ArgumentException("`translation.custom-prompt` must have a prompt-before specified");
            }
            if (string.IsNullOrEmpty(config.Translation.CustomPrompt.PromptAfter))
            {
                throw new ArgumentException("`translation.custom-prompt` must have a prompt-after specified");
            }

            var promptBefore = new StringBuilder();
            if (config.Translation.CustomPrompt.HasInstruction)
            {
                if (string.IsNullOrEmpty(config.Translation.CustomPrompt.PromptBeforeInstruction))
                {
                    throw new ArgumentException("`translation.custom-prompt` must have a prompt-before-instruction specified when `has-instruction` is true");
                }

                promptBefore.Append(config.Translation.CustomPrompt.PromptBeforeInstruction);
                promptBefore.Append(config.Writer.Prompt.Instruction);
            }

            promptBefore.Append(config.Translation.CustomPrompt.PromptBefore);
            promptBefore.AppendLine(config.Translation.CustomPrompt.PromptAfter);
            promptSurrounding = new PromptSurrounding(config.Translation.CustomPrompt.PromptBefore, config.Translation.CustomPrompt.PromptAfter);
        }

        var translationActor = Utils.CreateAIActor("translation", config.Translation.ApiUrl, config.Translation.ApiKey, config.Translation.Model);
        translation = new Translation(translationActor, promptSurrounding, config.Translation.MaxCharactersPerSegment ?? int.MaxValue, config.Translation.LinesMismatchedDelimiter);
    }

    using var httpClient = new HttpClient();
    var content = await new Writing(writerActor, rejectionDetection).Invoke(httpClient, config.Writer.Prompt.System, config.Writer.Prompt.Instruction + (config.Writer.Prompt.Requirement ?? ""));

    await File.WriteAllTextAsync(outputPath, content);
    if (translation != null && translatedPath != null)
    {
        using var translatedStream = new StreamWriter(translatedPath);
        await foreach (var translatedSegment in translation.Invoke(httpClient, content))
        {
            await translatedStream.WriteLineAsync(translatedSegment);
        }
    }

    return 0;
}
