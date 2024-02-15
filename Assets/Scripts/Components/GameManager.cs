using System;
using Unity.Netcode;
using UnityEngine;

namespace Components
{
    public class GameManager : MonoBehaviour
    {

        private static GameManager Instance { get; set; }

        public static event Action OnReset;

        public static event Action<ulong> OnClientConnected;
    
        private void Awake()
        {
            // If there is an instance, and it's not me, delete myself.

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SVANESJO] attempted to create another GameManager instance");
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public void Reset()
        {
            OnReset?.Invoke();
        }

        public static void ClientConnected(ulong clientId)
        {
            OnClientConnected?.Invoke(clientId);
        }
    
    }
}