using System.Net;
using System.Net.Http;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public class ConfigProviderTests
{
    private const string SampleConfig = """
        {
          "appName": "TestApp",
          "maxRetries": 3,
          "debugMode": true,
          "featureA": true,
          "featureB": false,
          "featureC": "true",
          "featureD": "false",
          "featureE": 1,
          "featureF": 0,
          "nested": { "value": 42 }
        }
        """;

    private static string CreateTempConfig(string content = SampleConfig)
    {
        var path = Path.Combine(Path.GetTempPath(), $"fulora-config-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task GetValueAsync_returns_value_for_existing_key()
    {
        var path = CreateTempConfig();
        try
        {
            var provider = new JsonFileConfigProvider(path);
            var value = await provider.GetValueAsync("appName");
            Assert.NotNull(value);
            Assert.Equal("TestApp", value);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GetValueAsync_returns_null_for_missing_key()
    {
        var path = CreateTempConfig();
        try
        {
            var provider = new JsonFileConfigProvider(path);
            var value = await provider.GetValueAsync("nonexistent");
            Assert.Null(value);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GetValueAsync_T_deserializes_int_correctly()
    {
        var path = CreateTempConfig();
        try
        {
            var provider = new JsonFileConfigProvider(path);
            var value = await provider.GetValueAsync<int>("maxRetries");
            Assert.Equal(3, value);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GetValueAsync_T_deserializes_bool_correctly()
    {
        var path = CreateTempConfig();
        try
        {
            var provider = new JsonFileConfigProvider(path);
            var value = await provider.GetValueAsync<bool>("debugMode");
            Assert.True(value);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GetValueAsync_T_deserializes_string_correctly()
    {
        var path = CreateTempConfig();
        try
        {
            var provider = new JsonFileConfigProvider(path);
            var value = await provider.GetValueAsync<string>("appName");
            Assert.Equal("TestApp", value);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_returns_true_for_enabled_feature()
    {
        var path = CreateTempConfig();
        try
        {
            var provider = new JsonFileConfigProvider(path);
            Assert.True(await provider.IsFeatureEnabledAsync("featureA"));
            Assert.True(await provider.IsFeatureEnabledAsync("featureC"));
            Assert.True(await provider.IsFeatureEnabledAsync("featureE"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_returns_false_for_disabled_feature()
    {
        var path = CreateTempConfig();
        try
        {
            var provider = new JsonFileConfigProvider(path);
            Assert.False(await provider.IsFeatureEnabledAsync("featureB"));
            Assert.False(await provider.IsFeatureEnabledAsync("featureD"));
            Assert.False(await provider.IsFeatureEnabledAsync("featureF"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GetSectionAsync_returns_nested_object_as_key_value_pairs()
    {
        var path = CreateTempConfig();
        try
        {
            var provider = new JsonFileConfigProvider(path);
            var section = await provider.GetSectionAsync("nested");
            Assert.NotNull(section);
            Assert.Single(section);
            Assert.True(section.ContainsKey("value"));
            Assert.Equal("42", section["value"]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GetSectionAsync_returns_null_for_missing_key()
    {
        var path = CreateTempConfig();
        try
        {
            var provider = new JsonFileConfigProvider(path);
            var section = await provider.GetSectionAsync("nonexistent");
            Assert.Null(section);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GetSectionAsync_returns_null_when_key_is_not_object()
    {
        var path = CreateTempConfig();
        try
        {
            var provider = new JsonFileConfigProvider(path);
            var section = await provider.GetSectionAsync("appName");
            Assert.Null(section);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task RefreshAsync_reloads_changed_file()
    {
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, $"fulora-config-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, """{"key": "v1"}""");
            var provider = new JsonFileConfigProvider(tempFile);
            Assert.Equal("v1", await provider.GetValueAsync("key"));

            File.WriteAllText(tempFile, """{"key": "v2"}""");
            await provider.RefreshAsync();
            Assert.Equal("v2", await provider.GetValueAsync("key"));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Missing_file_throws_on_construction()
    {
        var nonexistent = Path.Combine(Path.GetTempPath(), $"fulora-nonexistent-{Guid.NewGuid():N}.json");
        Assert.False(File.Exists(nonexistent));
        Assert.Throws<FileNotFoundException>(() => new JsonFileConfigProvider(nonexistent));
    }

    [Fact]
    public async Task Thread_safety_concurrent_reads_and_refresh()
    {
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, $"fulora-config-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, """{"a": 1, "b": 2}""");
            var provider = new JsonFileConfigProvider(tempFile);

            var readTasks = new List<Task>();
            for (var i = 0; i < 20; i++)
            {
                readTasks.Add(Task.Run(async () =>
                {
                    for (var j = 0; j < 50; j++)
                    {
                        _ = await provider.GetValueAsync("a");
                        _ = await provider.GetValueAsync<int>("b");
                        _ = await provider.IsFeatureEnabledAsync("a");
                    }
                }));
            }

            var refreshTask = Task.Run(async () =>
            {
                for (var j = 0; j < 10; j++)
                {
                    File.WriteAllText(tempFile, "{\"a\": " + j + ", \"b\": 2}");
                    await provider.RefreshAsync();
                }
            });

            await Task.WhenAll([.. readTasks, refreshTask]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    // --- RemoteConfigProvider tests ---

    [Fact]
    public async Task RemoteConfigProvider_returns_remote_value_when_available()
    {
        var remoteJson = """{"remoteKey": "remoteValue", "featureX": true}""";
        var handler = new MockHttpHandler(remoteJson);
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));

        await provider.RefreshAsync();
        var value = await provider.GetValueAsync("remoteKey");
        Assert.Equal("remoteValue", value);
        Assert.True(await provider.IsFeatureEnabledAsync("featureX"));
    }

    [Fact]
    public async Task RemoteConfigProvider_falls_back_to_local_when_remote_key_missing()
    {
        var remoteJson = """{"remoteOnly": "fromRemote"}""";
        var localPath = CreateTempConfig("""{"localKey": "localValue"}""");
        try
        {
            var localProvider = new JsonFileConfigProvider(localPath);
            var handler = new MockHttpHandler(remoteJson);
            var client = new HttpClient(handler);
            var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"), localProvider);

            await provider.RefreshAsync();

            Assert.Equal("fromRemote", await provider.GetValueAsync("remoteOnly"));
            Assert.Equal("localValue", await provider.GetValueAsync("localKey"));
        }
        finally { File.Delete(localPath); }
    }

    [Fact]
    public async Task RemoteConfigProvider_remote_overrides_local()
    {
        var remoteJson = """{"sharedKey": "fromRemote"}""";
        var localPath = CreateTempConfig("""{"sharedKey": "fromLocal"}""");
        try
        {
            var localProvider = new JsonFileConfigProvider(localPath);
            var handler = new MockHttpHandler(remoteJson);
            var client = new HttpClient(handler);
            var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"), localProvider);

            await provider.RefreshAsync();

            Assert.Equal("fromRemote", await provider.GetValueAsync("sharedKey"));
        }
        finally { File.Delete(localPath); }
    }

    [Fact]
    public async Task RemoteConfigProvider_RefreshAsync_fetches_from_HTTP()
    {
        var handler = new MockHttpHandler("""{"v": "1"}""");
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));

        Assert.Null(await provider.GetValueAsync("v"));

        await provider.RefreshAsync();
        Assert.Equal("1", await provider.GetValueAsync("v"));

        handler.SetResponse("""{"v": "2"}""");
        await provider.RefreshAsync();
        Assert.Equal("2", await provider.GetValueAsync("v"));
    }

    [Fact]
    public async Task RemoteConfigProvider_GetSectionAsync_works_on_remote_provider()
    {
        var remoteJson = """{"section": {"a": "1", "b": "2"}}""";
        var handler = new MockHttpHandler(remoteJson);
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));

        await provider.RefreshAsync();
        var section = await provider.GetSectionAsync("section");

        Assert.NotNull(section);
        Assert.Equal(2, section.Count);
        Assert.Equal("1", section["a"]);
        Assert.Equal("2", section["b"]);
    }

    [Fact]
    public async Task RemoteConfigProvider_GetSectionAsync_falls_back_to_local()
    {
        var remoteJson = """{}""";
        var localPath = CreateTempConfig("""{"nested": {"value": 42}}""");
        try
        {
            var localProvider = new JsonFileConfigProvider(localPath);
            var handler = new MockHttpHandler(remoteJson);
            var client = new HttpClient(handler);
            var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"), localProvider);

            await provider.RefreshAsync();
            var section = await provider.GetSectionAsync("nested");

            Assert.NotNull(section);
            Assert.Single(section);
            Assert.Equal("42", section["value"]);
        }
        finally { File.Delete(localPath); }
    }

    // --- RemoteConfigProvider branch coverage ---

    [Fact]
    public async Task RemoteConfigProvider_IsTruthy_covers_all_branches()
    {
        var json = """{"boolTrue": true, "boolFalse": false, "numOne": 1, "numZero": 0, "strTrue": "true", "strFalse": "nope", "strYes": "yes", "strOn": "on", "str1": "1", "strEmpty": "", "arr": [1], "numOther": 2}""";
        var handler = new MockHttpHandler(json);
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));
        await provider.RefreshAsync();

        Assert.True(await provider.IsFeatureEnabledAsync("boolTrue"));
        Assert.False(await provider.IsFeatureEnabledAsync("boolFalse"));
        Assert.True(await provider.IsFeatureEnabledAsync("numOne"));
        Assert.False(await provider.IsFeatureEnabledAsync("numZero"));
        Assert.True(await provider.IsFeatureEnabledAsync("strTrue"));
        Assert.False(await provider.IsFeatureEnabledAsync("strFalse"));
        Assert.True(await provider.IsFeatureEnabledAsync("strYes"));
        Assert.True(await provider.IsFeatureEnabledAsync("strOn"));
        Assert.True(await provider.IsFeatureEnabledAsync("str1"));
        Assert.False(await provider.IsFeatureEnabledAsync("strEmpty"));
        Assert.False(await provider.IsFeatureEnabledAsync("arr"));
        Assert.False(await provider.IsFeatureEnabledAsync("numOther"));
    }

    [Fact]
    public async Task RemoteConfigProvider_GetValueAsync_returns_raw_for_non_string()
    {
        var json = """{"num": 42, "obj": {"a": 1}}""";
        var handler = new MockHttpHandler(json);
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));
        await provider.RefreshAsync();

        Assert.Equal("42", await provider.GetValueAsync("num"));
        Assert.Contains("\"a\"", await provider.GetValueAsync("obj"));
    }

    [Fact]
    public async Task RemoteConfigProvider_GetValueAsync_returns_null_without_fallback()
    {
        var handler = new MockHttpHandler("{}");
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));
        await provider.RefreshAsync();

        Assert.Null(await provider.GetValueAsync("missing"));
    }

    [Fact]
    public async Task RemoteConfigProvider_GetValueAsyncT_returns_default_on_deserialize_failure()
    {
        var json = """{"bad": "not_a_number"}""";
        var handler = new MockHttpHandler(json);
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));
        await provider.RefreshAsync();

        var result = await provider.GetValueAsync<int>("bad");
        Assert.Equal(default, result);
    }

    [Fact]
    public async Task RemoteConfigProvider_GetValueAsyncT_returns_value_from_remote()
    {
        var json = """{"count": 42}""";
        var handler = new MockHttpHandler(json);
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));
        await provider.RefreshAsync();

        var result = await provider.GetValueAsync<int>("count");
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task RemoteConfigProvider_GetValueAsyncT_falls_back_without_remote_key()
    {
        var handler = new MockHttpHandler("{}");
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));
        await provider.RefreshAsync();

        var result = await provider.GetValueAsync<int>("missing");
        Assert.Equal(default, result);
    }

    [Fact]
    public async Task RemoteConfigProvider_IsFeatureEnabled_falls_back_without_remote()
    {
        var handler = new MockHttpHandler("{}");
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));
        await provider.RefreshAsync();

        Assert.False(await provider.IsFeatureEnabledAsync("missing"));
    }

    [Fact]
    public async Task RemoteConfigProvider_GetSectionAsync_returns_null_without_fallback()
    {
        var handler = new MockHttpHandler("{}");
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));
        await provider.RefreshAsync();

        Assert.Null(await provider.GetSectionAsync("missing"));
    }

    [Fact]
    public async Task RemoteConfigProvider_GetSectionAsync_returns_null_for_non_object()
    {
        var json = """{"scalar": "hello"}""";
        var handler = new MockHttpHandler(json);
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));
        await provider.RefreshAsync();

        Assert.Null(await provider.GetSectionAsync("scalar"));
    }

    [Fact]
    public void RemoteConfigProvider_null_httpClient_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RemoteConfigProvider(null!, new Uri("https://example.com")));
    }

    [Fact]
    public void RemoteConfigProvider_null_uri_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RemoteConfigProvider(new HttpClient(), null!));
    }

    [Fact]
    public async Task RemoteConfigProvider_GetSectionAsync_with_numeric_values()
    {
        var json = """{"sec": {"x": 1, "y": "hello"}}""";
        var handler = new MockHttpHandler(json);
        var client = new HttpClient(handler);
        var provider = new RemoteConfigProvider(client, new Uri("https://config.example.com/config.json"));
        await provider.RefreshAsync();

        var section = await provider.GetSectionAsync("sec");
        Assert.NotNull(section);
        Assert.Equal("1", section!["x"]);
        Assert.Equal("hello", section["y"]);
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private string _response;
        private readonly object _lock = new();

        public MockHttpHandler(string response) => _response = response;

        public void SetResponse(string response)
        {
            lock (_lock) _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            lock (_lock)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_response)
                });
            }
        }
    }
}
