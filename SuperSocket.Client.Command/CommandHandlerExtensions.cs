using Microsoft.Extensions.DependencyInjection;
using SuperSocket.ProtoBase;

namespace Work;

public static class CommandHandlerExtensions
{
    public static Type GetKeyType<TPackageInfo>()
    {
        var interfaces = typeof(TPackageInfo).GetInterfaces();
        var keyInterface = interfaces.FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IKeyedPackageInfo<>));

        if (keyInterface == null)
            throw new Exception($"The package type {nameof(TPackageInfo)} should implement the interface {typeof(IKeyedPackageInfo<>).Name}.");

        return keyInterface.GetGenericArguments().FirstOrDefault();
    }

    public static IServiceCollection UseCommand<TClient, TPackageInfo>(this IServiceCollection services, Action<CommandOptions> configurator)
        where TPackageInfo : class
    {
        var keyType = GetKeyType<TPackageInfo>();

        var useCommandMethod = typeof(CommandHandlerExtensions).GetMethod("UseCommand", new Type[] { typeof(IServiceCollection) });
        useCommandMethod = useCommandMethod.MakeGenericMethod(typeof(TClient), keyType, typeof(TPackageInfo));

        useCommandMethod.Invoke(null, new object[] { services });

        return services.Configure(configurator);
    }

    public static IServiceCollection UseCommand<TClient, TKey, TPackageInfo>(this IServiceCollection services)
        where TPackageInfo : class, IKeyedPackageInfo<TKey>
        where TClient : class
    {
        return services.AddSingleton<IPackageHandler<TClient, TKey, TPackageInfo>, CommandHandler<TClient, TKey, TPackageInfo>>();
    }


    //public static IServiceCollection UseCommand<TKey, TPackageInfo>(this ISuperSocketHostBuilder<TPackageInfo> builder, Action<CommandOptions> configurator, IEqualityComparer<TKey> comparer)
    //    where TPackageInfo : class, IKeyedPackageInfo<TKey>
    //{
    //    return builder.UseCommand(configurator)
    //        .ConfigureServices((hostCtx, services) =>
    //        {
    //            services.AddSingleton<IEqualityComparer<TKey>>(comparer);
    //        }) as ISuperSocketHostBuilder<TPackageInfo>;
    //}

    //public static ISuperSocketHostBuilder<TPackageInfo> UseCommand<TKey, TPackageInfo>(this ISuperSocketHostBuilder builder)
    //    where TPackageInfo : class, IKeyedPackageInfo<TKey>
    //{
    //    return builder.UseMiddleware<CommandMiddleware<TKey, TPackageInfo>>()
    //        .ConfigureCommand() as ISuperSocketHostBuilder<TPackageInfo>;
    //}

    //public static ISuperSocketHostBuilder<TPackageInfo> UseCommand<TKey, TPackageInfo>(this ISuperSocketHostBuilder builder, Action<CommandOptions> configurator)
    //    where TPackageInfo : class, IKeyedPackageInfo<TKey>
    //{
    //    return builder.UseCommand<TKey, TPackageInfo>()
    //       .ConfigureServices((hostCtx, services) =>
    //       {
    //           services.Configure(configurator);
    //       }) as ISuperSocketHostBuilder<TPackageInfo>;
    //}

    //public static ISuperSocketHostBuilder<TPackageInfo> UseCommand<TKey, TPackageInfo>(this ISuperSocketHostBuilder builder, Action<CommandOptions> configurator, IEqualityComparer<TKey> comparer)
    //    where TPackageInfo : class, IKeyedPackageInfo<TKey>
    //{
    //    return builder.UseCommand<TKey, TPackageInfo>(configurator)
    //        .ConfigureServices((hostCtx, services) =>
    //        {
    //            services.AddSingleton<IEqualityComparer<TKey>>(comparer);
    //        }) as ISuperSocketHostBuilder<TPackageInfo>;
    //}
}
