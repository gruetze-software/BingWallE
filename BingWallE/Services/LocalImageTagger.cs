using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace BingWallE.Services;

public interface IImageTagger
{
    Task<IReadOnlyList<ImageTag>> RecognizeAsync(byte[] image, CancellationToken ct, Action<string>? progress = null);
}

public sealed class LocalImageTagger(string modelDirectory) : IImageTagger, IDisposable
{
    public const string ModelSha256 = "c1c513582d56afceff8516c73804e484c81c6a830712ab6d682253f4a3cd042f";
    public const int ModelLength = 14246826;
    public const string ModelUrl = "https://media.githubusercontent.com/media/onnx/models/c5cae0f1942c8d7727372c7b1f6b485aa39e4421/validated/vision/classification/mobilenet/model/mobilenetv2-7.onnx";
    public static string DefaultDirectory => ApplicationPaths.ModelsDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private InferenceSession? _session;
    private readonly CancellationTokenSource _shutdown = new();
    private volatile bool _disposed;
    private static readonly string[] Labels = LoadLabels();

    public async Task<IReadOnlyList<ImageTag>> RecognizeAsync(byte[] image, CancellationToken ct, Action<string>? progress = null)
    {
        InferenceRuntimePolicy.EnsureConfigured();
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _shutdown.Token);
        ct = linked.Token;
        await _gate.WaitAsync(ct);
        try
        {
            if (_session == null)
            {
                var path = await EnsureModelAsync(ct, progress);
                _session = await Task.Run(() =>
                {
                    ct.ThrowIfCancellationRequested();
                    OrtEnv.Instance().DisableTelemetryEvents();
                    using var options = new SessionOptions { IntraOpNumThreads = Math.Min(2, Environment.ProcessorCount) };
                    return new InferenceSession(path, options);
                }, ct);
            }
            progress?.Invoke("Bildmotive werden lokal erkannt …");
            return await Task.Run(() => Recognize(image, ct), ct);
        }
        finally
        {
            if (_disposed) { _session?.Dispose(); _session = null; }
            _gate.Release();
        }
    }

    private async Task<string> EnsureModelAsync(CancellationToken ct, Action<string>? progress)
    {
        Directory.CreateDirectory(modelDirectory);
        var path = Path.Combine(modelDirectory, "mobilenetv2-7.onnx");
        if (File.Exists(path) && await VerifyAsync(path, ct)) return path;
        progress?.Invoke("Bilderkennungsmodell wird geladen (14 MB, einmalig) …");
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromMinutes(3));
            using var client = new HttpClient();
            using var response = await client.GetAsync(ModelUrl, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            await using (var source = await response.Content.ReadAsStreamAsync(deadline.Token))
            await using (var target = File.Create(temporary))
            {
                var buffer = new byte[81920]; var total = 0;
                int count;
                while ((count = await source.ReadAsync(buffer, deadline.Token)) > 0)
                {
                    total += count;
                    if (total > ModelLength) throw new InvalidDataException("Unexpected model size.");
                    await target.WriteAsync(buffer.AsMemory(0, count), deadline.Token);
                }
            }
            if (!await VerifyAsync(temporary, ct)) throw new InvalidDataException("Model checksum verification failed.");
            ct.ThrowIfCancellationRequested(); File.Move(temporary, path, true);
            return path;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static async Task<bool> VerifyAsync(string path, CancellationToken ct)
    {
        if (new FileInfo(path).Length != ModelLength) return false;
        await using var input = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(input, ct)).Equals(ModelSha256, StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<ImageTag> Recognize(byte[] data, CancellationToken ct)
    {
        using var bitmap = SKBitmap.Decode(data) ?? throw new InvalidDataException("Invalid image.");
        // Resize the shorter side to 256, then take a centered 224-pixel crop as documented by the model.
        var scale = 256f / Math.Min(bitmap.Width, bitmap.Height);
        using var resized = bitmap.Resize(new SKImageInfo((int)(bitmap.Width * scale), (int)(bitmap.Height * scale)), new SKSamplingOptions(SKCubicResampler.Mitchell))
            ?? throw new InvalidDataException("Cannot resize image.");
        var left = (resized.Width - 224) / 2; var top = (resized.Height - 224) / 2;
        var tensor = new DenseTensor<float>(new[] { 1, 3, 224, 224 });
        for (var y = 0; y < 224; y++)
        {
            ct.ThrowIfCancellationRequested();
            for (var x = 0; x < 224; x++)
            {
                var pixel = resized.GetPixel(left + x, top + y);
                tensor[0, 0, y, x] = (pixel.Red / 255f - .485f) / .229f;
                tensor[0, 1, y, x] = (pixel.Green / 255f - .456f) / .224f;
                tensor[0, 2, y, x] = (pixel.Blue / 255f - .406f) / .225f;
            }
        }
        using var options = new RunOptions();
        using var cancellation = ct.Register(() => options.Terminate = true);
        try
        {
            using var output = _session!.Run(new[] { NamedOnnxValue.CreateFromTensor(_session.InputMetadata.Keys.Single(), tensor) }, _session.OutputMetadata.Keys.ToArray(), options);
            ct.ThrowIfCancellationRequested();
            return SelectTags(output.First().AsEnumerable<float>().ToArray(), Labels);
        }
        catch (OnnxRuntimeException) when (ct.IsCancellationRequested) { throw new OperationCanceledException(ct); }
    }

    public static IReadOnlyList<ImageTag> SelectTags(float[] scores, string[] labels)
    {
        if (scores.Length != labels.Length || scores.Any(x => !float.IsFinite(x))) throw new InvalidDataException("Unexpected model output.");
        var max = scores.Max(); var probabilities = scores.Select(x => Math.Exp(x - max)).ToArray();
        var sum = probabilities.Sum();
        return probabilities.Select((x, i) => new ImageTag(labels[i], (float)(x / sum)))
            .Where(x => x.Score >= .50f).OrderByDescending(x => x.Score).Take(3).ToArray();
    }

    private static string[] LoadLabels()
    {
        using var stream = typeof(LocalImageTagger).Assembly.GetManifestResourceStream("BingWallE.Assets.Recognition.imagenet-labels.txt")
            ?? throw new InvalidOperationException("Missing model labels.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()[(x.IndexOf(' ') + 1)..].Split(',')[0].Trim()).ToArray();
    }
    public void Dispose()
    {
        _disposed = true; _shutdown.Cancel();
        // An active inference owns the session and releases it in its finally block.
        if (_gate.Wait(0))
        {
            try { _session?.Dispose(); _session = null; }
            finally { _gate.Release(); }
        }
    }
}
