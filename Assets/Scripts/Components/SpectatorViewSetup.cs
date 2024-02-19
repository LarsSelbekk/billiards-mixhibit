using UnityEngine;

public class SpectatorViewSetup : MonoBehaviour
{
    private BilliardsMixhibitInputActions _input;

    public BilliardsMixhibitInputActions Input =>
        // Script execution order seems to be random on (some) Android devices, so this can't be initialized in
        // Awake or something.
        _input ??= new BilliardsMixhibitInputActions();

    private void OnEnable()
    {
        Input.XRIUI.Enable();
        Input.XRIHead.Enable();
        Input.SpectatorView.Enable();
    }

    private void OnDisable()
    {
        Input.XRIUI.Disable();
        Input.XRIHead.Disable();
        Input.SpectatorView.Disable();
    }

    private void OnDestroy()
    {
        Input.Dispose();
    }
}
