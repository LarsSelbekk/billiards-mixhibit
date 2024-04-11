using System;
using UnityEngine;

public class CameraFacer : MonoBehaviour
{
    private Transform _cameraTransform;

    private void Start()
    {
        if (Camera.main is not { } mainCamera) throw new Exception("No main camera");
        _cameraTransform = mainCamera.transform;
    }

    private void LateUpdate()
    {
        transform.LookAt(_cameraTransform.position);
    }
}
