using Agibuild.Fulora.Plugin.LocalStorage;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public class LocalStorageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public LocalStorageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "fulora-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "local-storage.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private LocalStorageService CreateService() => new(_filePath);

    [Fact]
    public async Task Get_NonExistentKey_ReturnsNull()
    {
        var svc = CreateService();
        Assert.Null(await svc.Get("unknown"));
    }

    [Fact]
    public async Task SetAndGet_RoundTrips()
    {
        var svc = CreateService();
        await svc.Set("theme", "dark");
        Assert.Equal("dark", await svc.Get("theme"));
    }

    [Fact]
    public async Task Set_OverwritesExistingKey()
    {
        var svc = CreateService();
        await svc.Set("lang", "en");
        await svc.Set("lang", "zh");
        Assert.Equal("zh", await svc.Get("lang"));
    }

    [Fact]
    public async Task Remove_ExistingKey()
    {
        var svc = CreateService();
        await svc.Set("k", "v");
        await svc.Remove("k");
        Assert.Null(await svc.Get("k"));
    }

    [Fact]
    public async Task Remove_NonExistentKey_NoOp()
    {
        var svc = CreateService();
        await svc.Remove("nope");
    }

    [Fact]
    public async Task Clear_RemovesAll()
    {
        var svc = CreateService();
        await svc.Set("a", "1");
        await svc.Set("b", "2");
        await svc.Clear();
        Assert.Null(await svc.Get("a"));
        Assert.Null(await svc.Get("b"));
        Assert.Empty(await svc.GetKeys());
    }

    [Fact]
    public async Task GetKeys_ReturnsAllKeys()
    {
        var svc = CreateService();
        await svc.Set("x", "1");
        await svc.Set("y", "2");
        await svc.Set("z", "3");
        var keys = await svc.GetKeys();
        Assert.Equal(3, keys.Length);
        Assert.Contains("x", keys);
        Assert.Contains("y", keys);
        Assert.Contains("z", keys);
    }

    [Fact]
    public async Task GetKeys_Empty_ReturnsEmpty()
    {
        var svc = CreateService();
        Assert.Empty(await svc.GetKeys());
    }

    [Fact]
    public async Task Persistence_AcrossServiceRestart()
    {
        var svc1 = CreateService();
        await svc1.Set("theme", "dark");
        await svc1.Set("lang", "zh");

        var svc2 = CreateService();
        Assert.Equal("dark", await svc2.Get("theme"));
        Assert.Equal("zh", await svc2.Get("lang"));
    }

    [Fact]
    public async Task Persistence_RemoveThenRestart()
    {
        var svc1 = CreateService();
        await svc1.Set("a", "1");
        await svc1.Set("b", "2");
        await svc1.Remove("a");

        var svc2 = CreateService();
        Assert.Null(await svc2.Get("a"));
        Assert.Equal("2", await svc2.Get("b"));
    }

    [Fact]
    public async Task Persistence_ClearThenRestart()
    {
        var svc1 = CreateService();
        await svc1.Set("x", "1");
        await svc1.Clear();

        var svc2 = CreateService();
        Assert.Empty(await svc2.GetKeys());
    }

    [Fact]
    public void Constructor_CorruptJsonFile_RecoveryGracefully()
    {
        File.WriteAllText(_filePath, "not-json!!!");
        var svc = CreateService();
        var keys = svc.GetKeys().Result;
        Assert.Empty(keys);
    }

    [Fact]
    public async Task Set_EmptyValue_Works()
    {
        var svc = CreateService();
        await svc.Set("empty", "");
        Assert.Equal("", await svc.Get("empty"));
    }

    [Fact]
    public async Task Set_UnicodeValue_Persists()
    {
        var svc = CreateService();
        await svc.Set("greeting", "你好世界 🌍");

        var svc2 = CreateService();
        Assert.Equal("你好世界 🌍", await svc2.Get("greeting"));
    }
}
