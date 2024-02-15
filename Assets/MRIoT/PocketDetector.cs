#nullable enable

using System;
using Unity.Netcode;
using UnityEngine;

namespace MRIoT
{
    [RequireComponent(typeof(MeshRenderer))]
    public class PocketDetector : MonoBehaviour
    {
        [SerializeField] private PocketEnum pocketLocation;
        [SerializeField] private Material defaultMaterial = null!;
        [SerializeField] private bool debugging; // = false;

        private MeshRenderer _meshRenderer = null!;
        private IotNetworkProxy _iotNetworkProxy = null!;

        private void Awake()
        {
            if (!debugging) return;

            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                throw new MissingComponentException("MeshRenderer missing");
            }

            if (defaultMaterial == null)
            {
                throw new ArgumentException("defaultMaterial is null");
            }

            _meshRenderer.enabled = debugging;
        }

        private void Start()
        {
            _iotNetworkProxy = FindFirstObjectByType<IotNetworkProxy>();
            if (_iotNetworkProxy == null)
            {
                throw new MissingComponentException($"{nameof(IotNetworkProxy)} not found");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var ball = other.gameObject;
            var substring = ball.name.Substring("Ball".Length, 2);
            var index = ball.name.Contains("BallCue") ? 0 : int.Parse(substring);

            if (ball.GetComponent<NetworkObject>()?.IsOwner == false)
            {
                Debug.Log($"PocketDetector OnTriggerEnter [ball: {index}, pocket: {pocketLocation}] aborted, not owner");
                return;
            }
            Debug.Log($"PocketDetector OnTriggerEnter [ball: {index}, pocket: {pocketLocation}, calling Scored");

            _iotNetworkProxy.Scored((BallEnum)index, pocketLocation);

            if (debugging)
                _meshRenderer.material = ball.GetComponent<MeshRenderer>().material;
        }

        public PocketEnum GetPocketLocation()
        {
            return pocketLocation;
        }

        public void SetColor(Color color)
        {
            Debug.Log($"PocketDetector SetColor called with color {color}");

            if (!debugging) return;
            _meshRenderer.material = defaultMaterial;
            _meshRenderer.material.color = color;
        }
    }
}
