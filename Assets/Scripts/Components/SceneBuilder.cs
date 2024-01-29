using System.Linq;
using Attributes;
using Unity.Netcode;
using UnityEngine;

namespace Components
{
    public class SceneBuilder : NetworkBehaviour
    {
        public GameObject tableReference;
        
        // These are the prefabs identified in `tableReference`, which will be used to build the actual table
        [ReadOnlyInInspector] public GameObject[] tableParts;
        
        public GameObject tableHolder;

        public GameObject resetButton;

        private static SceneBuilder Instance { get; set; }

        private void Awake()
        {
            // If there is an instance, and it's not me, delete myself.

            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            GameManager.OnReset += Reset;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            SpawnTable();
            SpawnResetButton();
        }

        public void Reset()
        {
            if (!IsServer)
            {
                ResetServerRpc();
                return;
            }

            DoReset();
        }

        private void DoReset()
        {
            ResetTable();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ResetServerRpc()
        {
            DoReset();
        }

        private void ResetTable()
        {
            foreach (var holder in GameObject.FindGameObjectsWithTag("TableHolder"))
            {
                for (var i = 0; i < holder.transform.childCount; i++)
                {
                    foreach (var n in holder.transform.GetChild(i).GetComponentsInChildren<NetworkObject>())
                    {
                        Destroy(n.gameObject);
                    }
                }

                Destroy(holder);
            }

            SpawnTable();
        }

        private void SpawnTable()
        {
            if (!IsServer) return;

            var holder = Instantiate(tableHolder);
            holder.GetComponent<NetworkObject>().Spawn();
            for (var i = 0; i < tableReference.transform.childCount; i++)
            {
                var referencePart = tableReference.transform.GetChild(i).gameObject;
                var partPrefab = tableParts.First(o => o.name == referencePart.name);
                var part = Instantiate(partPrefab, referencePart.transform.position, referencePart.transform.rotation);
                part.transform.localScale = referencePart.transform.localScale;
                var partNetworkObject = part.GetComponent<NetworkObject>();
                if (partNetworkObject == null) continue;
                partNetworkObject.Spawn();
                partNetworkObject.TrySetParent(holder, false);
            }
        }

        private void SpawnResetButton()
        {
            if (!IsServer) return;
            Instantiate(resetButton).GetComponent<NetworkObject>().Spawn();
        }
    }
}