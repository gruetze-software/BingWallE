using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BingWallE.Services;

/// <summary>Traces headers separately from body completion to locate stalled requests.</summary>
public sealed class HttpTraceHandler : DelegatingHandler
{
    private readonly Action<string> _write;

    public HttpTraceHandler(HttpMessageHandler innerHandler, Action<string>? write = null) : base(innerHandler)
    {
        _write = write ?? (message => Trace.WriteLine(message));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var id = Guid.NewGuid().ToString("N")[..8];
        void Log(string message) => _write($"[{DateTimeOffset.Now:O}] HTTP {id} {message}");
        Log($"START {request.Method} {request.RequestUri}");
        HttpResponseMessage? response = null;
        try
        {
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            Log($"HEADERS {(int)response.StatusCode} {response.ReasonPhrase}, {timer.ElapsedMilliseconds} ms");
            // All callers consume complete images/JSON. Buffer here so the trace and
            // HttpClient timeout include body transfer, not just receipt of headers.
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            Log($"COMPLETE status={(int)response.StatusCode}, bytes={bytes.Length}, {timer.ElapsedMilliseconds} ms");
            return response;
        }
        catch (OperationCanceledException ex)
        {
            response?.Dispose();
            Log($"CANCELED (user cancellation or request timeout) after {timer.ElapsedMilliseconds} ms: {ex}");
            throw;
        }
        catch (Exception ex)
        {
            response?.Dispose();
            Log($"FAILED after {timer.ElapsedMilliseconds} ms: {ex}");
            throw;
        }
    }
}
