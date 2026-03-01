using AvaloniVue.Bridge.Models;
using AvaloniVue.Bridge.Services;

namespace AvaloniVue.Tests;

public class ChatServiceTests
{
    [Fact]
    public async Task SendMessage_returns_response_with_content()
    {
        var service = new ChatService();
        var response = await service.SendMessage(new ChatRequest("hello"));

        Assert.NotNull(response);
        Assert.NotEmpty(response.Id);
        Assert.NotEmpty(response.Message);
    }

    [Fact]
    public async Task SendMessage_adds_to_history()
    {
        var service = new ChatService();
        await service.SendMessage(new ChatRequest("test"));

        var history = await service.GetHistory();
        Assert.Equal(2, history.Count); // user + assistant
        Assert.Equal("user", history[0].Role);
        Assert.Equal("assistant", history[1].Role);
    }

    [Fact]
    public async Task GetHistory_initially_empty()
    {
        var service = new ChatService();
        var history = await service.GetHistory();

        Assert.Empty(history);
    }

    [Fact]
    public async Task ClearHistory_resets_conversation()
    {
        var service = new ChatService();
        await service.SendMessage(new ChatRequest("hello"));
        await service.ClearHistory();

        var history = await service.GetHistory();
        Assert.Empty(history);
    }

    [Fact]
    public async Task SendMessage_preserves_message_order()
    {
        var service = new ChatService();
        await service.SendMessage(new ChatRequest("first"));
        await service.SendMessage(new ChatRequest("second"));

        var history = await service.GetHistory();
        Assert.Equal(4, history.Count);
        Assert.Equal("first", history[0].Content);
        Assert.Equal("second", history[2].Content);
    }
}
