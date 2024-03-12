using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

public class ARRecenterer : MonoBehaviour
{
    public SpectatorViewSetup spectatorViewSetup;
    public ARSession arSession;

    private void OnEnable()
    {
        spectatorViewSetup.Input.Mobile.Recenter.performed += OnRecenter;
    }

    private void OnDisable()
    {
        spectatorViewSetup.Input.Mobile.Recenter.performed -= OnRecenter;
    }

    public void OnRecenter(InputAction.CallbackContext _)
    {
        arSession.Reset();
    }
}
