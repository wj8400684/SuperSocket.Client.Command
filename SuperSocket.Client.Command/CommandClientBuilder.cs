﻿using Microsoft.Extensions.DependencyInjection;
using SuperSocket.ProtoBase;

namespace SuperSocket.Client.Command;

public interface ICommandClientBuilder<TReceivePackage>
    where TReceivePackage : class
{
    public ICommandClientBuilder<TReceivePackage> UsePackageEncoder<TPackageEncoder>()
     where TPackageEncoder : class, IPackageEncoder<TReceivePackage>;

    public ICommandClientBuilder<TReceivePackage> UseCommand(Action<CommandOptions> configurator);
}

public interface ICommandClientBuilder<TKey, TReceivePackage, TPipelineFilter> : ICommandClientBuilder<TReceivePackage>
    where TReceivePackage : class
    where TPipelineFilter : class, IPipelineFilter<TReceivePackage>
{
    public ICommandClientBuilder<TReceivePackage> UseDefaultClient();

    public ICommandClientBuilder<TReceivePackage> UseClient<TCommandClient>() where TCommandClient : EasyCommandClient<TKey, TReceivePackage>;
}

public class CommandClientBuilder<TKey, TReceivePackage, TPipelineFilter> : ICommandClientBuilder<TKey, TReceivePackage, TPipelineFilter>
    where TReceivePackage : class, IKeyedPackageInfo<TKey>
    where TPipelineFilter : class, IPipelineFilter<TReceivePackage>
{
    private readonly IServiceCollection _serviceContainer;

    public CommandClientBuilder(IServiceCollection serviceContainer)
    {
        _serviceContainer = serviceContainer;
        _serviceContainer.AddSingleton<IPipelineFilter<TReceivePackage>, TPipelineFilter>();
    }

    public ICommandClientBuilder<TReceivePackage> UseClient<TCommandClient>() where TCommandClient : EasyCommandClient<TKey, TReceivePackage>
    {
        _serviceContainer.AddSingleton<IEasyClient<TReceivePackage, TReceivePackage>, TCommandClient>();
        _serviceContainer.AddSingleton<IEasyClient<TReceivePackage>>(s => s.GetRequiredService<IEasyClient<TReceivePackage, TReceivePackage>>() as TCommandClient);
        return this;
    }

    public ICommandClientBuilder<TReceivePackage> UseDefaultClient()
    {
        _serviceContainer.AddSingleton<IEasyClient<TReceivePackage, TReceivePackage>, EasyCommandClient<TKey, TReceivePackage>>();
        _serviceContainer.AddSingleton<IEasyClient<TReceivePackage>>(s => s.GetRequiredService<IEasyClient<TReceivePackage, TReceivePackage>>() as EasyCommandClient<TKey, TReceivePackage>);
        return this;
    }

    public ICommandClientBuilder<TReceivePackage> UsePackageEncoder<TPackageEncoder>() where TPackageEncoder : class, IPackageEncoder<TReceivePackage>
    {
        _serviceContainer.AddSingleton<IPackageEncoder<TReceivePackage>, TPackageEncoder>();
        return this;
    }

    public ICommandClientBuilder<TReceivePackage> UseCommand(Action<CommandOptions> configurator)
    {
        _serviceContainer.AddSingleton<IPackageHandler<TKey, TReceivePackage>, CommandHandler<TKey, TReceivePackage>>();
        _serviceContainer.AddSingleton<IPackageHandler<TReceivePackage>>(s => s.GetRequiredService<IPackageHandler<TKey, TReceivePackage>>() as CommandHandler<TKey, TReceivePackage>);
        _serviceContainer.Configure(configurator);

        return this;
    }
}

public static class EasyCommandClientBuilderExtensions
{
    private static Type GetKeyType<TPackageInfo>()
    {
        var interfaces = typeof(TPackageInfo).GetInterfaces();
        var keyInterface = interfaces.FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IKeyedPackageInfo<>));

        if (keyInterface == null)
            throw new Exception($"The package type {nameof(TPackageInfo)} should implement the interface {typeof(IKeyedPackageInfo<>).Name}.");

        return keyInterface.GetGenericArguments().FirstOrDefault();
    }

    public static ICommandClientBuilder<TReceivePackage> AddCommandClient<TKey, TReceivePackage, TPipelineFilter>(this IServiceCollection serviceDescriptors)
        where TReceivePackage : class, IKeyedPackageInfo<TKey>
        where TPipelineFilter : class, IPipelineFilter<TReceivePackage>
    {
        return new CommandClientBuilder<TKey, TReceivePackage, TPipelineFilter>(serviceDescriptors);
    }

    public static ICommandClientBuilder<TReceivePackage> AddDefaultCommandClient<TReceivePackage, TPipelineFilter>(this IServiceCollection services)
        where TReceivePackage : class
    {
        var keyType = GetKeyType<TReceivePackage>();

        //使用 UseEasyCommandClient<TKey, TPackageInfo>(this IServiceCollection services)
        var useCommandMethod = typeof(CommandHandlerExtensions).GetMethod("AddDefaultCommandClient", new Type[] { typeof(IServiceCollection) });
        useCommandMethod = useCommandMethod.MakeGenericMethod(keyType, typeof(TReceivePackage), typeof(TPipelineFilter));

        return (ICommandClientBuilder<TReceivePackage>)useCommandMethod.Invoke(null, new object[] { services });
    }

    public static ICommandClientBuilder<TReceivePackage> AddDefaultCommandClient<TKey, TReceivePackage, TPipelineFilter>(this IServiceCollection serviceDescriptors)
        where TReceivePackage : class, IKeyedPackageInfo<TKey>
        where TPipelineFilter : class, IPipelineFilter<TReceivePackage>
    {
        return new CommandClientBuilder<TKey, TReceivePackage, TPipelineFilter>(serviceDescriptors).UseDefaultClient();
    }
}
