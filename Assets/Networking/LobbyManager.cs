#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using MRIoT;
using NaughtyAttributes;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Utils;
using Application = UnityEngine.Device.Application;

#if UNITY_EDITOR
using ParrelSync;
#endif

namespace Networking
{
    public class LobbyManager : MonoBehaviour
    {
        [Serializable]
        public class AutoStartArgs
        {
            public bool autoStart;
            public string? hostAddress;
            public StartType deviceStartType;
            public StartType editorStartType;
        }

        public enum LobbyState
        {
            Lobby,
            Client,
            Server
        }

        public LobbyState CurrentLobbyState
        {
            get => _currentLobbyState;
            private set
            {
                _currentLobbyState = value;
                IsUIShown = _currentLobbyState is LobbyState.Lobby;
                if (value is LobbyState.Server)
                {
                    ListenForClients();
                }
                else
                {
                    StopListeningForClients();
                }
            }
        }

        private const string LastServerUrlPrefKey = "lastServerUrl";

        [SerializeField, Required] private PlayerLauncher playerLauncher = null!;
        [SerializeField, Required] private TMP_InputField serverUrlInputField = null!;
        [SerializeField, Required] private TMP_Text myIpTextField = null!;
        [SerializeField] private List<XRRayInteractor> rayInteractors = new();
        [SerializeField, Required] private IotNetworkProxy iotNetworkProxy = null!;
        [SerializeField, Required] private Toggle iotToggle = null!;
        [SerializeField] private AutoStartArgs autoStartArgs = null!;
        [SerializeField] private LocalLobbyFinder? localLobbyFinder;
        [SerializeField] private ServerList? serverList;

        private string _myIpAddress = null!;
        private CancellationTokenSource? _serverFinderCancellationTokenSource, _clientListenerCancellationTokenSource;
        private LobbyState _currentLobbyState = LobbyState.Lobby;
        private bool _isUIShown = true;

        private bool IsUIShown
        {
            get => _isUIShown;
            set
            {
                _isUIShown = value;
                var children = new List<GameObject>();
                gameObject.GetChildGameObjects(children);

                foreach (var child in children)
                {
                    child.SetActive(value);
                }

                foreach (var rayInteractor in rayInteractors)
                {
                    rayInteractor.gameObject.SetActive(value);
                }

                if (value)
                {
                    ListenForServers();
                }
                else
                {
                    StopListeningForServers();
                }
            }
        }

        private void Awake()
        {
            if (playerLauncher == null)
                throw new ArgumentNullException(nameof(playerLauncher));
            if (serverUrlInputField == null)
                throw new ArgumentNullException(nameof(serverUrlInputField));
            if (myIpTextField == null)
                throw new ArgumentNullException(nameof(myIpTextField));
            if (iotToggle == null)
                throw new ArgumentNullException(nameof(iotToggle));
            if (iotNetworkProxy == null)
                throw new ArgumentNullException(nameof(iotNetworkProxy));
            if (autoStartArgs == null)
                throw new ArgumentNullException(nameof(autoStartArgs));
        }

        private void Start()
        {
            serverUrlInputField.text = PlayerPrefs.GetString(LastServerUrlPrefKey);
            _myIpAddress = NetworkingUtils.GetLocalIPAddress();
            myIpTextField.text = $"My IP:\n{_myIpAddress}";

            playerLauncher.OnConnect += startType => CurrentLobbyState = startType switch
            {
                StartType.Client => LobbyState.Client,
                StartType.Host or StartType.Server => LobbyState.Server,
                _ => throw new ArgumentOutOfRangeException(nameof(startType), startType, null)
            };
            playerLauncher.OnDisconnect += () => CurrentLobbyState = LobbyState.Lobby;

            CurrentLobbyState = LobbyState.Lobby;

            if (autoStartArgs.autoStart)
            {
                AutoLaunch();
            }
        }


        private void OnDisable()
        {
            StopListeningForClients();
            StopListeningForServers();
        }

        private void OnDestroy()
        {
            PlayerPrefs.Save();
        }

        public void LaunchClient()
        {
            LaunchClient(serverUrlInputField.text);
        }

        public void LaunchClient(string serverUrl)
        {
            var hostAddress = serverUrl.Trim();
            if (!playerLauncher.LaunchPlayerAs(StartType.Client, hostAddress)) return;

            PlayerPrefs.SetString(LastServerUrlPrefKey, serverUrl.Trim());
        }

        public void LaunchHost()
        {
            iotNetworkProxy.SetEnableIot(iotToggle.isOn);

            playerLauncher.LaunchPlayerAs(StartType.Host, _myIpAddress);
        }

        private void AutoLaunch()
        {
#if UNITY_EDITOR
            if (ClonesManager.IsClone()
                && Enum.TryParse(ClonesManager.GetArgument(), true, out StartType argumentStartType))
            {
                playerLauncher.LaunchPlayerAs(argumentStartType, autoStartArgs.hostAddress ?? _myIpAddress);
                return;
            }
#endif
            if (autoStartArgs.hostAddress != null && _myIpAddress == autoStartArgs.hostAddress)
            {
                playerLauncher.LaunchPlayerAs(StartType.Host, autoStartArgs.hostAddress);
                return;
            }

            playerLauncher.LaunchPlayerAs(
                Application.isEditor ? autoStartArgs.editorStartType : autoStartArgs.deviceStartType,
                autoStartArgs.hostAddress ?? _myIpAddress
            );
        }

        private async void ListenForServers()
        {
            if (localLobbyFinder is null || _serverFinderCancellationTokenSource is not null) return;
            _serverFinderCancellationTokenSource = new CancellationTokenSource();
            if (serverList is null)
            {
                throw new NullReferenceException($"{nameof(serverList)} is null, unable to show local lobbies");
            }

            Action<IPAddress> callback = ip => LaunchClient(ip.ToString());
            serverList.OnClickServer += callback;

            await foreach (
                var servers in
                localLobbyFinder.Client.FindServers(_serverFinderCancellationTokenSource.Token)
            )
            {
                serverList.Servers = servers;
            }

            serverList.OnClickServer -= callback;
        }

        private void ListenForClients()
        {
            if (localLobbyFinder is null || _clientListenerCancellationTokenSource is not null) return;
            _clientListenerCancellationTokenSource = new CancellationTokenSource();
            localLobbyFinder.Server.StartRespondingToClientPings(_clientListenerCancellationTokenSource.Token);
        }

        private void StopListeningForServers()
        {
            if (_serverFinderCancellationTokenSource is null) return;
            _serverFinderCancellationTokenSource.Cancel();
            _serverFinderCancellationTokenSource.Dispose();
            _serverFinderCancellationTokenSource = null;
        }

        private void StopListeningForClients()
        {
            if (_clientListenerCancellationTokenSource is null) return;
            _clientListenerCancellationTokenSource.Cancel();
            _clientListenerCancellationTokenSource.Dispose();
            _clientListenerCancellationTokenSource = null;
        }
    }
}
