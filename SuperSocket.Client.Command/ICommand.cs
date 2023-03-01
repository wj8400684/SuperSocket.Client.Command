
namespace Work;

public interface ICommand
{
    // empty interface
}

public interface IAsyncCommand<TClient, TPackageInfo> : ICommand
{
    ValueTask ExecuteAsync(TClient client, TPackageInfo package);
}
