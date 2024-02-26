#nullable enable

using System;
using Components;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Networking
{
    public enum StartType
    {
        Server,
        Host,
        Client,
    }

    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public class PlayerLauncher : MonoBehaviour
    {
        [SerializeField] private int recheckNotClient = 120;
        [SerializeField] private int delayedDisconnect = 60;

        public event Action<StartType>? OnConnect;
        public event Action? OnDisconnect;

        private NetworkManager _networkManager = null!;
        private UnityTransport _unityTransport = null!;

        private void Start()
        {
            _networkManager = GetComponent<NetworkManager>();
            if (_networkManager == null)
                throw new MissingComponentException("Missing NetworkManager component");

            _unityTransport = GetComponent<UnityTransport>();
            if (_unityTransport == null)
                throw new MissingComponentException("Missing UnityTransport component");

            StartCoroutine(CheckConnection());
        }

        private void ShutdownPlayer()
        {
            _networkManager.Shutdown();
            OnDisconnect?.Invoke();
        }

        public bool LaunchPlayerAs(StartType startType, string hostAddress)
        {
            Debug.Log($"PlayerLauncher LaunchPlayerAs({startType}, {hostAddress})");
            ShutdownPlayer();
            _unityTransport.ConnectionData.Address = hostAddress;

            var success = startType switch
            {
                StartType.Host => _networkManager.StartHost(),
                StartType.Server => _networkManager.StartServer(),
                StartType.Client => _networkManager.StartClient(),
                _ => throw new IndexOutOfRangeException(nameof(startType)),
            };

            if (success)
            {
                OnConnect?.Invoke(startType);
                Debug.Log($"Player launched successfully as {startType}");

                NetworkManager.Singleton.OnClientConnectedCallback += GameManager.ClientConnected;
            }
            else
            {
                OnDisconnect?.Invoke();
                Debug.LogError($"Failed to launch player as {startType}");
            }

            return success;
        }

        private IEnumerator CheckConnection()
        {
            Debug.Log("PlayerLauncher CheckConnection coroutine started");
            while (true)
            {
                if (!_networkManager.IsClient)
                {
                    Debug.Log($"PlayerLauncher CheckConnection not a client... rechecking in {recheckNotClient} seconds");
                    yield return new WaitForSeconds(recheckNotClient);
                    continue;
                }

                var connected = _networkManager.IsConnectedClient;
                if (!connected)
                {
                    Debug.Log($"PlayerLauncher CheckConnection not connected, rechecking in {delayedDisconnect} seconds");
                    yield return new WaitForSeconds(delayedDisconnect);
                    connected = _networkManager.IsConnectedClient;
                    if (!connected)
                    {
                        Debug.Log("PlayerLauncher CheckConnection still not connected, returning to lobby");
                        ShutdownPlayer();
                        continue;
                    }
                }
                else
                {
                    Debug.Log("PlayerLauncher CheckConnection connected, rechecking in 10 seconds");
                }

                yield return new WaitForSeconds(10);
            }
        }
    }
}
