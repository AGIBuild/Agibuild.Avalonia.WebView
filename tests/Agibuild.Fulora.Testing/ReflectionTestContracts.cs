namespace Agibuild.Fulora.Testing;

/// <summary>
/// Test interfaces for exercising the reflection-based path in RuntimeBridgeService.
/// These are defined in the Testing assembly which does NOT reference the Bridge.Generator,
/// so they will NOT have source-generated registrations/proxies.
/// This forces RuntimeBridgeService to use the reflection fallback for Expose/GetProxy.
/// </summary>

[JsExport]
public interface IReflectionExportService
{
    Task<string> Greet(string name);
    Task SaveData(string key, string value);
    Task VoidNoArgs();
}

[JsExport(Name = "reflectionCustomName")]
public interface IReflectionCustomNameExport
{
    Task<int> GetCount();
}

[JsImport]
public interface IReflectionImportService
{
    Task NotifyAsync(string message);
    Task<bool> CheckStatus(string id);
}

/// <summary>Default implementation of IReflectionExportService for testing.</summary>
public class FakeReflectionExportService : IReflectionExportService
{
    public string? LastGreetName { get; private set; }
    public (string Key, string Value)? LastSavedData { get; private set; }
    public int VoidCallCount { get; private set; }

    public Task<string> Greet(string name)
    {
        LastGreetName = name;
        return Task.FromResult($"Hello, {name}!");
    }

    public Task SaveData(string key, string value)
    {
        LastSavedData = (key, value);
        return Task.CompletedTask;
    }

    public Task VoidNoArgs()
    {
        VoidCallCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Default implementation of IReflectionCustomNameExport for testing.</summary>
public class FakeReflectionCustomNameExport : IReflectionCustomNameExport
{
    public Task<int> GetCount() => Task.FromResult(42);
}

/// <summary>Export interface whose method always throws, to cover TargetInvocationException unwrap.</summary>
[JsExport]
public interface IReflectionThrowingExport
{
    Task<string> WillThrow();
}

public class FakeReflectionThrowingExport : IReflectionThrowingExport
{
    public Task<string> WillThrow() => throw new InvalidOperationException("Deliberate test exception");
}

/// <summary>Export interface with value-type parameters, to cover Activator.CreateInstance default path.</summary>
[JsExport]
public interface IReflectionValueTypeExport
{
    Task<int> Add(int a, int b);
}

public class FakeReflectionValueTypeExport : IReflectionValueTypeExport
{
    public (int A, int B)? LastArgs { get; private set; }

    public Task<int> Add(int a, int b)
    {
        LastArgs = (a, b);
        return Task.FromResult(a + b);
    }
}
