namespace Jason5Lee.WriterTool.Core;

public interface ILogger
{
    void Log(string message);
    Task LogAsync(string message);
}
