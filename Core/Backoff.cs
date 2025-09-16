namespace Jason5Lee.WriterTool.Core;

internal static class Backoff
{
    public static IEnumerable<int> GetDurationMillis()
    {
        yield return 1_000;
        yield return 5_000;
        yield return 10_000;
        yield return 15_000;
        yield return 30_000;
        yield return 45_000;

        while (true)
        {
            yield return 60_000;
        }
    }

    public static async Task<T> RetryUntilSuccessAsync<T>(string context, Func<Task<T>> func)
    {
        foreach (var duration in GetDurationMillis())
        {
            try
            {
                return await func();
            }
            catch (SystemException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[{context}] {ex.Message}. Retrying in {duration}ms...");
                await Task.Delay(duration);
            }
        }

        throw new ApplicationException($"[{context}] Failed to perform after retrying");
    }
}
