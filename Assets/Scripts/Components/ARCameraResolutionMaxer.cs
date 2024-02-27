// #define DEBUG_AR_CAMERA_RESOLUTION_MAXER

using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARCameraManager))]
public class ARCameraResolutionMaxer : MonoBehaviour
{
    private ARCameraManager _arCameraManager;

    private void OnEnable()
    {
        _arCameraManager = GetComponent<ARCameraManager>();

        _arCameraManager.frameReceived += OnFrameReceived;
    }

    private void OnFrameReceived(ARCameraFrameEventArgs _)
    {
        var configs = _arCameraManager.GetConfigurations(Allocator.Temp);
        #if DEBUG_AR_CAMERA_RESOLUTION_MAXER
        Debug.Log(
            $"Received first frame. Camera configurations are: {string.Join("\n", configs)}\nChoosing {configs[^1]}"
        );
        #endif
        _arCameraManager.subsystem.currentConfiguration = configs[^1];

        _arCameraManager.frameReceived -= OnFrameReceived;
    }

    private void OnDisable()
    {
        _arCameraManager.frameReceived -= OnFrameReceived;
    }
}
