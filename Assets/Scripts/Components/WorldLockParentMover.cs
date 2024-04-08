using UnityEngine;
using UnityEngine.InputSystem;

public class WorldLockParentMover : MonoBehaviour
{
    public float movementSpeed = 0.01f;
    public float rotationSpeed = 0.1f;
    public SpectatorViewSetup spectatorViewSetup;
    private Vector3 _movementVector, _rotationVector;
    private GameObject _worldLockParent;

    private void OnEnable()
    {
        spectatorViewSetup.Input.BaseSpectatorView.MoveOrigin.performed += OnMoveOrigin;
        spectatorViewSetup.Input.BaseSpectatorView.RotateOrigin.performed += OnRotateOrigin;
    }

    private void OnDisable()
    {
        spectatorViewSetup.Input.BaseSpectatorView.MoveOrigin.performed -= OnMoveOrigin;
        spectatorViewSetup.Input.BaseSpectatorView.RotateOrigin.performed -= OnRotateOrigin;
    }

    private void FixedUpdate()
    {
        if (_movementVector == Vector3.zero && _rotationVector == Vector3.zero) return;
        if (!EnsureWorldLockParentAvailable()) return;
        _worldLockParent.transform.position += _movementVector * movementSpeed;
        _worldLockParent.transform.eulerAngles +=_rotationVector * rotationSpeed;
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
