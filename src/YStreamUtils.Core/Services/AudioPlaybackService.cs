using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Structs;
using YStreamUtils.Core.Events;

namespace YStreamUtils.Core.Services;

public record AudioClip(byte[] Audio)
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
}

public interface IAudioPlaybackService
{
    void Enqueue(byte[] audioData);
    void SkipCurrent();
    void Clear();
}

public class AudioPlaybackService(IEventBus eventBus) : BackgroundService, IAudioPlaybackService
{
    private readonly ConcurrentQueue<AudioClip> _audioQueue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly Lock _skipLock = new();

    private CancellationTokenSource? _currentClipCts;
    private string? _currentClipId;
    private MiniAudioEngine? _engine;
    private AudioPlaybackDevice? _outputDevice;

    public void Enqueue(byte[] audioData)
    {
        _audioQueue.Enqueue(new AudioClip(audioData));
        _signal.Release();
    }

    public void SkipCurrent()
    {
        string? skippedId;
        lock (_skipLock)
        {
            skippedId = _currentClipId;
            _currentClipCts?.Cancel();
        }

        if (skippedId is not null)
        {
            _ = eventBus.PublishAsync(EventKey.AudioSkip, new AudioClipEventPayload(skippedId));
        }
    }

    public void Clear()
    {
        while (_audioQueue.TryDequeue(out _)) { }

        lock (_skipLock)
        {
            _currentClipCts?.Cancel();
        }

        _ = eventBus.PublishAsync(EventKey.AudioClear, null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _engine = new MiniAudioEngine();
        var defaultDevice = _engine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
        _outputDevice = _engine.InitializePlaybackDevice(defaultDevice, AudioFormat.Dvd);
        _outputDevice.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(stoppingToken);

                    if (_audioQueue.TryDequeue(out var clip))
                    {
                        await PlayClipAsync(clip, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    // Current clip was skipped; continue loop
                }
            }
        }
        finally
        {
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _engine.Dispose();
        }
    }

    private async Task PlayClipAsync(AudioClip clip, CancellationToken stoppingToken)
    {
        if (_engine is null || _outputDevice is null) return;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        lock (_skipLock)
        {
            _currentClipCts = linkedCts;
            _currentClipId = clip.Id;
        }

        try
        {
            var base64Audio = Convert.ToBase64String(clip.Audio);
            await eventBus.PublishAsync(
                EventKey.AudioPlay,
                new AudioPlayPayload(clip.Id, base64Audio),
                stoppingToken);

            using var ms = new MemoryStream(clip.Audio);
            using var provider = new StreamDataProvider(_engine, ms);
            using var player = new SoundPlayer(_engine, _outputDevice.Format, provider);

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            player.PlaybackEnded += (_, _) => tcs.TrySetResult();

            _outputDevice.MasterMixer.AddComponent(player);
            player.Play();

            var registration = linkedCts.Token.Register(static state =>
            {
                var (p, taskSource) = ((SoundPlayer, TaskCompletionSource))state!;
                p.Stop();
                taskSource.TrySetResult();
            }, (player, tcs));

            await using (registration)
            {
                await tcs.Task;
            }

            _outputDevice.MasterMixer.RemoveComponent(player);

            if (!linkedCts.IsCancellationRequested)
            {
                await eventBus.PublishAsync(
                    EventKey.AudioEnded,
                    new AudioClipEventPayload(clip.Id),
                    stoppingToken);
            }
        }
        finally
        {
            lock (_skipLock)
            {
                _currentClipCts = null;
                _currentClipId = null;
            }
        }
    }

    public override void Dispose()
    {
        lock (_skipLock)
        {
            _currentClipCts?.Dispose();
        }
        _signal.Dispose();
        base.Dispose();
    }
    
    public static byte[] ToWav(byte[] pcmData, int sourceRate = 22050, int targetRate = 48000)
    {
        const short channels = 2;
        const short bitsPerSample = 16;

        var resampledFrames = (int)((long)pcmData.Length * targetRate / sourceRate);
        var dataLength = resampledFrames * channels * (bitsPerSample / 8);
        var byteRate = targetRate * channels * (bitsPerSample / 8);
        const short blockAlign = channels * (bitsPerSample / 8);

        using var ms = new MemoryStream(44 + dataLength);
        using var writer = new BinaryWriter(ms);

        writer.Write("RIFF"u8);
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write(channels);
        writer.Write(targetRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8);
        writer.Write(dataLength);

        var step = (double)sourceRate / targetRate;
        for (var i = 0; i < resampledFrames; i++)
        {
            var srcPos = i * step;
            var idx = (int)srcPos;
            var frac = srcPos - idx;

            var s1 = (short)((pcmData[idx] - 128) << 8);
            var s2 = idx + 1 < pcmData.Length ? (short)((pcmData[idx + 1] - 128) << 8) : s1;
            var sample = (short)(s1 + (s2 - s1) * frac);

            writer.Write(sample);
            writer.Write(sample);
        }

        return ms.ToArray();
    }


}