
namespace SuperSocket.Client.Command;

public interface ICommand
{
    // empty interface
}

public interface IAsyncCommand<TPackageInfo> : ICommand
{
    ValueTask ExecuteAsync(object sender, TPackageInfo package);
}
