using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

public class WorldLockParentMover : MonoBehaviour
{
    public float movementSpeed = 0.01f;
    public float rotationSpeed = 0.1f;
    public SpectatorViewSetup spectatorViewSetup;
    public ARSession arSession;
    private Vector3 _movementVector, _rotationVector;
    private GameObject _worldLockParent;

    private void OnEnable()
    {
        spectatorViewSetup.Input.SpectatorView.Recenter.performed += OnRecenter;
        spectatorViewSetup.Input.SpectatorView.MoveOrigin.performed += OnMoveOrigin;
        spectatorViewSetup.Input.SpectatorView.RotateOrigin.performed += OnRotateOrigin;
    }

    private void OnDisable()
    {
        spectatorViewSetup.Input.SpectatorView.Recenter.performed -= OnRecenter;
        spectatorViewSetup.Input.SpectatorView.MoveOrigin.performed -= OnMoveOrigin;
        spectatorViewSetup.Input.SpectatorView.RotateOrigin.performed -= OnRotateOrigin;
    }

    private void FixedUpdate()
    {
        if (_movementVector == Vector3.zero && _rotationVector == Vector3.zero) return;
        if (!EnsureWorldLockParentAvailable()) return;
        _worldLockParent.transform.Translate(_movementVector * movementSpeed);
        _worldLockParent.transform.Rotate(_rotationVector * rotationSpeed);
    }

    public void OnRecenter(InputAction.CallbackContext _)
    {
        arSession.Reset();
    }

    public void OnMoveOrigin(InputAction.CallbackContext context)
    {
        _movementVector = context.ReadValue<Vector3>();
    }

    public void OnRotateOrigin(InputAction.CallbackContext context)
    {
        _rotationVector = context.ReadValue<Vector3>();
    }

    private bool EnsureWorldLockParentAvailable()
    {
        if (_worldLockParent == null) _worldLockParent = GameObject.FindWithTag("WorldLockParent");

        return _worldLockParent != null;
    }
}
