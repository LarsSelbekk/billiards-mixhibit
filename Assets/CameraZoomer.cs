using UnityEngine;
using UnityEngine.InputSystem;

public class CameraZoomer : MonoBehaviour
{
    public SpectatorViewSetup spectatorViewSetup;
    public float speed = 0.01f;

    private float _actuation;
    private Camera _mainCamera;

    private void OnEnable()
    {
        spectatorViewSetup.Input.Projectator.Zoom.performed += OnZoom;
        spectatorViewSetup.Input.Projectator.Zoom.canceled += OnZoomStop;
        _mainCamera = Camera.main;
    }

    private void OnDisable()
    {
        spectatorViewSetup.Input.Projectator.Zoom.performed -= OnZoom;
        spectatorViewSetup.Input.Projectator.Zoom.canceled -= OnZoomStop;
    }

    private void FixedUpdate()
    {
        if (_actuation == 0) return;
        _mainCamera.orthographicSize += speed * _actuation;
    }

    private void OnZoom(InputAction.CallbackContext context)
    {
        _actuation = context.ReadValue<float>();
    }

    private void OnZoomStop(InputAction.CallbackContext context)
    {
        _actuation = 0;
    }

}
