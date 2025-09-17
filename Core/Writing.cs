namespace Jason5Lee.WriterTool.Core;

public record class Writing(
    ILogger Logger,
    AIActor AIActor,
    IRetrier Retrier,
    RejectionDetection? RejectionDetection
)
{
    public Task<string> Invoke(HttpClient httpClient, string? system, string user)
    {
        return Retrier.Run(Logger, "writing", async () =>
        {
            var content = await AIActor.GetCompletionAsync(httpClient, system, user);
            if (RejectionDetection != null && await RejectionDetection.Invoke(httpClient, content))
            {
                throw new ApplicationException("Rejection detected");
            }
            return content;
        });
    }
}