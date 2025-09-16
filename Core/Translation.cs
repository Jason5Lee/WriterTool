using System.Text;

namespace Jason5Lee.WriterTool.Core;

public record class Translation(
    AIActor AIActor,
    PromptSurrounding PromptSurrounding,
    int MaxCharactersPerSegment, // TODO: doc comment: if you don't want limit, pass int.MaxValue
    string LinesMismatchedDelimiter
)
{
    public static string TranslatedTagBegin => "<translated>";
    public static string TranslatedTagEnd => "</translated>";

    public static PromptSurrounding DefaultPromptSurrounding(string language, string instruction) =>
        new(
            Before: $@"Now, I need you to translate a segment to {language}. FYI, the segment is part of the text written based on the following instruction.

<instruction>
{instruction}
</instruction>

Here is the segment, please translate it into {language}.

<segment>",
            After: $@"</segment>

Please output the translated {language} text within <translated></translated>, ensuring the line count matches the original."
        );
    public async IAsyncEnumerable<string> Invoke(HttpClient httpClient, string content)
    {
        var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        for (int start = 0, end = 0; end < lines.Length; start = end)
        {
            var length = lines[start].Length;
            for (end = start + 1; end < lines.Length; ++end)
            {
                var line = lines[end];
                if (length + line.Length > MaxCharactersPerSegment)
                    break;
                length += line.Length;
            }
            var prompt = PromptSurrounding.CreatePrompt(lines.AsSpan()[start..end]);
            var translated = await Backoff.RetryUntilSuccessAsync("translation", async () =>
            {
                var translateResponse = await AIActor.GetCompletionAsync(httpClient, null, prompt);
                var startIndex = translateResponse.IndexOf(TranslatedTagBegin);
                if (startIndex == -1)
                {
                    throw new ApplicationException("Translated tag not found");
                }
                startIndex += TranslatedTagBegin.Length;

                var endIndex = translateResponse.IndexOf(TranslatedTagEnd, startIndex);
                if (endIndex == -1)
                {
                    throw new ApplicationException("Translated tag not found");
                }

                return translateResponse[startIndex..endIndex];
            });

            var translatedLines = translated.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            var final = new StringBuilder();
            if (translatedLines.Length != end - start)
            {
                final.AppendLine(LinesMismatchedDelimiter);
                for (int i = start; i < end; ++i)
                {
                    final.AppendLine(lines[i]);
                    final.AppendLine();
                }

                if (translatedLines.Length > 0)
                {
                    final.AppendLine(translatedLines[0]);
                    for (int i = 1; i < translatedLines.Length; ++i)
                    {
                        final.AppendLine();
                        final.AppendLine(translatedLines[i]);
                    }
                }

                final.AppendLine(LinesMismatchedDelimiter);
            }
            else
            {
                for (int i = 0; i < translatedLines.Length; ++i)
                {
                    if (i > 0)
                    {
                        final.AppendLine();
                    }
                    final.AppendLine(lines[start + i]);
                    final.AppendLine();
                    final.AppendLine(translatedLines[i]);
                }
            }

            yield return final.ToString();
        }
    }
}