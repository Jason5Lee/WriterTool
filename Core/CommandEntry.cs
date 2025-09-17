using System.Data.SqlTypes;
using System.Text;
using CsToml;

namespace Jason5Lee.WriterTool.Core;

public static class CommandEntry
{
    public static ValueTask<Config.Config> GetConfig(Stream configStream)
    {
        return CsTomlSerializer.DeserializeAsync<Config.Config>(configStream);
    }

    public static async Task<string> RunWriter(ILogger logger, Config.Config config)
    {
        if (config.Writer == null)
        {
            throw new ArgumentException("Error: Writer section is missing from the config file");
        }

        if (config.Writer.Prompt == null)
        {
            throw new ArgumentException("Error: Writer instruction is missing from the config file");
        }

        if (string.IsNullOrEmpty(config.Writer.Prompt.Instruction))
        {
            throw new ArgumentException("Error: Writer prompt instruction is missing from the config file");
        }

        var retrier = GetRetrierByConfig(config.Writer.Retry);
        var writerActor = CreateAIActor("writer", config.Writer.ApiUrl, config.Writer.ApiKey, config.Writer.Model);

        RejectionDetection? rejectionDetection = null;
        if (config.RejectDetection?.Enable ?? false)
        {
            if (config.RejectDetection.Threshold == null)
            {
                throw new ArgumentException("`reject-detection` must have a threshold specified");
            }

            var rdRetrier = GetRetrierByConfig(config.RejectDetection.Retry);
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

            var rejectDetectionActor = CreateAIActor("reject-detection", config.RejectDetection.ApiUrl, config.RejectDetection.ApiKey, config.RejectDetection.Model);
            rejectionDetection = new RejectionDetection(logger, rejectDetectionActor, rdRetrier, promptSurrounding, config.RejectDetection.SampleLength ?? int.MaxValue, config.RejectDetection.Threshold.Value);
        }

        using var httpClient = new HttpClient();
        return await new Writing(logger, writerActor, retrier, rejectionDetection).Invoke(httpClient, config.Writer.Prompt.System, config.Writer.Prompt.Instruction + (config.Writer.Prompt.Requirement ?? ""));
    }

    /// <summary>
    /// Verify the config and return a function that can be used to translate the content.
    /// It can verify the config before having the content to translate.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Func<HttpClient, string, IAsyncEnumerable<string>> GetTranslation(ILogger logger, Config.Config config)
    {
        if (config.Translation == null)
        {
            throw new ArgumentException("Error: Translation section is missing from the config file");
        }

        if (!config.Translation.SkipOriginal && string.IsNullOrEmpty(config.Translation.LinesMismatchedDelimiter))
        {
            throw new ArgumentException("`translation` section of the config must have a lines-mismatched-delimiter specified when `skip-original` is false");
        }

        var retrier = GetRetrierByConfig(config.Translation.Retry);
        PromptSurrounding promptSurrounding;
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
            var instruction = config.Writer?.Prompt?.Instruction;
            if (string.IsNullOrEmpty(instruction))
            {
                throw new ArgumentException("`writer.prompt.instruction` is required for default-prompt");
            }
            promptSurrounding = Translation.DefaultPromptSurrounding(config.Translation.DefaultPrompt.Language, instruction);
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
                var instruction = config.Writer?.Prompt?.Instruction;
                if (string.IsNullOrEmpty(instruction))
                {
                    throw new ArgumentException("`writer.prompt.instruction` is required for custom-prompt when `has-instruction` is true");
                }

                promptBefore.Append(config.Translation.CustomPrompt.PromptBeforeInstruction);
                promptBefore.Append(instruction);
            }

            promptBefore.Append(config.Translation.CustomPrompt.PromptBefore);
            promptBefore.AppendLine(config.Translation.CustomPrompt.PromptAfter);
            promptSurrounding = new PromptSurrounding(config.Translation.CustomPrompt.PromptBefore, config.Translation.CustomPrompt.PromptAfter);
        }

        var translationActor = CreateAIActor("translation", config.Translation.ApiUrl, config.Translation.ApiKey, config.Translation.Model);
        var translation = new Translation(
            logger,
            translationActor,
            retrier,
            promptSurrounding,
            config.Translation.MaxCharactersPerSegment ?? int.MaxValue,
            config.Translation.SkipOriginal,
            config.Translation.LinesMismatchedDelimiter ?? ""
        );
        return translation.Invoke;
    }

    private static IRetrier GetRetrierByConfig(Config.RetryConfig? retryConfig)
    {
        if (retryConfig == null || !retryConfig.Enable)
        {
            return NoRetryRetrier.Instance;
        }

        if (retryConfig.Duration == null)
        {
            throw new ArgumentException("`retry` must have a duration specified");
        }

        IEnumerable<int> durations;
        switch (retryConfig.Duration)
        {
            case long duration:
                if (duration < 0)
                {
                    throw new ArgumentException("`retry.duration` must be a non-negative integer");
                }
                if (duration > int.MaxValue)
                {
                    throw new ArgumentException("`retry.duration` must fit in an int");
                }
                durations = Enumerable.Repeat((int)duration, retryConfig.MaxRetries ?? int.MaxValue);
                break;
            case string durationString:
                if (durationString != Config.RetryConfig.BackoffDuration)
                {
                    throw new ArgumentException("`retry.duration` must be `backoff` if it is string");
                }
                durations = Retrier.GetBackoffDurationMillis();
                if (retryConfig.MaxRetries != null)
                {
                    durations = durations.Take(retryConfig.MaxRetries.Value);
                }
                break;
            default:
                throw new ArgumentException("`retry.duration` must be a non-negative integer or `backoff`");
        }

        return new Retrier(durations);
    }

    private static AIActor CreateAIActor(string configPath, string? apiUrl, string? apiKey, string? model)
    {
        if (apiUrl == null)
        {
            throw new ArgumentException($"Error: {configPath} section is missing api-url from the config file");
        }

        if (model == null)
        {
            throw new ArgumentException($"Error: {configPath} section is missing model from the config file");
        }

        return new AIActor(apiUrl, apiKey, model);
    }
}