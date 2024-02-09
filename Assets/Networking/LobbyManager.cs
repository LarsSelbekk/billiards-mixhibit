using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Utils;

namespace Networking
{
    public class LobbyManager : MonoBehaviour
    {
        private const string LastServerUrlPrefKey = "lastServerUrl";

        public PlayerLauncher playerLauncher;
        public TMP_InputField serverUrlInputField;
        public TMP_Text myIpTextField;
        public List<XRRayInteractor> rayInteractors;

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

        private void Start()
        {
            serverUrlInputField.text = PlayerPrefs.GetString(LastServerUrlPrefKey);
            myIpTextField.text = $"My IP: {NetworkingUtils.GetLocalIPAddress()}";
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
            playerLauncher.SetServerUrl(serverUrl.Trim());
            if (!playerLauncher.LaunchPlayerAs(StartType.Client)) return;

            PlayerPrefs.SetString(LastServerUrlPrefKey, serverUrl.Trim());
            IsUIShown = false;
        }

        public void LaunchHost()
        {
            if (playerLauncher.LaunchPlayerAs(StartType.Host))
            {
                IsUIShown = false;
            }
        }
    }
}
