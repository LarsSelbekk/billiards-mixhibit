#nullable enable

using System;
using System.Collections.Generic;
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

        private const string LastServerUrlPrefKey = "lastServerUrl";

        [SerializeField, Required] private PlayerLauncher playerLauncher = null!;
        [SerializeField, Required] private TMP_InputField serverUrlInputField = null!;
        [SerializeField, Required] private TMP_Text myIpTextField = null!;
        [SerializeField] private List<XRRayInteractor> rayInteractors = new();
        [SerializeField, Required] private IotNetworkProxy iotNetworkProxy = null!;
        [SerializeField, Required] private Toggle iotToggle = null!;
        [SerializeField] private AutoStartArgs autoStartArgs = null!;

        private string _myIpAddress = null!;
        private bool _isUIShown;

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
            myIpTextField.text = $"My IP: {_myIpAddress}";

            playerLauncher.OnConnect += HideUI;
            playerLauncher.OnDisconnect += ShowUI;

            if (autoStartArgs.autoStart)
            {
                AutoLaunch();
                HideUI();
            }
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
                autoStartArgs.hostAddress ?? _myIpAddress);
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
            HideUI();
        }

        public void LaunchHost()
        {
            iotNetworkProxy.SetEnableIot(iotToggle.isOn);
            if (playerLauncher.LaunchPlayerAs(StartType.Host, _myIpAddress))
            {
                HideUI();
            }
        }

        private void ShowUI()
        {
            IsUIShown = true;
        }

        private void HideUI()
        {
            IsUIShown = false;
        }
    }
}
