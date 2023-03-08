namespace SuperSocket.Client.Command;

public static class AsyncParallel
{
    public static async Task ForEach<TItem>(IEnumerable<TItem> source, Func<TItem, Task> operation, int maxDegreeOfParallelism = 5)
    {
        await ForEach(source, operation, new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = CancellationToken.None
        });
    }

    public static async Task ForEach<TItem>(IEnumerable<TItem> source, Func<TItem, Task> operation, ParallelOptions parallelOptions)
    {
        var allTasks = new List<Task>();
        var throttler = new SemaphoreSlim(parallelOptions.MaxDegreeOfParallelism);
        foreach (TItem item in source)
        {
            await throttler.WaitAsync(parallelOptions.CancellationToken);
            if (parallelOptions.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            allTasks.Add(Task.Run(async delegate
            {
                try
                {
                    await operation(item);
                }
                finally
                {
                    throttler.Release();
                }
            }, parallelOptions.CancellationToken));
        }

        await Task.WhenAll(allTasks);
    }
}
