using AvaloniVue.Bridge.Services;

namespace AvaloniVue.Tests;

public class SystemInfoServiceTests
{
    [Fact]
    public async Task GetSystemInfo_returns_non_null_fields()
    {
        var service = new SystemInfoService();
        var info = await service.GetSystemInfo();

        Assert.NotEmpty(info.OsName);
        Assert.NotEmpty(info.OsVersion);
        Assert.NotEmpty(info.DotnetVersion);
        Assert.NotEmpty(info.MachineName);
        Assert.NotEmpty(info.WebViewEngine);
        Assert.True(info.ProcessorCount > 0);
        Assert.True(info.TotalMemoryMb > 0);
    }

    [Fact]
    public async Task GetRuntimeMetrics_returns_positive_values()
    {
        var service = new SystemInfoService();
        var metrics = await service.GetRuntimeMetrics();

        Assert.True(metrics.WorkingSetMb > 0);
        Assert.True(metrics.GcTotalMemoryMb >= 0);
        Assert.True(metrics.ThreadCount > 0);
        Assert.True(metrics.UptimeSeconds >= 0);
    }

    [Fact]
    public async Task GetRuntimeMetrics_uptime_increases_over_time()
    {
        var service = new SystemInfoService();
        var m1 = await service.GetRuntimeMetrics();
        await Task.Delay(100);
        var m2 = await service.GetRuntimeMetrics();

        Assert.True(m2.UptimeSeconds >= m1.UptimeSeconds);
    }
}
