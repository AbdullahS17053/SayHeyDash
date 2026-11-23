using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;

namespace SayHeyDash.EditorTools.Handover
{
    /// <summary>
    /// Encodes the handover tutorial video (H.264 + AAC .mp4) with Unity's built-in MediaEncoder,
    /// so no external video tools are needed. Input is a manifest written by
    /// Tools/Tutorial/compose_tutorial.ps1:
    /// <code>
    /// size 1920 1080
    /// fps 12
    /// frames &lt;text file: one image path per video frame (PNG/JPG, exactly "size")&gt;
    /// audio &lt;16-bit PCM .wav&gt; &lt;start seconds&gt;     (any number of lines; mixed to mono 48 kHz)
    /// out &lt;.mp4 path&gt;
    /// </code>
    /// Batchmode: Unity.exe -batchmode -quit -projectPath &lt;path&gt;
    ///   -executeMethod SayHeyDash.EditorTools.Handover.TutorialVideoEncoder.EncodeFromCommandLine -videoManifest &lt;file&gt;
    /// </summary>
    public static class TutorialVideoEncoder
    {
        private const int SampleRate = 48000;

        public static void EncodeFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-videoManifest");
            bool ok = i >= 0 && i + 1 < args.Length && Encode(args[i + 1]);
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Encode(string manifestPath)
        {
            int width = 0, height = 0, fps = 12;
            string framesList = null, outPath = null;
            var audio = new List<(string path, double start)>();
            foreach (string raw in File.ReadAllLines(manifestPath))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int sp = line.IndexOf(' ');
                string key = line.Substring(0, sp), rest = line.Substring(sp + 1).Trim();
                switch (key)
                {
                    case "size":
                        string[] wh = rest.Split(' ');
                        width = int.Parse(wh[0]); height = int.Parse(wh[1]);
                        break;
                    case "fps": fps = int.Parse(rest); break;
                    case "frames": framesList = rest; break;
                    case "out": outPath = rest; break;
                    case "audio":
                        int last = rest.LastIndexOf(' ');
                        audio.Add((rest.Substring(0, last), double.Parse(rest.Substring(last + 1), CultureInfo.InvariantCulture)));
                        break;
                }
            }

            string[] frames = File.ReadAllLines(framesList);
            if (SampleRate % fps != 0) { Debug.LogError("[TutorialVideoEncoder] fps must divide 48000."); return false; }
            int samplesPerFrame = SampleRate / fps;
            float[] mix = MixAudio(audio, frames.Length * samplesPerFrame);

            var videoAttrs = new VideoTrackAttributes
            {
                frameRate = new MediaRational(fps),
                width = (uint)width,
                height = (uint)height,
                includeAlpha = false,
                bitRateMode = VideoBitrateMode.High,
            };
            var audioAttrs = new AudioTrackAttributes
            {
                sampleRate = new MediaRational(SampleRate),
                channelCount = 1,
                language = "en",
            };

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));
            if (File.Exists(outPath)) File.Delete(outPath);

            var frame = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var loader = new Texture2D(2, 2);
            string loaded = null;
            try
            {
                using (var encoder = new MediaEncoder(outPath, videoAttrs, audioAttrs))
                using (var samples = new NativeArray<float>(samplesPerFrame, Allocator.Persistent))
                {
                    for (int f = 0; f < frames.Length; f++)
                    {
                        string path = frames[f].Trim();
                        if (path != loaded)
                        {
                            if (!loader.LoadImage(File.ReadAllBytes(path)) || loader.width != width || loader.height != height)
                            {
                                Debug.LogError($"[TutorialVideoEncoder] Bad frame {path} ({loader.width}x{loader.height}).");
                                return false;
                            }
                            frame.SetPixels32(loader.GetPixels32());
                            frame.Apply();
                            loaded = path;
                        }
                        encoder.AddFrame(frame);
                        NativeArray<float>.Copy(mix, f * samplesPerFrame, samples, 0, samplesPerFrame);
                        encoder.AddSamples(samples);
                        if (f % 240 == 0) Debug.Log($"[TutorialVideoEncoder] frame {f}/{frames.Length}");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(frame);
                UnityEngine.Object.DestroyImmediate(loader);
            }

            Debug.Log($"[TutorialVideoEncoder] Wrote {outPath}: {frames.Length} frames, {frames.Length / (float)fps:0.0} s.");
            return true;
        }

        private static float[] MixAudio(List<(string path, double start)> clips, int totalSamples)
        {
            var mix = new float[totalSamples];
            foreach (var (path, start) in clips)
            {
                float[] clip = ReadWavMono(path, out int rate);
                double step = rate / (double)SampleRate;
                int offset = (int)Math.Round(start * SampleRate);
                int count = (int)(clip.Length / step);
                for (int i = 0; i < count && offset + i < totalSamples; i++)
                {
                    double src = i * step;
                    int s0 = (int)src;
                    int s1 = Math.Min(s0 + 1, clip.Length - 1);
                    float v = Mathf.Lerp(clip[s0], clip[s1], (float)(src - s0));
                    if (offset + i >= 0) mix[offset + i] = Mathf.Clamp(mix[offset + i] + v, -1f, 1f);
                }
            }
            return mix;
        }

        /// <summary>Reads a 16-bit PCM WAV and averages its channels to mono.</summary>
        private static float[] ReadWavMono(string path, out int sampleRate)
        {
            byte[] b = File.ReadAllBytes(path);
            int channels = 1, bits = 16;
            sampleRate = SampleRate;
            int pos = 12;
            while (pos + 8 <= b.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(b, pos, 4);
                int size = BitConverter.ToInt32(b, pos + 4);
                if (id == "fmt ")
                {
                    channels = BitConverter.ToInt16(b, pos + 10);
                    sampleRate = BitConverter.ToInt32(b, pos + 12);
                    bits = BitConverter.ToInt16(b, pos + 22);
                }
                else if (id == "data")
                {
                    if (bits != 16) throw new NotSupportedException($"{path}: {bits}-bit WAV");
                    size = Math.Min(size, b.Length - pos - 8);
                    int frames = size / (2 * channels);
                    var result = new float[frames];
                    for (int f = 0; f < frames; f++)
                    {
                        float sum = 0;
                        for (int c = 0; c < channels; c++)
                            sum += BitConverter.ToInt16(b, pos + 8 + (f * channels + c) * 2) / 32768f;
                        result[f] = sum / channels;
                    }
                    return result;
                }
                pos += 8 + size + (size & 1);
            }
            throw new InvalidDataException($"{path}: no data chunk");
        }
    }
}
