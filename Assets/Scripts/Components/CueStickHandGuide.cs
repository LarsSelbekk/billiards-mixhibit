// #define DEBUG_CUE_STICK_HAND_GUIDE

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class CueStickHandGuide : MonoBehaviour
{
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    public float emissionIntensity = 5f;
    public List<Behaviour> componentsToEnable = new();

    private Renderer _renderer;
    private Color _restoreColor;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void OnHover()
    {
        foreach (var component in componentsToEnable)
        {
            component.enabled = true;
        }

        var color = _renderer.material.GetColor(EmissionColor);
        _restoreColor = color;
        color *= Mathf.Pow(2, emissionIntensity);

#if DEBUG_CUE_STICK_HAND_GUIDE
        Debug.Log("[HandGuideTest] Hover!");
        Debug.Log($"[HandGuideTest] hover color from {_renderer.material.GetColor(EmissionColor)} to {color}");
#endif
        _renderer.material.SetColor(EmissionColor, color);
    }

    public void OnHoverEnd()
    {
        foreach (var component in componentsToEnable)
        {
            component.enabled = false;
        }

#if DEBUG_CUE_STICK_HAND_GUIDE
        Debug.Log("[HandGuideTest] Hover end!");
        Debug.Log(
            $"[HandGuideTest] hoverend color from {_renderer.material.GetColor(EmissionColor)} to {_restoreColor}"
        );
#endif
        _renderer.material.SetColor(EmissionColor, _restoreColor);
    }
}
