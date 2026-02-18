using System;
using System.IO;
using Avalonia.Platform;
using OpenTK.Audio.OpenAL;

namespace TyperPro.Services;

public sealed class TypingSoundService : IDisposable
{
    private readonly ALDevice _device;
    private readonly ALContext _context;
    private readonly int _buffer;
    private readonly int _source;

    private DateTime _lastPlayed = DateTime.MinValue;
    private const int MinIntervalMs = 25;

    public TypingSoundService()
    {
        _device = ALC.OpenDevice(null);
        if (_device == ALDevice.Null)
            throw new Exception("Failed to open OpenAL device");

        _context = ALC.CreateContext(_device, (int[])null);
        ALC.MakeContextCurrent(_context);

        _buffer = AL.GenBuffer();
        _source = AL.GenSource();

        LoadWavFromAssets("avares://TyperPro/Assets/Button.wav");
    }

    private void LoadWavFromAssets(string uri)
    {
        using var stream = AssetLoader.Open(new Uri(uri));
        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        var wav = ms.ToArray();

        // Skip WAV header (44 bytes)
        var audioData = new byte[wav.Length - 44];
        Array.Copy(wav, 44, audioData, 0, audioData.Length);

        AL.BufferData(
            _buffer,
            ALFormat.Mono16,
            audioData,
            44100
        );

        AL.Source(_source, ALSourcei.Buffer, _buffer);
        AL.Source(_source, ALSourcef.Gain, 0.25f);
    }

    public void Play()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastPlayed).TotalMilliseconds < MinIntervalMs)
            return;

        _lastPlayed = now;

        AL.SourceStop(_source);
        AL.SourcePlay(_source);
    }

    public void Dispose()
    {
        AL.DeleteSource(_source);
        AL.DeleteBuffer(_buffer);

        ALC.MakeContextCurrent(ALContext.Null);
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
    }
}