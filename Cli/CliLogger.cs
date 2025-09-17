using Jason5Lee.WriterTool.Core;

namespace Jason5Lee.WriterTool.Cli;

internal class CliLogger : ILogger
{
    public static CliLogger Instance { get; } = new();

    public void Log(string message)
    {
        Console.Error.WriteLine(message);
    }

    public Task LogAsync(string message)
    {
        return Console.Error.WriteLineAsync(message);
    }
}