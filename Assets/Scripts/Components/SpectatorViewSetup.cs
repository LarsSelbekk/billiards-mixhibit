using UnityEngine;

public class SpectatorViewSetup : MonoBehaviour
{
    private BilliardsMixhibitInputActions _input;

    public BilliardsMixhibitInputActions Input =>
        // Script execution order seems to be random on (some) Android devices, so this can't be initialized in
        // Awake or something.
        _input ??= new BilliardsMixhibitInputActions();

    public bool isMobileSpectatorView;

    private void OnEnable()
    {
        Input.BaseSpectatorView.Enable();

        if (isMobileSpectatorView)
        {
            Input.XRIUI.Enable();
            Input.XRIHead.Enable();
            Input.Mobile.Enable();
        }
    }

    private void OnDisable()
    {
        Input.Mobile.Enable();
        Input.XRIHead.Disable();
        Input.XRIUI.Disable();
        Input.BaseSpectatorView.Disable();
    }

    private void OnDestroy()
    {
        Input.Dispose();
    }
}
