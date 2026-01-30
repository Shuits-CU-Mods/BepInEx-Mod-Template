using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;

namespace BepInExModTemplate.Helpers
{
    internal static class FileManager
    {
        // Loads files that have been set to Embedded Resource in the Build Action file properties
        internal static T WithEmbeddedStream<T>(string fileName, Func<string, Stream, T> useStream, string folderName = null)
        {
            // Gets the file name. If it isn't exact, it returns null
            Assembly asm = Assembly.GetExecutingAssembly();
            string resourceName = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(fileName));
            if (resourceName == null)
            {
                Debug.LogError($"File by the name of {fileName} does not exist. Check capitalization and file extension");
                return default;
            }

            if (!resourceName.StartsWith(asm.GetName().Name + "." + (folderName != null ? folderName + "." : "")))
            {
                Debug.LogError($"File does not exist in embedded resources");
                return default;
            }

            using (Stream stream = asm.GetManifestResourceStream(resourceName))
            {
                return useStream(FileNameFromResource(resourceName), stream);
            }
        }

        internal static (string, byte[]) LoadFileBytes(string fileName)
        {
            return WithEmbeddedStream(fileName, (name, stream) =>
            {
                byte[] fileData = new byte[stream.Length];
                stream.Read(fileData, 0, fileData.Length);
                return (name, fileData);
            });
        }

        internal static AudioClip LoadEmbeddedAudio(string fileName)
        {
            return WithEmbeddedStream(fileName, (name, stream) =>
            {
                string clipName = name;
                string fileExt = clipName.Substring(clipName.IndexOf(".") + 1);
                ISampleProvider provider;
                switch (fileExt)
                {
                    case "wav":
                        using (WaveFileReader reader = new WaveFileReader(stream)) { provider = reader.ToSampleProvider(); }
                        break;

                    case "mp1":
                    case "mp2":
                    case "mp3":
                        using (Mp3FileReader reader = new Mp3FileReader(stream)) { provider = reader.ToSampleProvider(); }
                        break;

                    case "cue":
                        using (CueWaveFileReader reader = new CueWaveFileReader(stream)) { provider = reader.ToSampleProvider(); }
                        break;

                    case "aif":
                    case "aiff":
                        using (AiffFileReader reader = new AiffFileReader(stream)) { provider = reader.ToSampleProvider(); }
                        break;

                    default:
                        Debug.LogError($"Could not load audio file {fileName}: Unknown file extension {fileExt}");
                        return null;
                }

                // Reads file data sample rate and channels to make a samples data array
                List<float> samples = new List<float>();
                float[] buffer = new float[provider.WaveFormat.SampleRate * provider.WaveFormat.Channels];
                int read;
                while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    samples.AddRange(buffer.Take(read));
                }

                // Sets mandatory variables for sample rate, channels, and samples/channel
                WaveFormat waveFormat = provider.WaveFormat;
                int sampleRate = waveFormat.SampleRate;
                int channels = waveFormat.Channels;
                int samplesPerChannel = samples.Count / channels;

                // Creates and returns the audio clip
                AudioClip clip = AudioClip.Create(fileName, samplesPerChannel, channels, sampleRate, false);
                clip.SetData(samples.ToArray(), 0);
                return clip;
            });
        }

        internal static Sprite LoadEmbeddedSprite(string fileName, float ppu = 100, FilterMode filterMode = FilterMode.Point, int widthMultiplier = 1, int heightMultiplier = 1)
        {
            return WithEmbeddedStream(fileName, (name, stream) =>
            {
                byte[] fileData = new byte[stream.Length];
                stream.Read(fileData, 0, fileData.Length);
                // Creates texture and sprite by the stream data
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBAHalf, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = filterMode,
                    name = name
                };
                texture.LoadImage(fileData);
                texture.Apply();

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    ppu
                );
                sprite.name = name;
                return sprite;
            });
        }

        internal static string SaveFileToAppData(string fileName)
        {
            return SaveFileToDir(fileName, Path.Combine(Application.persistentDataPath, Assembly.GetExecutingAssembly().GetName().Name));
        }

        internal static string SaveFileToGameDir(string fileName, string relativeDir)
        {
            return SaveFileToDir(fileName, Path.Combine(Application.dataPath, relativeDir));
        }

        internal static string SaveFileToDir(string fileName, string dir)
        {
            (string, byte[]) fileInfo = LoadFileBytes(fileName);
            Directory.CreateDirectory(dir);
            string filePath = Path.Combine(dir, fileInfo.Item1);
            SHA256 sha256 = SHA256.Create();
            if (!File.Exists(filePath))
                File.WriteAllBytes(filePath, fileInfo.Item2);
            else
            {
                byte[] existingFileSha256 = sha256.ComputeHash(File.OpenRead(filePath));
                byte[] embeddedFileSha256 = sha256.ComputeHash(fileInfo.Item2);
                if (!embeddedFileSha256.SequenceEqual(existingFileSha256))
                {
                    Debug.Log("File hash isn't the same, overwriting");
                    File.WriteAllBytes(filePath, fileInfo.Item2);
                }
            }

            return filePath;
        }

        internal static TMP_FontAsset LoadEmbeddedFont(string fileName)
        {
            string fontPath = SaveFileToAppData(fileName);
            Font unityFont = new Font(fontPath);
            return TMP_FontAsset.CreateFontAsset(unityFont);
        }

        internal static string FileNameFromResource(string resourceName)
        {
            int extensionDotIndex = resourceName.IndexOf(".");
            int fileDotIndex = resourceName.LastIndexOf(".", extensionDotIndex - 1);
            return resourceName.Substring(fileDotIndex + 1);
        }
    }
}