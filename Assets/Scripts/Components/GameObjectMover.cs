using UnityEngine;
using UnityEngine.InputSystem;

public class GameObjectMover : MonoBehaviour
{
    public SpectatorViewSetup spectatorViewSetup;
    public float movementSpeed = 0.01f;
    public float rotationSpeed = 0.1f;

    private Vector3 _movementVector, _rotationVector;
    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
    }

    private void OnEnable()
    {
        spectatorViewSetup.Input.BaseSpectatorView.MoveOrigin.performed += OnMove;
        spectatorViewSetup.Input.BaseSpectatorView.RotateOrigin.performed += OnRotate;
    }

    private void OnDisable()
    {
        spectatorViewSetup.Input.BaseSpectatorView.MoveOrigin.performed -= OnMove;
        spectatorViewSetup.Input.BaseSpectatorView.RotateOrigin.performed -= OnRotate;
    }

    private void FixedUpdate()
    {
        if (_movementVector == Vector3.zero && _rotationVector == Vector3.zero) return;
        _transform.position += _transform.TransformDirection(_movementVector * movementSpeed);
        _transform.Rotate(_rotationVector * rotationSpeed);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _movementVector = context.ReadValue<Vector3>();
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        _rotationVector = context.ReadValue<Vector3>();
    }
}
