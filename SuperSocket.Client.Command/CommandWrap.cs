using Microsoft.Extensions.DependencyInjection;

namespace SuperSocket.Client.Command;

interface ICommandWrap
{
    ICommand InnerCommand { get; }
}

class AsyncCommandWrap<TPackageInfo, IPackageInterface, TAsyncCommand> : IAsyncCommand<TPackageInfo>, ICommandWrap
    where TPackageInfo : IPackageInterface
    where TAsyncCommand : IAsyncCommand<IPackageInterface>
{
    public TAsyncCommand InnerCommand { get; }

    public AsyncCommandWrap(TAsyncCommand command)
    {
        InnerCommand = command;
    }

    public AsyncCommandWrap(IServiceProvider serviceProvider)
    {
        InnerCommand = (TAsyncCommand)ActivatorUtilities.CreateInstance(serviceProvider, typeof(TAsyncCommand));
    }

    public async ValueTask ExecuteAsync(object sender, TPackageInfo package)
    {
        await InnerCommand.ExecuteAsync(sender, package);
    }

    ICommand ICommandWrap.InnerCommand
    {
        get { return InnerCommand; }
    }
}
