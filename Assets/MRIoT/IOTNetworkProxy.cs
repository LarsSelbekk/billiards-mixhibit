#nullable enable

using System;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;

namespace MRIoT
{
    [RequireComponent(typeof(NetworkObject))]
    public class IOTNetworkProxy : NetworkBehaviour
    {
        [SerializeField, Required] private IOTController iotPrefab = null!;

        private IOTController? _iotController;

        private void Awake()
        {
            if (iotPrefab == null)
            {
                throw new ArgumentNullException(nameof(iotPrefab));
            }
        }

        private void Start()
        {
            Debug.Log($"{nameof(IOTNetworkProxy)} Start");
            Initialize();
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"{nameof(IOTNetworkProxy)} OnNetworkSpawn");
            Initialize();
            base.OnNetworkSpawn();
        }

        private void Initialize()
        {
            if (!IsServer) return;

            if (_iotController != null)
            {
                Debug.Log($"{nameof(IOTNetworkProxy)} Initialize {nameof(_iotController)} already initialize, aborting");
                return;
            }

            Debug.Log($"{nameof(IOTNetworkProxy)} Initialize instantiating {nameof(_iotController)}");
            _iotController = Instantiate(iotPrefab);
            if (_iotController == null)
            {
                throw new ArgumentNullException(nameof(_iotController));
            }
        }

        public void Scored(BallEnum ballEnum, PocketEnum pocketEnum)
        {
            if (IsServer)
                ScoredInternal(ballEnum, pocketEnum);
            else
                ScoredServerRpc(ballEnum, pocketEnum);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ScoredServerRpc(BallEnum ballEnum, PocketEnum pocketEnum)
        {
            ScoredInternal(ballEnum, pocketEnum);
        }

        private void ScoredInternal(BallEnum ballEnum, PocketEnum pocketEnum)
        {
            if (_iotController == null)
            {
                Debug.LogError("IOTNetworkProxy ScoredInternal called prior to initialization, or init failed...");
                return;
            }

            _iotController.Scored(ballEnum, pocketEnum);
        }
    }
}
