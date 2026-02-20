using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;
using OpenTK.Audio.OpenAL;

namespace TyperPro.Services;

public sealed class TypingSoundService : IDisposable
{
    private readonly int _keyBuffer;
    private readonly int _keySource;
    private DateTime _lastPlayed = DateTime.MinValue;
    private const int MinIntervalMs = 25;

    private readonly int _tickBuffer;
    private readonly int _tickSource;

    private readonly int _goBuffer;
    private readonly int _goSource;

    private readonly ALDevice  _device;
    private readonly ALContext _context;

    public TypingSoundService()
    {
        ALDevice device = ALDevice.Null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, ALC.OpenDevice(null) can hang indefinitely.
            // Use a 1-second timeout — if it doesn't respond, throw so
            // the caller disables sound gracefully.
            var ready = new ManualResetEventSlim(false);
            var t = new Thread(() =>
            {
                try   { device = ALC.OpenDevice(null); }
                catch { device = ALDevice.Null; }
                finally { ready.Set(); }
            }) { IsBackground = true };
            t.Start();

            if (!ready.Wait(TimeSpan.FromSeconds(1)) || device == ALDevice.Null)
                throw new Exception("OpenAL unavailable on Windows — sound disabled");
        }
        else
        {
            // Linux / macOS — OpenAL Soft responds immediately
            device = ALC.OpenDevice(null);
            if (device == ALDevice.Null)
                throw new Exception("Failed to open OpenAL device");
        }

        _device = device;

        _context = ALC.CreateContext(_device, (int[])null!);
        if (_context == ALContext.Null)
            throw new Exception("Failed to create OpenAL context");

        ALC.MakeContextCurrent(_context);

        _keyBuffer = AL.GenBuffer();
        _keySource = AL.GenSource();
        LoadWavFromAssets("avares://TyperPro/Assets/Button.wav", _keyBuffer, _keySource, gain: 0.25f);

        _tickBuffer = AL.GenBuffer();
        _tickSource = AL.GenSource();
        LoadSineBeep(_tickBuffer, _tickSource, frequency: 600f, durationMs: 80,  gain: 0.35f, fadeOutRatio: 0.4f);

        _goBuffer = AL.GenBuffer();
        _goSource = AL.GenSource();
        LoadSineBeep(_goBuffer,   _goSource,   frequency: 1000f, durationMs: 220, gain: 0.45f, fadeOutRatio: 0.5f);
    }

    private static void LoadWavFromAssets(string uri, int buffer, int source, float gain)
    {
        using var stream = AssetLoader.Open(new Uri(uri));
        using var reader = new BinaryReader(stream);

        var riff = new string(reader.ReadChars(4));
        if (riff != "RIFF") throw new Exception("Not a valid WAV file");
        reader.ReadInt32();
        var wave = new string(reader.ReadChars(4));
        if (wave != "WAVE") throw new Exception("Not a valid WAV file");

        int   sampleRate    = 44100;
        short channels      = 1;
        short bitsPerSample = 16;
        byte[]? audioData   = null;

        while (stream.Position < stream.Length - 8)
        {
            var chunkId   = new string(reader.ReadChars(4));
            int chunkSize = reader.ReadInt32();
            switch (chunkId)
            {
                case "fmt ":
                    reader.ReadInt16();
                    channels       = reader.ReadInt16();
                    sampleRate     = reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt16();
                    bitsPerSample  = reader.ReadInt16();
                    if (chunkSize > 16) reader.ReadBytes(chunkSize - 16);
                    break;
                case "data":
                    audioData = reader.ReadBytes(chunkSize);
                    break;
                default:
                    reader.ReadBytes(chunkSize);
                    break;
            }
            if (audioData != null) break;
        }

        if (audioData == null) throw new Exception("WAV file contained no data chunk");

        var format = channels switch
        {
            1 => bitsPerSample == 8 ? ALFormat.Mono8   : ALFormat.Mono16,
            2 => bitsPerSample == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16,
            _ => throw new Exception($"Unsupported channel count: {channels}")
        };

        AL.BufferData(buffer, format, audioData, sampleRate);
        AL.Source(source, ALSourcei.Buffer, buffer);
        AL.Source(source, ALSourcef.Gain,   gain);
    }

    private static void LoadSineBeep(int buffer, int source, float frequency, int durationMs, float gain, float fadeOutRatio)
    {
        const int sampleRate = 44100;
        int totalSamples = sampleRate * durationMs / 1000;
        int fadeStart    = (int)(totalSamples * (1f - fadeOutRatio));

        var data = new short[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            double t         = (double)i / sampleRate;
            double sineValue = Math.Sin(2.0 * Math.PI * frequency * t);
            double envelope  = 1.0;

            if (i >= fadeStart)
            {
                double fp = (double)(i - fadeStart) / (totalSamples - fadeStart);
                envelope = 0.5 * (1.0 + Math.Cos(Math.PI * fp));
            }

            double attackSamples = sampleRate * 0.004;
            if (i < attackSamples) envelope *= i / attackSamples;

            data[i] = (short)(sineValue * envelope * short.MaxValue * 0.85);
        }

        var bytes = new byte[data.Length * 2];
        Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);

        AL.BufferData(buffer, ALFormat.Mono16, bytes, sampleRate);
        AL.Source(source, ALSourcei.Buffer, buffer);
        AL.Source(source, ALSourcef.Gain,   gain);
    }

    public void Play()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastPlayed).TotalMilliseconds < MinIntervalMs) return;
        _lastPlayed = now;
        AL.SourceStop(_keySource);
        AL.SourcePlay(_keySource);
    }

    public void PlayCountdownTick()
    {
        AL.SourceStop(_tickSource);
        AL.SourcePlay(_tickSource);
    }

    public void PlayCountdownGo()
    {
        AL.SourceStop(_goSource);
        AL.SourcePlay(_goSource);
    }

    public void Dispose()
    {
        AL.DeleteSource(_keySource);
        AL.DeleteSource(_tickSource);
        AL.DeleteSource(_goSource);
        AL.DeleteBuffer(_keyBuffer);
        AL.DeleteBuffer(_tickBuffer);
        AL.DeleteBuffer(_goBuffer);
        ALC.MakeContextCurrent(ALContext.Null);
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
    }
}