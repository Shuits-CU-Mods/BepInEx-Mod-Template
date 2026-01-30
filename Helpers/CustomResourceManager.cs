using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace BepInExModTemplate.Helpers
{
    internal class CustomResourceManager
    {
        internal static ManualLogSource Logger;

        internal static class LoadedResources
        {
            public static Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
            public static Dictionary<string, TMP_FontAsset> tmpFontAssets = new Dictionary<string, TMP_FontAsset>();
        }

        internal class AudioManager
        {
            private static readonly string[] validExtensions = new string[]
            {
                "wav",
                "mp1",
                "mp2",
                "mp3",
                "cue",
                "aif",
                "aiff"
            };

            public static void Initialize(ManualLogSource logger)
            {
                Logger = logger;
                Logger.LogInfo("Custom Audio Manager loaded!");
                Logger.LogInfo("\tLoading all embedded audio files...");
                foreach (string manifestName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                {
                    string clipName = FileManager.FileNameFromResource(manifestName);
                    string fileExt = clipName.Substring(clipName.IndexOf(".") + 1);
                    if (validExtensions.Contains(fileExt))
                    {
                        LoadedResources.audioClips[clipName] = FileManager.LoadEmbeddedAudio(clipName);
                        Logger.LogInfo($"\t\tAdded audio file {clipName} to audioClips dictionary");
                    }
                    else
                        continue;
                }
                Logger.LogInfo("\tLoaded all audio files!");
            }

            public static AudioClip GetAudioClip(string clipName)
            {
                if (LoadedResources.audioClips.ContainsKey(clipName))
                    return LoadedResources.audioClips[clipName];
                else
                {
                    Logger.LogError($"Audio clip by name {clipName} does not exist");
                    return null;
                }
            }
        }

        internal class FontManager
        {
            private static readonly string[] validExtensions = new string[]
            {
                "otf",
                "ttf",
                "woff",
                "woff2"
            };

            public static void Initialize(ManualLogSource logger)
            {
                Logger = logger;
                Logger.LogInfo("Custom Font Manager loaded!");
                Logger.LogInfo("\tLoading all embedded font files...");
                foreach (string manifestName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                {
                    string fontName = FileManager.FileNameFromResource(manifestName);
                    string fileExt = fontName.Substring(fontName.IndexOf(".") + 1);
                    if (validExtensions.Contains(fileExt))
                    {
                        LoadedResources.tmpFontAssets[fontName] = FileManager.LoadEmbeddedFont(fontName);
                        Logger.LogInfo($"\t\tAdded font file {fontName} to tmpFontAssets dictionary");
                    }
                    else
                        continue;
                }
                Logger.LogInfo("\tLoaded all font files!");
            }

            public static TMP_FontAsset GetTMPFont(string fontName)
            {
                if (LoadedResources.tmpFontAssets.ContainsKey(fontName))
                    return LoadedResources.tmpFontAssets[fontName];
                else
                {
                    Logger.LogError($"Font by name {fontName} does not exist");
                    return null;
                }
            }
        }
    }
}