using System.Collections.Concurrent;
using Agibuild.Fulora.Testing;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ContractSemanticsV1OperationQueueTests
{
    [Fact]
    public void Operations_execute_in_enqueue_order()
    {
        var dispatcher = new TestDispatcher();
        var invocationOrder = new ConcurrentQueue<string>();
        var adapter = new MockWebViewAdapter
        {
            ScriptCallback = script =>
            {
                invocationOrder.Enqueue(script);
                return "ok";
            }
        };
        using var core = new WebViewCore(adapter, dispatcher);

        var tasks = new List<Task<string?>>();
        for (var i = 0; i < 20; i++)
        {
            tasks.Add(core.InvokeScriptAsync($"op-{i}"));
        }

        while (tasks.Any(t => !t.IsCompleted))
        {
            dispatcher.RunAll();
            Thread.Sleep(1);
        }

        Task.WhenAll(tasks).GetAwaiter().GetResult();

        Assert.Equal(Enumerable.Range(0, 20).Select(i => $"op-{i}"), invocationOrder.ToArray());
    }

    [Fact]
    public void Concurrent_producers_complete_once_per_operation()
    {
        var dispatcher = new TestDispatcher();
        var invocationOrder = new ConcurrentQueue<string>();
        var adapter = new MockWebViewAdapter
        {
            ScriptCallback = script =>
            {
                invocationOrder.Enqueue(script);
                return "ok";
            }
        };
        using var core = new WebViewCore(adapter, dispatcher);

        var results = new ConcurrentBag<string?>();
        var operations = Enumerable.Range(0, 50)
            .Select(i => Task.Run(async () =>
            {
                var result = await core.InvokeScriptAsync($"p-{i}");
                results.Add(result);
            }))
            .ToArray();

        while (operations.Any(t => !t.IsCompleted))
        {
            dispatcher.RunAll();
            Thread.Sleep(1);
        }

        Task.WhenAll(operations).GetAwaiter().GetResult();

        Assert.Equal(50, results.Count);
        Assert.Equal(50, invocationOrder.Count);
        Assert.Equal(50, invocationOrder.Distinct().Count());
    }
}
