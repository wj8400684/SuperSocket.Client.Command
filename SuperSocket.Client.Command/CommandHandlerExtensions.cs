using Microsoft.Extensions.DependencyInjection;
using SuperSocket.ProtoBase;

namespace SuperSocket.Client.Command;

public static class CommandHandlerExtensions
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

    /// <summary>
    /// 使用command客户端
    /// IEasyClient<TPackageInfo, TPackageInfo>
    /// IEasyClient<TPackageInfo>
    /// IPackageHandler<TKey, TPackageInfo>
    /// IPackageHandler<TPackageInfo>
    /// </summary>
    /// <typeparam name="TPackageInfo"></typeparam>
    /// <typeparam name="TCommandClient"></typeparam>
    /// <param name="services"></param>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static IServiceCollection UseCommandClient<TPackageInfo, TCommandClient>(this IServiceCollection services, Action<CommandOptions> configurator)
        where TPackageInfo : class
    {
        var keyType = GetKeyType<TPackageInfo>();

        //使用 UseEasyCommandClient<TKey, TPackageInfo>(this IServiceCollection services)
        var useCommandMethod = typeof(CommandHandlerExtensions).GetMethod("UseEasyCommandClient", new Type[] { typeof(IServiceCollection) });
        useCommandMethod = useCommandMethod.MakeGenericMethod(keyType, typeof(TPackageInfo), typeof(TCommandClient));

        useCommandMethod.Invoke(null, new object[] { services });

        return services.Configure(configurator);
    }

    /// <summary>
    /// 使用command客户端
    /// IEasyClient<TPackageInfo, TPackageInfo>
    /// IEasyClient<TPackageInfo>
    /// IPackageHandler<TKey, TPackageInfo>
    /// IPackageHandler<TPackageInfo>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TPackageInfo"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection UseEasyCommandClient<TKey, TPackageInfo>(this IServiceCollection services)
        where TPackageInfo : class, IKeyedPackageInfo<TKey>
    {
        return services.UseCommandClient<TKey, TPackageInfo, EasyCommandClient<TKey, TPackageInfo>>();
    }

    /// <summary>
    /// 使用command
    /// </summary>
    /// <typeparam name="TKey">命令</typeparam>
    /// <typeparam name="TPackageInfo">包</typeparam>
    /// <typeparam name="TCommandClient">客户端</typeparam>
    /// <typeparam name="TPipelineFilter">过滤器</typeparam>
    /// <typeparam name="TPackageEncoder">包编码</typeparam>
    /// <param name="services"></param>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static IServiceCollection UseCommandClient<TKey, TPackageInfo, TCommandClient, TPipelineFilter, TPackageEncoder>(this IServiceCollection services, Action<CommandOptions> configurator)
        where TPackageInfo : class, IKeyedPackageInfo<TKey>
        where TCommandClient : EasyCommandClient<TKey, TPackageInfo>
        where TPipelineFilter : class, IPipelineFilter<TPackageInfo>
        where TPackageEncoder : class, IPackageEncoder<TPackageInfo>
    {
        services.AddSingleton<IPackageEncoder<TPackageInfo>, TPackageEncoder>();
        services.AddSingleton<IPipelineFilter<TPackageInfo>, TPipelineFilter>();

        services.AddSingleton<TCommandClient>();
        services.AddSingleton<IEasyClient<TPackageInfo, TPackageInfo>>(s => s.GetRequiredService<TCommandClient>());
        services.AddSingleton<IEasyClient<TPackageInfo>>(s => s.GetRequiredService<TCommandClient>());

        services.AddSingleton<IPackageHandler<TKey, TPackageInfo>, CommandHandler<TKey, TPackageInfo>>();
        services.AddSingleton<IPackageHandler<TPackageInfo>>(s => s.GetRequiredService<IPackageHandler<TKey, TPackageInfo>>() as CommandHandler<TKey, TPackageInfo>);

        return services.Configure(configurator);
    }

    /// <summary>
    /// 使用command客户端
    /// IEasyClient<TPackageInfo, TPackageInfo>
    /// IEasyClient<TPackageInfo>
    /// IPackageHandler<TKey, TPackageInfo>
    /// IPackageHandler<TPackageInfo>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TPackageInfo"></typeparam>
    /// <typeparam name="TCommandClient"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection UseCommandClient<TKey, TPackageInfo, TCommandClient>(this IServiceCollection services)
        where TPackageInfo : class, IKeyedPackageInfo<TKey>
        where TCommandClient : EasyCommandClient<TKey, TPackageInfo>
    {
        services.AddSingleton<IEasyClient<TPackageInfo, TPackageInfo>, TCommandClient>();
        services.AddSingleton<IEasyClient<TPackageInfo>>(s => s.GetRequiredService<IEasyClient<TPackageInfo, TPackageInfo>>() as TCommandClient);

        services.AddSingleton<IPackageHandler<TKey, TPackageInfo>, CommandHandler<TKey, TPackageInfo>>();
        services.AddSingleton<IPackageHandler<TPackageInfo>>(s => s.GetRequiredService<IPackageHandler<TKey, TPackageInfo>>() as CommandHandler<TKey, TPackageInfo>);

        return services;
    }

    /// <summary>
    /// 使用command
    /// </summary>
    /// <typeparam name="TPackageInfo"></typeparam>
    /// <param name="services"></param>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static IServiceCollection UseCommand<TPackageInfo>(this IServiceCollection services, Action<CommandOptions> configurator)
        where TPackageInfo : class
    {
        var keyType = GetKeyType<TPackageInfo>();

        var useCommandMethod = typeof(CommandHandlerExtensions).GetMethod("UseCommand", new Type[] { typeof(IServiceCollection) });
        useCommandMethod = useCommandMethod.MakeGenericMethod(keyType, typeof(TPackageInfo));

        useCommandMethod.Invoke(null, new object[] { services });

        return services.Configure(configurator);
    }

    /// <summary>
    /// 使用command
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TPackageInfo"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection UseCommand<TKey, TPackageInfo>(this IServiceCollection services)
        where TPackageInfo : class, IKeyedPackageInfo<TKey>
    {
        services.AddSingleton<IPackageHandler<TKey, TPackageInfo>, CommandHandler<TKey, TPackageInfo>>();
        services.AddSingleton<IPackageHandler<TPackageInfo>>(s => s.GetRequiredService<IPackageHandler<TKey, TPackageInfo>>() as CommandHandler<TKey, TPackageInfo>);

        return services;
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
