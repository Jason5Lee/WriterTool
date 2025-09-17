namespace Jason5Lee.WriterTool.Core.Config;

using CsToml;
using CsToml.Values;

[TomlSerializedObject]
public partial class Config
{
    [TomlValueOnSerialized(aliasName: "writer")]
    public WriterConfig? Writer { get; set; }

    [TomlValueOnSerialized(aliasName: "reject-detection")]
    public RejectDetectionConfig? RejectDetection { get; set; }

    [TomlValueOnSerialized(aliasName: "translation")]
    public TranslationConfig? Translation { get; set; }
}

[TomlSerializedObject]
public partial class WriterPromptConfig
{

    [TomlValueOnSerialized(aliasName: "system")]
    public string? System { get; set; }

    [TomlValueOnSerialized(aliasName: "instruction")]
    public string? Instruction { get; set; }

    [TomlValueOnSerialized(aliasName: "requirement")]
    public string? Requirement { get; set; }
}

[TomlSerializedObject]
public partial class WriterConfig
{
    [TomlValueOnSerialized(aliasName: "api-url")]
    public string? ApiUrl { get; set; }

    [TomlValueOnSerialized(aliasName: "api-key")]
    public string? ApiKey { get; set; }

    [TomlValueOnSerialized(aliasName: "model")]
    public string? Model { get; set; }

    [TomlValueOnSerialized(aliasName: "prompt")]
    public WriterPromptConfig? Prompt { get; set; }

    [TomlValueOnSerialized(aliasName: "retry")]
    public RetryConfig? Retry { get; set; }
}

[TomlSerializedObject]
public partial class RejectDetectionConfig
{
    [TomlValueOnSerialized(aliasName: "enable")]
    public bool Enable { get; set; }

    [TomlValueOnSerialized(aliasName: "api-url")]
    public string? ApiUrl { get; set; }

    [TomlValueOnSerialized(aliasName: "api-key")]
    public string? ApiKey { get; set; }

    [TomlValueOnSerialized(aliasName: "model")]
    public string? Model { get; set; }

    [TomlValueOnSerialized(aliasName: "sample-length")]
    public int? SampleLength { get; set; }

    [TomlValueOnSerialized(aliasName: "threshold")]
    public int? Threshold { get; set; }

    [TomlValueOnSerialized(aliasName: "custom-prompt")]
    public RejectDetectionCustomPromptConfig? CustomPrompt { get; set; }

    [TomlValueOnSerialized(aliasName: "retry")]
    public RetryConfig? Retry { get; set; }
}

[TomlSerializedObject]
public partial class RejectDetectionCustomPromptConfig
{
    [TomlValueOnSerialized(aliasName: "enable")]
    public bool Enable { get; set; }

    [TomlValueOnSerialized(aliasName: "prompt-before")]
    public string? PromptBefore { get; set; }

    [TomlValueOnSerialized(aliasName: "prompt-after")]
    public string? PromptAfter { get; set; }
}

[TomlSerializedObject]
public partial class TranslationConfig
{
    [TomlValueOnSerialized(aliasName: "enable")]
    public bool Enable { get; set; }

    [TomlValueOnSerialized(aliasName: "api-url")]
    public string? ApiUrl { get; set; }

    [TomlValueOnSerialized(aliasName: "api-key")]
    public string? ApiKey { get; set; }

    [TomlValueOnSerialized(aliasName: "model")]
    public string? Model { get; set; }

    [TomlValueOnSerialized(aliasName: "max-characters-per-segment")]
    public int? MaxCharactersPerSegment { get; set; }

    [TomlValueOnSerialized(aliasName: "skip-original")]
    public bool SkipOriginal { get; set; } = false;

    [TomlValueOnSerialized(aliasName: "lines-mismatched-delimiter")]
    public string? LinesMismatchedDelimiter { get; set; }

    [TomlValueOnSerialized(aliasName: "default-prompt")]
    public DefaultTranslationPromptConfig? DefaultPrompt { get; set; }

    [TomlValueOnSerialized(aliasName: "custom-prompt")]
    public CustomTranslationPromptConfig? CustomPrompt { get; set; }

    [TomlValueOnSerialized(aliasName: "retry")]
    public RetryConfig? Retry { get; set; }
}

[TomlSerializedObject]
public partial class DefaultTranslationPromptConfig
{
    [TomlValueOnSerialized(aliasName: "enable")]
    public bool Enable { get; set; }

    [TomlValueOnSerialized(aliasName: "language")]
    public string? Language { get; set; }
}

[TomlSerializedObject]
public partial class CustomTranslationPromptConfig
{
    [TomlValueOnSerialized(aliasName: "enable")]
    public bool Enable { get; set; }

    [TomlValueOnSerialized(aliasName: "has-instruction")]
    public bool HasInstruction { get; set; }

    [TomlValueOnSerialized(aliasName: "prompt-before-instruction")]
    public string? PromptBeforeInstruction { get; set; }

    [TomlValueOnSerialized(aliasName: "prompt-before")]
    public string? PromptBefore { get; set; }

    [TomlValueOnSerialized(aliasName: "prompt-after")]
    public string? PromptAfter { get; set; }
}

[TomlSerializedObject]
public partial class RetryConfig
{
    [TomlValueOnSerialized(aliasName: "enable")]
    public bool Enable { get; set; }

    [TomlValueOnSerialized(aliasName: "duration")]
    public object? Duration { get; set; }

    [TomlValueOnSerialized(aliasName: "max-retries")]
    public int? MaxRetries { get; set; }

    public static string BackoffDuration { get; } = "backoff";
}
