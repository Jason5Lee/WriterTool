using Jason5Lee.WriterTool.Core;

namespace Jason5Lee.WriterTool.Cli;

internal static class Utils
{
    public static AIActor CreateAIActor(string configPath, string? apiUrl, string? apiKey, string? model)
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
