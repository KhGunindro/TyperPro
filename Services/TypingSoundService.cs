using System;
using System.IO;
using Avalonia.Platform;
using OpenTK.Audio.OpenAL;

namespace TyperPro.Services;

public sealed class TypingSoundService : IDisposable
{
    // ── Keystroke sound (Button.wav) ─────────────────────────────────────────
    private readonly int _keyBuffer;
    private readonly int _keySource;
    private DateTime _lastPlayed = DateTime.MinValue;
    private const int MinIntervalMs = 25;

    // ── Countdown tick sound (procedurally generated sine beep) ─────────────
    private readonly int _tickBuffer;
    private readonly int _tickSource;

    // ── Countdown "GO" sound (higher pitched, longer beep) ───────────────────
    private readonly int _goBuffer;
    private readonly int _goSource;

    // ── OpenAL device/context ────────────────────────────────────────────────
    private readonly ALDevice  _device;
    private readonly ALContext _context;

    public TypingSoundService()
    {
        _device = ALC.OpenDevice(null);
        if (_device == ALDevice.Null)
            throw new Exception("Failed to open OpenAL device");

        _context = ALC.CreateContext(_device, (int[])null!);
        if (_context == ALContext.Null)
            throw new Exception("Failed to create OpenAL context");

        ALC.MakeContextCurrent(_context);

        // ── Keystroke sound ──────────────────────────────────────────────────
        _keyBuffer = AL.GenBuffer();
        _keySource = AL.GenSource();
        LoadWavFromAssets("avares://TyperPro/Assets/Button.wav", _keyBuffer, _keySource, gain: 0.25f);

        // ── Countdown tick (600 Hz, 80 ms, soft) ─────────────────────────────
        _tickBuffer = AL.GenBuffer();
        _tickSource = AL.GenSource();
        LoadSineBeep(_tickBuffer, _tickSource,
                     frequency: 600f,
                     durationMs: 80,
                     gain: 0.35f,
                     fadeOutRatio: 0.4f);

        // ── GO sound (1000 Hz, 220 ms, brighter) ─────────────────────────────
        _goBuffer = AL.GenBuffer();
        _goSource = AL.GenSource();
        LoadSineBeep(_goBuffer, _goSource,
                     frequency: 1000f,
                     durationMs: 220,
                     gain: 0.45f,
                     fadeOutRatio: 0.5f);
    }

    // ── WAV loader ───────────────────────────────────────────────────────────
    private static void LoadWavFromAssets(string uri, int buffer, int source, float gain)
    {
        using var stream = AssetLoader.Open(new Uri(uri));
        using var reader = new BinaryReader(stream);

        var riff = new string(reader.ReadChars(4));
        if (riff != "RIFF") throw new Exception("Not a valid WAV file (missing RIFF header)");
        reader.ReadInt32();
        var wave = new string(reader.ReadChars(4));
        if (wave != "WAVE") throw new Exception("Not a valid WAV file (missing WAVE marker)");

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
        AL.Source(source, ALSourcef.Gain, gain);
    }

    // ── Procedural sine-wave beep generator ──────────────────────────────────
    /// <summary>
    /// Generates a pure sine tone and uploads it to an OpenAL buffer.
    /// </summary>
    /// <param name="frequency">Tone frequency in Hz (e.g. 600, 1000).</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="gain">Source playback gain (0–1).</param>
    /// <param name="fadeOutRatio">Fraction of the tone to apply a cosine fade-out (0–1).</param>
    private static void LoadSineBeep(
        int   buffer,
        int   source,
        float frequency,
        int   durationMs,
        float gain,
        float fadeOutRatio)
    {
        const int sampleRate = 44100;
        int totalSamples = sampleRate * durationMs / 1000;
        int fadeStart    = (int)(totalSamples * (1f - fadeOutRatio));

        var data = new short[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            // Raw sine
            double t         = (double)i / sampleRate;
            double sineValue = Math.Sin(2.0 * Math.PI * frequency * t);

            // Fade-out envelope (cosine so it's smooth)
            double envelope = 1.0;
            if (i >= fadeStart)
            {
                double fadeProgress = (double)(i - fadeStart) / (totalSamples - fadeStart);
                envelope = 0.5 * (1.0 + Math.Cos(Math.PI * fadeProgress));
            }

            // Tiny attack (first 4 ms) to avoid click
            double attackSamples = sampleRate * 0.004;
            if (i < attackSamples)
                envelope *= i / attackSamples;

            data[i] = (short)(sineValue * envelope * short.MaxValue * 0.85);
        }

        // Convert short[] → byte[]
        var bytes = new byte[data.Length * 2];
        Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);

        AL.BufferData(buffer, ALFormat.Mono16, bytes, sampleRate);
        AL.Source(source, ALSourcei.Buffer, buffer);
        AL.Source(source, ALSourcef.Gain, gain);
    }

    // ── Public play methods ───────────────────────────────────────────────────

    /// <summary>Play the keystroke click sound (throttled to MinIntervalMs).</summary>
    public void Play()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastPlayed).TotalMilliseconds < MinIntervalMs) return;
        _lastPlayed = now;
        AL.SourceStop(_keySource);
        AL.SourcePlay(_keySource);
    }

    /// <summary>Play a short tick beep for countdown numbers 5–2.</summary>
    public void PlayCountdownTick()
    {
        AL.SourceStop(_tickSource);
        AL.SourcePlay(_tickSource);
    }

    /// <summary>Play the bright "GO!" beep on countdown reaching 0.</summary>
    public void PlayCountdownGo()
    {
        AL.SourceStop(_goSource);
        AL.SourcePlay(_goSource);
    }

    // ── Dispose ──────────────────────────────────────────────────────────────
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