using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using SuperSocket.ProtoBase;
using Microsoft.Extensions.Logging;

namespace Work;

public interface IPackageHandler<TClient, TKey, TReceivePackageInfo> where TClient : class
{
    ValueTask HandleAsync(TClient client, TReceivePackageInfo package);

    ValueTask HandleAsync(TClient client, TReceivePackageInfo package, TKey key);
}

public class CommandHandler<TClient, TKey, TPackageInfo> : CommandHandler<TClient, TKey, TPackageInfo, TPackageInfo>
    where TPackageInfo : class, IKeyedPackageInfo<TKey>
    where TClient : class
{

    class TransparentMapper : IPackageMapper<TPackageInfo, TPackageInfo>
    {
        public TPackageInfo Map(TPackageInfo package)
        {
            return package;
        }
    }

    public CommandHandler(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions)
        : base(serviceProvider, commandOptions)
    {
    }

    protected override IPackageMapper<TPackageInfo, TPackageInfo> CreatePackageMapper(IServiceProvider serviceProvider)
    {
        return new TransparentMapper();
    }
}

public class CommandHandler<TClient, TKey, TNetPackageInfo, TPackageInfo> : IPackageHandler<TClient, TKey, TPackageInfo>
    where TPackageInfo : class, IKeyedPackageInfo<TKey>
    where TNetPackageInfo : class
    where TClient : class
{
    private readonly Dictionary<TKey, ICommandSet> _commands;

    private ILogger _logger;

    protected IPackageMapper<TNetPackageInfo, TPackageInfo> PackageMapper { get; private set; }

    public CommandHandler(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions)
        : this(serviceProvider, commandOptions, null)
    {
    }

    public CommandHandler(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions, IPackageMapper<TNetPackageInfo, TPackageInfo> packageMapper)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>()!.CreateLogger("CommandMiddleware");

        var commandInterfaces = new List<CommandTypeInfo>();
        var commandSetFactories = new List<ICommandSetFactory>();

        var ignorePackageInterfaces = new Type[] { typeof(IKeyedPackageInfo<TKey>) };
        var availablePackageTypes = typeof(TPackageInfo).GetTypeInfo()
            .GetInterfaces()
            .Where(f => !ignorePackageInterfaces.Contains(f))
            .ToList();

        availablePackageTypes.Add(typeof(TPackageInfo));

        var knownInterfaces = new Type[] { typeof(IKeyedPackageInfo<TKey>) };

        foreach (var pt in availablePackageTypes)
        {
            RegisterCommandInterfaces(commandInterfaces, commandSetFactories, serviceProvider, pt, true);
        }

        commandSetFactories.AddRange(commandOptions.Value.GetCommandTypes(t => true).Select((t) =>
        {
            if (t.IsAbstract)
                return null;

            for (var i = 0; i < commandInterfaces.Count; i++)
            {
                var face = commandInterfaces[i];

                if (face.CommandType.IsAssignableFrom(t))
                    return face.CreateCommandSetFactory(t);
            }

            return null;
        }).Where(t => t != null));


        var commands = commandSetFactories.Select(t => t.Create(serviceProvider, commandOptions.Value));
        var comparer = serviceProvider.GetService<IEqualityComparer<TKey>>();

        var commandDict = comparer == null ?
            new Dictionary<TKey, ICommandSet>() : new Dictionary<TKey, ICommandSet>(comparer);

        foreach (var cmd in commands)
        {
            if (commandDict.ContainsKey(cmd.Key))
            {
                var error = $"Duplicated command with Key {cmd.Key} is found: {cmd.ToString()}";
                _logger.LogError(error);
                throw new Exception(error);
            }

            commandDict.Add(cmd.Key, cmd);
            _logger.LogDebug($"The command with key {cmd.Key} is registered: {cmd.ToString()}");
        }

        _commands = commandDict;

        PackageMapper = packageMapper != null ? packageMapper : CreatePackageMapper(serviceProvider);
    }

    private void RegisterCommandInterfaces(List<CommandTypeInfo> commandInterfaces, List<ICommandSetFactory> commandSetFactories, IServiceProvider serviceProvider, Type packageType, bool wrapRequired = false)
    {
        var genericTypes = new[] { typeof(TClient), packageType };

        var asyncCommandInterface = typeof(IAsyncCommand<,>).GetTypeInfo().MakeGenericType(genericTypes);

        var commandSetFactoryType = typeof(CommandSetFactory);

        var asyncCommandType = new CommandTypeInfo(typeof(IAsyncCommand<,>).GetTypeInfo().MakeGenericType(genericTypes), commandSetFactoryType);

        commandInterfaces.Add(asyncCommandType);

        if (wrapRequired)
        {
            asyncCommandType.WrapRequired = true;
            asyncCommandType.WrapFactory = (t) =>
            {
                return typeof(AsyncCommandWrap<,,,>).GetTypeInfo().MakeGenericType(typeof(TClient), typeof(TPackageInfo), packageType, t);
            };
        }

        RegisterCommandSetFactoriesFromServices(commandSetFactories, serviceProvider, asyncCommandType.CommandType, commandSetFactoryType, asyncCommandType.WrapFactory);
    }

    private void RegisterCommandSetFactoriesFromServices(List<ICommandSetFactory> commandSetFactories, IServiceProvider serviceProvider, Type commandType, Type commandSetFactoryType, Func<Type, Type> commandWrapFactory)
    {
        foreach (var command in serviceProvider.GetServices(commandType).OfType<ICommand>())
        {
            var cmd = command;
            var actualCommandType = cmd.GetType();

            if (commandWrapFactory != null)
            {
                var commandWrapType = commandWrapFactory(command.GetType());
                cmd = ActivatorUtilities.CreateInstance(null, commandWrapType, command) as ICommand;
            }

            var commandTypeInfo = new CommandTypeInfo(cmd)
            {
                ActualCommandType = actualCommandType
            };

            commandSetFactories.Add(ActivatorUtilities.CreateInstance(null, commandSetFactoryType, commandTypeInfo) as ICommandSetFactory);
        }
    }

    protected virtual IPackageMapper<TNetPackageInfo, TPackageInfo> CreatePackageMapper(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<IPackageMapper<TNetPackageInfo, TPackageInfo>>()!;
    }

    public async ValueTask HandleAsync(TClient client, TPackageInfo package)
    {
        if (!_commands.TryGetValue(package.Key, out ICommandSet commandSet))
            return;

        await commandSet.ExecuteAsync(client, package);
    }

    public async ValueTask HandleAsync(TClient client, TPackageInfo package, TKey key)
    {
        if (!_commands.TryGetValue(key, out ICommandSet commandSet))
            return;

        await commandSet.ExecuteAsync(client, package);
    }

    interface ICommandSet
    {
        TKey Key { get; }

        ValueTask ExecuteAsync(TClient cleint, TPackageInfo package);
    }

    class CommandTypeInfo
    {
        public Type CommandType { get; private set; }

        public Type ActualCommandType { get; set; }

        public ICommand Command { get; private set; }

        public Type CommandSetFactoryType { get; private set; }

        public bool WrapRequired { get; set; }

        public Func<Type, Type> WrapFactory { get; set; }

        public CommandTypeInfo(ICommand command)
        {
            Command = command;
            CommandType = command.GetType();
        }

        public CommandTypeInfo(Type commandType, Type commandSetFactoryType)
            : this(commandType, commandSetFactoryType, false)
        {

        }

        public CommandTypeInfo(Type commandType, Type commandSetFactoryType, bool wrapRequired)
        {
            CommandType = commandType;
            CommandSetFactoryType = commandSetFactoryType;
            WrapRequired = wrapRequired;
        }

        public ICommandSetFactory CreateCommandSetFactory(Type type)
        {
            var commandTyeInfo = new CommandTypeInfo(WrapRequired ? WrapFactory(type) : type, null)
            {
                ActualCommandType = type
            };

            return ActivatorUtilities.CreateInstance(null, this.CommandSetFactoryType, commandTyeInfo) as ICommandSetFactory;
        }
    }

    interface ICommandSetFactory
    {
        ICommandSet Create(IServiceProvider serviceProvider, CommandOptions commandOptions);
    }

    class CommandSetFactory : ICommandSetFactory
    {
        public CommandTypeInfo CommandType { get; private set; }

        public CommandSetFactory(CommandTypeInfo commandType)
        {
            CommandType = commandType;
        }

        public ICommandSet Create(IServiceProvider serviceProvider, CommandOptions commandOptions)
        {
            var commandSet = new CommandSet();
            commandSet.Initialize(serviceProvider, CommandType, commandOptions);
            return commandSet;
        }
    }

    class CommandSet : ICommandSet
    {
        public IAsyncCommand<TClient, TPackageInfo> AsyncCommand { get; private set; }

        public CommandMetadata Metadata { get; private set; }

        public TKey Key { get; private set; }

        private readonly bool _isKeyString = false;

        public CommandSet()
        {
            _isKeyString = typeof(TKey) == typeof(string);
        }

        private CommandMetadata GetCommandMetadata(Type commandType)
        {
            var cmdAtt = commandType.GetCustomAttribute(typeof(CommandAttribute)) as CommandAttribute;
            var cmdMeta = default(CommandMetadata);

            if (cmdAtt == null)
            {
                if (!_isKeyString)
                {
                    throw new Exception($"The command {commandType.FullName} needs a CommandAttribute defined.");
                }

                cmdMeta = new CommandMetadata(commandType.Name, commandType.Name);
            }
            else
            {
                var cmdName = cmdAtt.Name;

                if (string.IsNullOrEmpty(cmdName))
                    cmdName = commandType.Name;

                if (cmdAtt.Key == null)
                {
                    if (!_isKeyString)
                    {
                        throw new Exception($"The command {commandType.FullName} needs a Key in type '{typeof(TKey).Name}' defined in its CommandAttribute.");
                    }

                    cmdMeta = new CommandMetadata(cmdName, cmdName);
                }
                else
                {
                    cmdMeta = new CommandMetadata(cmdName, cmdAtt.Key);
                }
            }

            return cmdMeta;
        }

        protected void SetCommand(ICommand command)
        {
            AsyncCommand = command as IAsyncCommand<TClient, TPackageInfo>;
        }

        public void Initialize(IServiceProvider serviceProvider, CommandTypeInfo commandTypeInfo, CommandOptions commandOptions)
        {
            var command = commandTypeInfo.Command;

            if (command == null)
            {
                if (commandTypeInfo.CommandType != commandTypeInfo.ActualCommandType)
                {
                    var commandFactory = ActivatorUtilities.CreateFactory(commandTypeInfo.CommandType, new[] { typeof(IServiceProvider) });
                    command = commandFactory.Invoke(serviceProvider, new object[] { serviceProvider }) as ICommand;
                }
                else
                {
                    command = ActivatorUtilities.CreateInstance(serviceProvider, commandTypeInfo.CommandType) as ICommand;
                }
            }

            SetCommand(command);

            var cmdMeta = GetCommandMetadata(commandTypeInfo.ActualCommandType);

            try
            {
                Key = (TKey)cmdMeta.Key;
                Metadata = cmdMeta;
            }
            catch (Exception e)
            {
                throw new Exception($"The command {cmdMeta.Name}'s Key {cmdMeta.Key} cannot be converted to the desired type '{typeof(TKey).Name}'.", e);
            }
        }

        public async ValueTask ExecuteAsync(TClient client, TPackageInfo package)
        {
            var asyncCommand = AsyncCommand;

            if (asyncCommand == null)
                return;

            await asyncCommand.ExecuteAsync(client, package);
        }

        public override string ToString()
        {
            ICommand command = AsyncCommand;

            return command?.GetType().ToString();
        }
    }
}
