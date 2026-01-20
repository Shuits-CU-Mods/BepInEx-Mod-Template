using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BepInExModTemplate.Helpers
{
    internal class CustomAudioManager
    {
        internal static ManualLogSource Logger;
        public static Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();

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
                if (!manifestName.StartsWith("OwO.Audio."))
                    continue;
                string clipName = manifestName.Substring(10);
                string fileExt = clipName.Substring(clipName.IndexOf(".") + 1);
                if (validExtensions.Contains(fileExt))
                {
                    audioClips.Add(clipName, FileLoader.LoadEmbeddedAudio(clipName));
                    Logger.LogInfo($"\t\tAdded audio file {clipName} to audioClips dictionary");
                }
                else
                    continue;
            }
            Logger.LogInfo("\tLoaded all audio files!");
        }

        public static AudioClip GetAudioClip(string clipName)
        {
            if (audioClips.ContainsKey(clipName))
                return audioClips[clipName];
            else
            {
                Logger.LogError($"Audio clip by name {clipName} does not exist");
                return null;
            }
        }
    }
}