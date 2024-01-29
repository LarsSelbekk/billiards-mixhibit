using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Utils;
using Application = UnityEngine.Device.Application;

#if UNITY_EDITOR
using ParrelSync;
#endif

namespace Networking
{
    public enum StartType
    {
        Server,
        Host,
        Client
    }

    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public class PlayerLauncher : MonoBehaviour
    {
        private string _hostIP;

        public StartType editorStartType = StartType.Server;
        public StartType nonEditorStartType = StartType.Client;

        private NetworkManager _networkManager;

        private void Start()
        {
            _networkManager = GetComponent<NetworkManager>();
            if (_networkManager == null)
            {
                throw new MissingComponentException("Missing NetworkManager component");
            }
            var unityTransport = GetComponent<UnityTransport>();
            if (unityTransport == null)
            {
                throw new MissingComponentException("Missing UnityTransport component");
            }

            _hostIP = unityTransport.ConnectionData.Address;

            LaunchPlayer();
        }

        private void LaunchPlayer()
        {
#if UNITY_EDITOR
            if (ClonesManager.IsClone() &&
                Enum.TryParse(ClonesManager.GetArgument(), true, out StartType argumentStartType))
            {
                LaunchPlayerAs(argumentStartType);
                return;
            }
#endif
            if (_hostIP != null && NetworkingUtils.GetLocalIPAddress() == _hostIP)
            {
                LaunchPlayerAs(StartType.Host);
                return;
            }

            LaunchPlayerAs(Application.isEditor ? editorStartType : nonEditorStartType);
        }

        private void LaunchPlayerAs(StartType startType)
        {
            Debug.Log($"Launching player as '{startType}'");
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            var success = startType switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                StartType.Host => _networkManager.StartHost(),
                StartType.Server => _networkManager.StartServer(),
                StartType.Client => _networkManager.StartClient(),
            };

            if (success)
            {
                Debug.Log($"Player launched successfully as {startType}");
            }
            else
            {
                Debug.LogError($"Failed to launch player as {startType}");
            }
        }
    }
}