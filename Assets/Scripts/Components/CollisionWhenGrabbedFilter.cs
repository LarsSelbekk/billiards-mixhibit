// #define DEBUG_COLLISION_WHEN_GRABBED_FILTER

using System;
using Networking;
using Unity.XR.CoreUtils;
using UnityEngine;

[RequireComponent(typeof(ObjectStateSync))]
public class CollisionWhenGrabbedFilter : MonoBehaviour
{
    public string partialClipLayerName;

    private ObjectStateSync _objectStateSync;
    private int _originalLayer;

    private void Awake()
    {
        _objectStateSync = GetComponent<ObjectStateSync>();
        _originalLayer = gameObject.layer;
    }

    private void OnEnable()
    {
        _objectStateSync.IsGrabbedOnNetworkChanged += OnIsGrabbedChanged;
        OnIsGrabbedChanged(_objectStateSync.IsGrabbedOnNetwork);
    }

    private void OnDisable()
    {
        _objectStateSync.IsGrabbedOnNetworkChanged -= OnIsGrabbedChanged;
        OnIsGrabbedChanged(_objectStateSync.IsGrabbedOnNetwork);
    }

    private void OnIsGrabbedChanged(bool isGrabbed)
    {
#if DEBUG_COLLISION_WHEN_GRABBED_FILTER
        Debug.Log($"[SVANESJO] [NoClipper] Is grabbed changed: {isGrabbed}");
#endif

        if (isGrabbed)
        {
            var partialClipLayer = LayerMask.NameToLayer(partialClipLayerName);
            if (partialClipLayer == -1)
                throw new ArgumentOutOfRangeException($"The layer {partialClipLayerName} does not exist!");
            gameObject.SetLayerRecursively(partialClipLayer);
#if DEBUG_COLLISION_WHEN_GRABBED_FILTER
            Debug.Log($"[SVANESJO] [NoClipper] Changed to layer {partialClipLayerName} ({partialClipLayer})");
#endif
        }
        else
        {
            gameObject.SetLayerRecursively(_originalLayer);
#if DEBUG_COLLISION_WHEN_GRABBED_FILTER
            Debug.Log(
                $"[SVANESJO] [NoClipper] Reverted to layer {LayerMask.LayerToName(_originalLayer)} ({_originalLayer})"
            );
#endif
        }
    }
}
