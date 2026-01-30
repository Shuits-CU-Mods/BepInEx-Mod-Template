using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BepInExModTemplate.Helpers
{
    public class PatchDefinitionException : Exception
    {
        public string PatchMethodName { get; }

        public PatchDefinitionException(string patchMethodName, string message) : base(message)
        {
            PatchMethodName = patchMethodName;
        }
    }

    internal class CustomPatcher
    {
        internal static void PatchByName(Harmony harmony, Type targetClassType)
        {
            List<MethodInfo> patches = typeof(MyPatches).GetMethods(BindingFlags.Static | BindingFlags.Public).ToList();
            foreach (MethodInfo patch in patches)
            {
                try
                {
                    string[] splitName = patch.Name.Replace("__", "$").Split('_');
                    for (int i = 0; i < splitName.Length; i++)
                        splitName[i] = splitName[i].Replace("$", "_");
                    if (splitName.Length < 3)
                        throw new PatchDefinitionException(patch.Name, "Expected format: TargetClass_TargetMethod_PatchType[_Version]");

                    if (splitName.Length >= 4)
                        if (splitName[3] != GameVersionChecker.ActiveVersion)
                        {
                            Debug.Log($"{patch.Name} is not supported by version {GameVersionChecker.ActiveVersion}");
                            continue;
                        }

                    string targetType = splitName[0];
                    MethodType targetMethodType;
                    if (splitName[1].Contains("get_"))
                        targetMethodType = MethodType.Getter;
                    else if (splitName[1].Contains("set_"))
                        targetMethodType = MethodType.Setter;
                    else
                        targetMethodType = MethodType.Normal;
                    string ogTargetMethod = splitName[1];
                    string targetMethod = splitName[1].Replace("get_", "").Replace("set_", "");
                    string patchType = splitName[2];

                    MethodInfo patchScript = targetClassType.GetMethod(patch.Name);

                    ParameterInfo[] parameters = patchScript.GetParameters();
                    Type[] scriptArgTypes = parameters.Where(p => p.Name != "__instance").Select(p => p.ParameterType).ToArray();

                    MethodInfo ogScript = null;
                    switch (targetMethodType)
                    {
                        case MethodType.Enumerator:
                        case MethodType.Normal:
                            try
                            {
                                ogScript = AccessTools.Method(AccessTools.TypeByName(targetType), targetMethod);
                            }
                            catch (Exception)
                            {
                                ogScript = AccessTools.Method(AccessTools.TypeByName(targetType), targetMethod, scriptArgTypes);
                            }
                            break;

                        case MethodType.Getter:
                            ogScript = AccessTools.PropertyGetter(AccessTools.TypeByName(targetType), targetMethod);
                            break;

                        case MethodType.Setter:
                        case MethodType.Constructor:
                        case MethodType.StaticConstructor:
                        default:
                            throw new Exception($"Unknown patch method\nPatch method type \"{targetMethodType}\" currently has no handling");
                    }

                    List<string> validPatchTypes = new List<string>
                    {
                        "Prefix",
                        "Postfix",
                        "Transpiler"
                    };
                    if (ogScript == null || patchScript == null || !validPatchTypes.Contains(patchType))
                    {
                        throw new PatchDefinitionException(patch.Name, "Expected format: TargetClass_TargetMethod_PatchType[_Version]");
                    }
                    HarmonyMethod harmonyMethod = new HarmonyMethod(patchScript)
                    {
                        methodType = targetMethodType
                    };

                    HarmonyMethod postfix = null;
                    HarmonyMethod prefix = null;
                    HarmonyMethod transpiler = null;
                    switch (patchType)
                    {
                        case "Prefix":
                            prefix = harmonyMethod;
                            break;

                        case "Postfix":
                            postfix = harmonyMethod;
                            break;

                        case "Transpiler":
                            transpiler = harmonyMethod;
                            break;
                    }
                    harmony.Patch(ogScript, prefix: prefix, postfix: postfix, transpiler: transpiler);
                    Debug.Log("Patched " + targetType + "." + targetMethod + " as a " + patchType);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to patch {patch.Name}");
                    Debug.LogError(exception);
                }
            }
        }
    }
}