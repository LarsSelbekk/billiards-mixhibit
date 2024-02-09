#if UNITY_EDITOR

// Uncommenting this will cause stronger guarantees by enforcing on every reload, which seems excessive
// #define TARGET_DEVICE_CHANGER_CHANGE_ON_RECOMPILE
// Uncommenting this will cause the change to run on build, which might be unstable
#define TARGET_DEVICE_CHANGER_CHANGE_ON_BUILD

#if TARGET_DEVICE_CHANGER_CHANGE_ON_BUILD
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.XR.Oculus;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.Management;

[InitializeOnLoad]
public class TargetDeviceChanger
#if TARGET_DEVICE_CHANGER_CHANGE_ON_BUILD
    : IPreprocessBuildWithReport
#endif
{
    public enum TargetType
    {
        Hmd,
        Phone,
    }

    private static readonly Dictionary<string, TargetType?> SceneTargetTypes = new()
    {
        { "Assets/Scenes/Billiards Mixhibit.unity", TargetType.Hmd },
        { "Assets/Scenes/Spectator View.unity", TargetType.Phone },
    };

    private static readonly List<Type> XRLoaderTypes = new() { typeof(OculusLoader), typeof(ARCoreLoader) };


    static TargetDeviceChanger()
    {
        EditorSceneManager.activeSceneChangedInEditMode += (_, newScene) => ChangeTarget(newScene);

#if TARGET_DEVICE_CHANGER_CHANGE_ON_RECOMPILE
        ChangeTarget(SceneManager.GetActiveScene());
#endif
    }


#if TARGET_DEVICE_CHANGER_CHANGE_ON_BUILD
    // Give this high priority on build
    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        ChangeTarget(SceneManager.GetActiveScene());
    }
#endif

    private static void ChangeTarget(Scene newScene)
    {
        // Ignore spurious calls on startup and build
        if (newScene.path == "") return;

        switch (SceneTargetTypes.GetValueOrDefault(newScene.path))
        {
            case TargetType.Phone:
                Debug.Log($"Setting target to phone because of switch to scene {newScene.name}");
                ChangeToPhoneTarget();
                break;
            case TargetType.Hmd:
                Debug.Log($"Setting target to HMD because of switch to scene {newScene.name}");
                ChangeToHmdTarget();
                break;
            default:
                throw new SwitchExpressionException(
                    $"Unknown scene '{newScene.name}' with path '{newScene.path}', map it in {nameof(TargetDeviceChanger)}"
                );
        }

        SetBuildScene(newScene);
        SetBuildOutputFile(newScene);
    }

    private static void ChangeToPhoneTarget()
    {
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        ChangeXRLoader<ARCoreLoader>();
    }

    private static void ChangeToHmdTarget()
    {
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        ChangeXRLoader<OculusLoader>();
    }

    private static void SetBuildScene(Scene newScene)
    {
        EditorBuildSettings.scenes = EditorBuildSettings
            .scenes
            .Select(
                scene =>
                {
                    scene.enabled = scene.path == newScene.path;
                    return scene;
                }
            )
            .ToArray();
    }

    private static void SetBuildOutputFile(Scene newScene)
    {
        var oldPath = EditorUserBuildSettings.GetBuildLocation(BuildTarget.Android);
        var directoryName = Path.GetDirectoryName(oldPath) ?? "";
        var fileName = newScene.name.Replace(" ", "-").ToLowerInvariant() + ".apk";
        var newPath = directoryName is null or "" ? fileName : Path.Combine(directoryName, fileName).Replace("\\", "/");

        if (oldPath == newPath) return;

        EditorUserBuildSettings.SetBuildLocation(BuildTarget.Android, newPath);
    }

    private static void ChangeXRLoader<T>()
        where T : XRLoader
    {
        var xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);

        if (!XRPackageMetadataStore.IsLoaderAssigned(
                typeof(T).FullName,
                BuildTargetGroup.Android
            ))
        {
            if (!XRPackageMetadataStore.AssignLoader(
                    xrSettings.AssignedSettings,
                    typeof(T).FullName,
                    BuildTargetGroup.Android
                ))
            {
                Debug.LogWarning($"Failed to add XR loader {typeof(T).FullName}; make sure you're not in Play mode");
            }
        }

        foreach (var xrLoaderType in XRLoaderTypes)
        {
            if (xrLoaderType == typeof(T)) continue;

            if (!XRPackageMetadataStore.IsLoaderAssigned(
                    xrLoaderType.FullName,
                    BuildTargetGroup.Android
                )) continue;

            if (!XRPackageMetadataStore.RemoveLoader(
                    xrSettings.AssignedSettings,
                    xrLoaderType.FullName,
                    BuildTargetGroup.Android
                ))
            {
                Debug.LogWarning($"Failed to remove XR loader {xrLoaderType.Name}; make sure you're not in Play mode");
            }
        }
    }
}

#endif
