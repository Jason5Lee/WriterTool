namespace Jason5Lee.WriterTool.Core;

public record class Writing(
    AIActor AIActor,
    RejectionDetection? RejectionDetection
)
{
    public Task<string> Invoke(HttpClient httpClient, string? system, string user)
    {
        return Backoff.RetryUntilSuccessAsync("writing", async () =>
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