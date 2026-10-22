using System.Net;
using BingWallE.Services;
using Xunit;

namespace BingWallE.Tests;

public class HttpTraceTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task LogsStatusAndBodyCompletionWithoutChangingResponse(HttpStatusCode status)
    {
        var messages = new List<string>();
        using var client = new HttpClient(new HttpTraceHandler(new Handler((_, _) => Task.FromResult(
            new HttpResponseMessage(status) { Content = new ByteArrayContent([1, 2, 3]) })), messages.Add));
        using var response = await client.GetAsync("https://example.test/image.jpg", TestContext.Current.CancellationToken);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(new byte[] { 1, 2, 3 }, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal(3, messages.Count);
        Assert.Contains("START GET https://example.test/image.jpg", messages[0]);
        Assert.Contains($"HEADERS {(int)status}", messages[1]);
        Assert.Contains("bytes=3", messages[2]);
        Assert.Contains("COMPLETE", messages[2]);
    }

    [Fact]
    public async Task LogsNetworkFailureAndPreservesException()
    {
        var messages = new List<string>();
        var error = new HttpRequestException("test connection failure");
        using var client = new HttpClient(new HttpTraceHandler(new Handler((_, _) => throw error), messages.Add));
        var actual = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("https://example.test/image.jpg", TestContext.Current.CancellationToken));
        Assert.Same(error, actual);
        Assert.Contains("FAILED", messages[^1]);
        Assert.Contains("test connection failure", messages[^1]);
    }

    [Fact]
    public async Task CancellationDuringBodyTransferIsTracedAndHonored()
    {
        var messages = new List<string>();
        var body = new WaitingContent();
        using var client = new HttpClient(new HttpTraceHandler(new Handler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = body })), messages.Add));
        using var cancellation = new CancellationTokenSource();
        var request = client.GetAsync("https://example.test/image.jpg", cancellation.Token);
        await body.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Contains(messages, message => message.Contains("HEADERS 200"));
        Assert.Contains("CANCELED", messages[^1]);
        Assert.DoesNotContain("Exception", messages[^1]);
        Assert.DoesNotContain("\n", messages[^1]);
        Assert.DoesNotContain(messages, message => message.Contains("COMPLETE"));
    }

    private sealed class WaitingContent : HttpContent
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken ct)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
    }
}
