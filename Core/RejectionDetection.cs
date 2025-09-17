namespace Jason5Lee.WriterTool.Core;

public record class RejectionDetection(
    ILogger Logger,
    AIActor AIActor,
    IRetrier Retrier,
    PromptSurrounding PromptSurrounding,
    int SampleLength,
    int Threshold
)
{
    public static PromptSurrounding DefaultPromptSurrounding { get; } =
        new(
            Before: @"Given the sample text, rate how likely it is a rejection message on a scale from 0 to 100, and output the result between <output></output>.

<sample>",
            After: @"</sample>

Please output the likelihood of the text being a rejection message (0 to 100) within <output></output> tags."
        );

    public async Task<bool> Invoke(HttpClient httpClient, string content)
    {
        var sample = SampleLength < content.Length ? content[..SampleLength] : content;
        var prompt = PromptSurrounding.CreatePrompt(sample);
        var likelihood = await Retrier.Run(Logger, "rejection-detection", async () =>
        {
            var response = await AIActor.GetCompletionAsync(httpClient, null, prompt);
            var startIndex = response.IndexOf("<output>");
            if (startIndex == -1)
            {
                throw new ApplicationException("Output tag not found");
            }
            startIndex += "<output>".Length;
            var endIndex = response.IndexOf("</output>", startIndex);
            if (endIndex == -1)
            {
                throw new ApplicationException("Output tag not found");
            }
            var output = response[startIndex..endIndex];
            if (!int.TryParse(output, out var likelihood))
            {
                throw new ApplicationException("Output is not a number");
            }

            return likelihood;
        });

        return 100 - likelihood < Threshold;
    }
}