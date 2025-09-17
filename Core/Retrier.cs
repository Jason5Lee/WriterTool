namespace Jason5Lee.WriterTool.Core;

public interface IRetrier
{
    public Task<T> Run<T>(ILogger logger, string context, Func<Task<T>> func);
}

public class NoRetryRetrier : IRetrier
{
    public static NoRetryRetrier Instance { get; } = new();
    public Task<T> Run<T>(ILogger logger, string context, Func<Task<T>> func) => func();
}

public class Retrier(IEnumerable<int> waitDurationMillis) : IRetrier
{
    public async Task<T> Run<T>(ILogger logger, string context, Func<Task<T>> func)
    {
        foreach (var duration in waitDurationMillis)
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
                await logger.LogAsync($"[{context}] {ex.Message}. Retrying in {duration}ms...");
                await Task.Delay(duration);
            }
        }
        throw new ApplicationException($"[{context}] Failed to perform after retrying");
    }

    public static IEnumerable<int> GetBackoffDurationMillis()
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
}