// #define DEBUG_AR_OCCLUSION_TOGGLER

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(AROcclusionManager))]
public class AROcclusionToggler : MonoBehaviour
{
    public SpectatorViewSetup spectatorViewSetup;
    private AROcclusionManager _occlusionManager;

    private void Awake()
    {
        _occlusionManager = GetComponent<AROcclusionManager>();
    }

    private void OnEnable()
    {
        spectatorViewSetup.Input.SpectatorView.ToggleOcclusion.performed += OnOcclusionToggle;
    }

    private void OnDisable()
    {
        spectatorViewSetup.Input.SpectatorView.ToggleOcclusion.performed -= OnOcclusionToggle;
    }

    public void OnOcclusionToggle(InputAction.CallbackContext _)
    {
        var newOcclusionPreference =
            _occlusionManager.currentOcclusionPreferenceMode is OcclusionPreferenceMode.NoOcclusion
                ? OcclusionPreferenceMode.PreferEnvironmentOcclusion
                : OcclusionPreferenceMode.NoOcclusion;
#if DEBUG_AR_OCCLUSION_TOGGLER
        Debug.Log(
            $"Toggling AR occlusion from {_occlusionManager.currentOcclusionPreferenceMode} to {newOcclusionPreference}"
        );
#endif
        _occlusionManager.requestedOcclusionPreferenceMode = newOcclusionPreference;
    }
}
