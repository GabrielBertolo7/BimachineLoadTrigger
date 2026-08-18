using System.Net;
using System.Net.Http.Json;
using BimachineLoadTrigger.Core.Configuration;
using BimachineLoadTrigger.Core.Models;
using Microsoft.Extensions.Options;

namespace BimachineLoadTrigger.Core.Tests;

public class BimachineLoadClientTests
{
    private const long ExecutionId = 12453;

    private static readonly BimachineOptions DefaultOptions = new()
    {
        BaseUrl = "https://api.bimachine.com/",
        AppKey = "test-app-key",
        LoadCode = "load-123",
        PollingIntervalSeconds = 0,
        PollingTimeoutMinutes = 1,
    };

    private static BimachineLoadClient CreateClient(FakeHttpMessageHandler handler, BimachineOptions? options = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(DefaultOptions.BaseUrl) };
        return new BimachineLoadClient(httpClient, Options.Create(options ?? DefaultOptions));
    }

    [Fact]
    public async Task ExecuteLoadAsync_Requests_ExpectedUrl_And_ReturnsId()
    {
        var handler = new FakeHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new ExecuteLoadResponse(1484)),
        });
        var client = CreateClient(handler);

        var result = await client.ExecuteLoadAsync(DefaultOptions.LoadCode);

        Assert.Equal(1484, result.Id);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(
            $"https://api.bimachine.com/api/origins/schedulings/{DefaultOptions.LoadCode}/execute?appKey={DefaultOptions.AppKey}",
            request.RequestUri!.ToString());
    }

    [Fact]
    public async Task ExecuteLoadAsync_Throws_When_Api_Returns_Error_Status()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.ExecuteLoadAsync(DefaultOptions.LoadCode));
    }

    [Fact]
    public async Task GetLoadStatusAsync_Requests_ExpectedUrl_And_ReturnsStatus()
    {
        var expectedStatus = new LoadStatusResponse(ExecutionId, "Incremental", "ERROR", 1458566160000, 1458566163000, "log da carga");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expectedStatus),
        });
        var client = CreateClient(handler);

        var result = await client.GetLoadStatusAsync(ExecutionId);

        Assert.Equal(expectedStatus, result);
        Assert.True(result.IsError);
        Assert.True(result.IsFinished);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            $"https://api.bimachine.com/api/origins/schedulings/{ExecutionId}/status?appKey={DefaultOptions.AppKey}",
            request.RequestUri!.ToString());
    }

    [Fact]
    public async Task WaitForCompletionAsync_Polls_Until_EndDate_Is_Present()
    {
        var responses = new Queue<LoadStatusResponse>(
        [
            new LoadStatusResponse(1, "Incremental", "RUNNING", 1000, null, null),
            new LoadStatusResponse(1, "Incremental", "RUNNING", 1000, null, null),
            new LoadStatusResponse(1, "Incremental", "SUCCESS", 1000, 2000, "ok"),
        ]);
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(responses.Dequeue()),
        });
        var client = CreateClient(handler);

        var result = await client.WaitForCompletionAsync(ExecutionId);

        Assert.True(result.IsFinished);
        Assert.False(result.IsError);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Throws_When_Timeout_Elapses()
    {
        var options = DefaultOptions with { PollingTimeoutMinutes = 0 };
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new LoadStatusResponse(1, "Incremental", "RUNNING", 1000, null, null)),
        });
        var client = CreateClient(handler, options);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.WaitForCompletionAsync(ExecutionId));
    }
}
