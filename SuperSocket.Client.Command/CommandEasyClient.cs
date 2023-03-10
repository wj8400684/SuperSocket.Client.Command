using Microsoft.Extensions.Logging;
using SuperSocket.ProtoBase;
using System.Net;

namespace SuperSocket.Client.Command;

public class EasyCommandClient<Tkey, TPackage> : EasyClient<TPackage, TPackage>
    where TPackage : class
{
    private int _regConnectiuonCount;
    private readonly Timer _regConnectionTimer;
    private readonly TimeSpan _regConnectionTime = TimeSpan.FromSeconds(10);
    protected readonly IPackageHandler<Tkey, TPackage> PackageCommandHandler;

    public EasyCommandClient(
        IPackageHandler<Tkey, TPackage> packageHandler,
        IPipelineFilter<TPackage> pipelineFilter,
        IPackageEncoder<TPackage> packageEncoder,
        ILogger logger)
        : base(pipelineFilter, packageEncoder, logger)
    {
        PackageCommandHandler = packageHandler;
        PackageHandler += new PackageHandler<TPackage>(OnPackageHandlerAsync);
        _regConnectionTimer = new Timer(OnRegConnectionCallBack, null, Timeout.Infinite, Timeout.Infinite);
    }

    public CancellationTokenSource CancellationTokenSource { get; private set; }

    protected override async ValueTask<bool> ConnectAsync(EndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        var result = false;

        Closed += OnClosed;

        try
        {

            result = await base.ConnectAsync(remoteEndPoint, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new ConnectException($"连接服务器失败 {remoteEndPoint}", ex);
        }
        finally
        {
            if (!result)
                Closed -= OnClosed;
        }

        if (!result)
            return false;

        base.StartReceive();

        CancellationTokenSource = new CancellationTokenSource();

        return true;
    }

    /// <summary>
    /// 已经断开连接
    /// </summary>
    /// <returns></returns>
    protected virtual ValueTask OnClosedAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 重新连接中
    /// </summary>
    /// <returns></returns>
    protected virtual ValueTask<bool> OnRegConnectioningAsync()
    {
        return new ValueTask<bool>(false);
    }

    /// <summary>
    /// 重新连接完成
    /// </summary>
    /// <returns></returns>
    protected virtual ValueTask OnRegConnectionedAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual async ValueTask OnPackageHandlerAsync(EasyClient<TPackage> sender, TPackage package)
    {
        try
        {
            await PackageCommandHandler.HandleAsync(this, package);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"命令：{package}");
        }
    }

    /// <summary>
    /// 连接断开
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    async void OnClosed(object sender, EventArgs e)
    {
        Closed -= OnClosed;

        try
        {
            await OnClosedAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "");
        }

        CancellationTokenSource?.Cancel();
        CancellationTokenSource?.Dispose();
        _regConnectionTimer.Change(0, 0);
    }

    /// <summary>
    /// 重新连接回调
    /// </summary>
    /// <param name="state"></param>
    async void OnRegConnectionCallBack(object state)
    {
        _regConnectionTimer.Change(Timeout.Infinite, Timeout.Infinite);

        try
        {
            if (CancellationTokenSource == null || !CancellationTokenSource.IsCancellationRequested)//已经断开
                return;

            Logger.LogInformation($"正在重新连接{_regConnectiuonCount}次");

            _regConnectiuonCount++;
            var result = await OnRegConnectioningAsync();

            if (result)
            {
                _regConnectiuonCount = 0;
                await OnRegConnectionedAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"重新连接{_regConnectiuonCount}次服务器抛出一个异常");
        }
        finally
        {
            _regConnectionTimer.Change(_regConnectionTime, _regConnectionTime);
        }
    }
}
