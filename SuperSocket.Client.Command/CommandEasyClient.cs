using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperSocket.ProtoBase;
using System.Net;
namespace SuperSocket.Client.Command;

public class EasyCommandClient<Tkey, TPackage> : EasyClient<TPackage, TPackage>
    where TPackage : class 
{
    private readonly IServiceProvider _serviceProvider;
    private IPackageHandler<Tkey, TPackage> _packageHandler = null!;

    public EasyCommandClient(
        IServiceProvider serviceProvider,
        IPipelineFilter<TPackage> pipelineFilter,
        IPackageEncoder<TPackage> packageEncoder,
        ILogger logger)
        : base(pipelineFilter, packageEncoder, logger)
    {
        _serviceProvider = serviceProvider;
        PackageHandler += new PackageHandler<TPackage>(OnPackageHandlerAsync);
    }

    protected override async ValueTask<bool> ConnectAsync(EndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        var result = await base.ConnectAsync(remoteEndPoint, cancellationToken);

        if (!result)
            return false;

        _packageHandler ??= _serviceProvider.GetRequiredService<IPackageHandler<Tkey, TPackage>>();

        base.StartReceive();

        return true;
    }

    protected virtual async ValueTask OnPackageHandlerAsync(EasyClient<TPackage> sender, TPackage package)
    {
        try
        {
            await _packageHandler.HandleAsync(this, package);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"命令：{package}");
        }
    }
}
