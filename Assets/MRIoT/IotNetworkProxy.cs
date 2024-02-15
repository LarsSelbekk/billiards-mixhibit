#nullable enable

using System;
using Components;
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
        private bool _enableIot; // = false;

        private void Awake()
        {
            if (iotPrefab == null)
            {
                throw new ArgumentNullException(nameof(iotPrefab));
            }
        }

        private void Start()
        {
            Debug.Log("IOTNetworkProxy Start");
            Initialize();
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log("IOTNetworkProxy OnNetworkSpawn");
            Initialize();
            base.OnNetworkSpawn();
        }

        private void Initialize()
        {
            if (!IsServer || !_enableIot || _iotController != null)
            {
                Debug.Log($"IOTNetworkProxy Initialize aborted, {nameof(IsServer)}: {IsServer}, {nameof(_enableIot)}: {_enableIot}, {nameof(_iotController)}: {_iotController}");
                return;
            }

            Debug.Log($"IOTNetworkProxy Initialize instantiating {nameof(_iotController)}");
            _iotController = Instantiate(iotPrefab);
            if (_iotController == null)
            {
                throw new ArgumentNullException(nameof(_iotController));
            }

            Debug.Log("IOTNetworkProxy Initialize registering GameManager.OnReset event handler");
            GameManager.OnReset += OnResetEventHandler;
        }

        public void Scored(BallEnum ballEnum, PocketEnum pocketEnum)
        {
            Debug.Log("IOTNetworkProxy Scored called");
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
            if (!_enableIot)
            {
                Debug.Log("IOTNetworkProxy ScoredInternal IOT disabled");
                return;
            }

            if (_iotController == null)
            {
                Debug.LogError("IOTNetworkProxy ScoredInternal called prior to initialization, or init failed...");
                return;
            }

            Debug.Log("IOTNetworkProxy ScoredInternal called");

            _iotController.Scored(ballEnum, pocketEnum);
        }

        public void SetEnableIot(bool value)
        {
            Debug.Log($"IOTNetworkProxy SetEnableIot {value}");
            _enableIot = value;
        }

        private void OnResetEventHandler()
        {
            if (IsServer)
                OnResetInternal();
            else
                OnResetServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnResetServerRpc()
        {
            OnResetInternal();
        }

        private void OnResetInternal()
        {
            if (!IsServer || !_enableIot || _iotController == null)
            {
                Debug.LogError($"IOTNetworkProxy OnResetInternal aborted, {nameof(IsServer)}: {IsServer}, {nameof(_enableIot)}: {_enableIot}, {nameof(_iotController)}: {_iotController}");
                return;
            }
            _iotController.ResetIot();
        }
    }
}
