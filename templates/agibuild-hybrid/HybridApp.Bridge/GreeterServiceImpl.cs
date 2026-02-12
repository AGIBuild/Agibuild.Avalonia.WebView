namespace HybridApp.Bridge;

public sealed class GreeterServiceImpl : IGreeterService
{
    public Task<string> Greet(string name)
        => Task.FromResult($"Hello, {name}! 👋 from C#");
}
