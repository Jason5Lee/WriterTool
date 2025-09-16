using System.Text;

namespace Jason5Lee.WriterTool.Core;

public readonly record struct PromptSurrounding(
    string Before,
    string After
)
{
    public string CreatePrompt(string content) =>
        Before + content + After;

    public string CreatePrompt(ReadOnlySpan<string> content)
    {
        var builder = new StringBuilder();
        builder.Append(Before);
        foreach (var line in content)
        {
            builder.AppendLine(line);
        }

        builder.Append(After);
        return builder.ToString();
    }
}