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

        _context = ALC.CreateContext(_device, (int[])null!);
        if (_context == ALContext.Null)
            throw new Exception("Failed to create OpenAL context");

        ALC.MakeContextCurrent(_context);

        _buffer = AL.GenBuffer();
        _source = AL.GenSource();

        LoadWavFromAssets("avares://TyperPro/Assets/Button.wav");
    }

    private void LoadWavFromAssets(string uri)
    {
        using var stream = AssetLoader.Open(new Uri(uri));
        using var reader = new BinaryReader(stream);

        // RIFF header
        var riff = new string(reader.ReadChars(4));
        if (riff != "RIFF")
            throw new Exception("Not a valid WAV file (missing RIFF header)");

        reader.ReadInt32();                          // file size (unused)

        var wave = new string(reader.ReadChars(4));
        if (wave != "WAVE")
            throw new Exception("Not a valid WAV file (missing WAVE marker)");

        // Walk chunks until we find "fmt " and "data"
        int sampleRate    = 44100;
        short channels    = 1;
        short bitsPerSample = 16;
        byte[]? audioData = null;

        while (stream.Position < stream.Length - 8)
        {
            var chunkId   = new string(reader.ReadChars(4));
            int chunkSize = reader.ReadInt32();

            switch (chunkId)
            {
                case "fmt ":
                    reader.ReadInt16();              // audio format (1 = PCM)
                    channels       = reader.ReadInt16();
                    sampleRate     = reader.ReadInt32();
                    reader.ReadInt32();              // byte rate
                    reader.ReadInt16();              // block align
                    bitsPerSample  = reader.ReadInt16();
                    if (chunkSize > 16)
                        reader.ReadBytes(chunkSize - 16); // skip extension bytes
                    break;

                case "data":
                    audioData = reader.ReadBytes(chunkSize);
                    break;

                default:
                    // Skip unknown chunks (e.g. "LIST", "INFO", "id3 ", etc.)
                    reader.ReadBytes(chunkSize);
                    break;
            }

            if (audioData != null) break;
        }

        if (audioData == null)
            throw new Exception("WAV file contained no data chunk");

        var format = channels switch
        {
            1 => bitsPerSample == 8 ? ALFormat.Mono8   : ALFormat.Mono16,
            2 => bitsPerSample == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16,
            _ => throw new Exception($"Unsupported channel count: {channels}")
        };

        AL.BufferData(_buffer, format, audioData, sampleRate);
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