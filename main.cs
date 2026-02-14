using BepInEx;
using BepInEx.Logging;
using BepInExModTemplate.Helpers;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BepInExModTemplate
{
    public static class SharedState
    {
    }

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class BepInExModTemplate : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public const string pluginGuid = "USER.casualtiesunknown.MOD";
        public const string pluginName = "MOD NAME";

        // Version 0.0.0.0 results in no release build upon commit to github repo
        // Year.Month.Version.Bugfix
        public const string pluginVersion = "0.0.0.0";

        public static BepInExModTemplate Instance;

        public static bool PatchByName = true;

        public void Awake()
        {
            Instance = this;
            logger = Logger;

            logger.LogInfo("Awake() ran - mod loaded!");

            Harmony harmony = new Harmony(pluginGuid);

            StartCoroutine(ConfirmGameVersion(harmony));
        }

        public static void Log(string message)
        {
            logger.LogInfo(message);
        }

        public static IEnumerator ConfirmGameVersion(Harmony harmony)
        {
            yield return GameVersionChecker.Run(harmony);

            List<string> SafeVersions = new List<string>()
            {
                "v5p4",
                "v5p5",
                "v5d"
            };

            if (GameVersionChecker.Status == GameVersionChecker.PatchStatus.Safe && SafeVersions.Contains(GameVersionChecker.ActiveVersion))
            {
                CustomResourceManager.AudioManager.Initialize(logger);
                CustomResourceManager.FontManager.Initialize(logger);

                if (PatchByName)
                    CustomPatcher.PatchByName(harmony, typeof(MyPatches));
                else
                    harmony.PatchAll();
            }
            else
            {
                Debug.Log("Version is not safe to patch, disabling mod...");
            }
        }
    }

    public class MyPatches
    {
    }
}