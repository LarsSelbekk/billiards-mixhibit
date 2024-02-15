#nullable enable

using System;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;

namespace MRIoT
{
    [RequireComponent(typeof(NetworkObject))]
    public class IotNetworkProxy : NetworkBehaviour
    {
        [SerializeField, Required] private IotController iotPrefab = null!;

        private IotController? _iotController;

        private void Awake()
        {
            if (iotPrefab == null)
            {
                throw new ArgumentNullException(nameof(iotPrefab));
            }
        }

        private void Start()
        {
            Debug.Log($"{nameof(IotNetworkProxy)} Start");
            Initialize();
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"{nameof(IotNetworkProxy)} OnNetworkSpawn");
            Initialize();
            base.OnNetworkSpawn();
        }

        private void Initialize()
        {
            if (!IsServer) return;

            if (_iotController != null)
            {
                Debug.Log($"{nameof(IotNetworkProxy)} Initialize {nameof(_iotController)} already initialize, aborting");
                return;
            }

            Debug.Log($"{nameof(IotNetworkProxy)} Initialize instantiating {nameof(_iotController)}");
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
