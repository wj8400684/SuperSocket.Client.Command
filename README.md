var services = new ServiceCollection();

services.AddLogging();

services.AddCommandClient<CommandKey, RpcPackageBase>(option =>
{
    option.UseClient<RpcClient>();
    option.UsePackageEncoder<RpcPackageEncode>();
    option.UsePipelineFilter<RpcPipeLineFilter>();
    option.UseCommand(options => options.AddCommandAssembly(typeof(LoginAck).Assembly));
});

var provider = services.BuildServiceProvider();

var client = provider.GetRequiredService<RpcClient>();
