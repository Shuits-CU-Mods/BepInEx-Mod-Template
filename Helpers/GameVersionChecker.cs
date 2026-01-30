using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace BepInExModTemplate.Helpers
{
    internal class GameVersionChecker
    {
        public enum PatchStatus
        {
            Pending = 0,
            Safe = 1,
            Unsafe = -1
        }

        public static PatchStatus Status { get; private set; } = PatchStatus.Pending;
        public static string ActiveVersion { get; private set; } = "";

        internal static IEnumerator Run(Harmony harmony)
        {
            Status = PatchStatus.Pending;
            ActiveVersion = "";

            MethodInfo target = AccessTools.Method(typeof(PreRunScript), "Awake");
            HarmonyMethod prefix = new HarmonyMethod(typeof(GameVersionChecker).GetMethod("VersionCheck", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
            harmony.Patch(target, prefix: prefix);

            while (Status == PatchStatus.Pending)
                yield return null;

            harmony.Unpatch(target, HarmonyPatchType.Prefix);
        }

        internal static void VersionCheck()
        {
            Dictionary<string, (string label, string code)> supportedVersions = new Dictionary<string, (string label, string code)>
            {
                ["Text (TMP) (18)"] = ("V5 Pre-testing 5", "v5p5"),
                ["Text (TMP) (17)"] = ("V5 Pre-testing 4", "v5p4")
            };
            foreach (var version in supportedVersions)
            {
                GameObject obj = GameObject.Find(version.Key);
                if (obj == null)
                    continue;

                if (obj.GetComponent<TextMeshProUGUI>().text.Contains(version.Value.label))
                {
                    ActiveVersion = version.Value.code;
                    Status = PatchStatus.Safe;
                    return;
                }
            }

            Status = PatchStatus.Unsafe;
        }
    }
}