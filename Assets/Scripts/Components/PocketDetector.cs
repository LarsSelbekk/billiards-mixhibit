#nullable enable

using System;
using MRIoT;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class PocketDetector : MonoBehaviour
{
    [SerializeField] private PocketEnum pocketLocation;
    [SerializeField] private Material defaultMaterial = null!;

    private MeshRenderer _meshRenderer = null!;
    private IOTNetworkProxy _iotNetworkProxy = null!;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
        {
            throw new MissingComponentException("MeshRenderer missing");
        }

        if (defaultMaterial == null)
        {
            throw new ArgumentException("defaultMaterial is null");
        }
    }

    private void Start()
    {
        _iotNetworkProxy = FindFirstObjectByType<IOTNetworkProxy>();
        if (_iotNetworkProxy == null)
        {
            throw new MissingComponentException($"{nameof(IOTNetworkProxy)} not found");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var ball = other.gameObject;
        var substring = ball.name.Substring("Ball".Length, 2);
        var index = ball.name.Contains("BallCue") ? 0 : int.Parse(substring);

        _iotNetworkProxy.Scored((BallEnum)index, pocketLocation);

        _meshRenderer.material = ball.GetComponent<MeshRenderer>().material;
    }

    public PocketEnum GetPocketLocation()
    {
        return pocketLocation;
    }

    public void SetColor(Color color)
    {
        Debug.Log($"PocketDetector SetColor called with color {color}");
        _meshRenderer.material = defaultMaterial;
        _meshRenderer.material.color = color;
    }
}
