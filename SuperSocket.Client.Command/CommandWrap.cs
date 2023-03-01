using Microsoft.Extensions.DependencyInjection;

namespace Work;

interface ICommandWrap
{
    ICommand InnerCommand { get; }
}

class AsyncCommandWrap<TClient, TPackageInfo, IPackageInterface, TAsyncCommand> : IAsyncCommand<TClient, TPackageInfo>, ICommandWrap
    where TPackageInfo : IPackageInterface
    where TAsyncCommand : IAsyncCommand<TClient, IPackageInterface>
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

    public async ValueTask ExecuteAsync(TClient client, TPackageInfo package)
    {
        await InnerCommand.ExecuteAsync(client, package);
    }

    ICommand ICommandWrap.InnerCommand
    {
        get { return InnerCommand; }
    }
}
