using System.Collections.Generic;
using System.Linq;
using Attributes;
using Unity.Netcode;
using UnityEngine;

namespace Components
{
    public class SceneBuilder : NetworkBehaviour
    {
        
        // Holds everything that should be physically collocated between clients
        public GameObject worldLockParent;
        private GameObject _worldLockParentInstance;

        public GameObject resetButton;

        public TableHolder tableHolder;

        public GameObject tableReference;

        // These are the prefabs identified in `tableReference`, which will be used to build the actual table
        // TODO: try to use network prefab list override instead
        [ReadOnlyInInspector] public GameObject[] tableParts;

        private void Awake()
        {
            GameManager.OnReset += Reset;
            GameManager.OnClientConnected += _OnClientConnected;
        }

        private void _OnClientConnected(ulong clientId)
        {
            if (!IsServer || clientId == NetworkManager.ServerClientId)
            {
                // ignore if not server, or if clientId is server (already handled in OnNetworkSpawn)
                return;
            }
            SetPlayerParent(clientId, _worldLockParentInstance);
        }
        
        private void SetPlayerParent(ulong clientId, GameObject parent)
        {
            if (!IsSpawned || !IsServer) return;
            if (!NetworkManager.ConnectedClients.ContainsKey(clientId))
            {
                Debug.LogError($"[SVANESJO] could not find clientId {clientId} in NetworkManager.ConnectedClients");
                return;
            }
            NetworkManager.ConnectedClients[clientId].PlayerObject.TrySetParent(parent, false);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            _worldLockParentInstance = Instantiate(worldLockParent);
            _worldLockParentInstance.GetComponent<NetworkObject>().Spawn();
            SetPlayerParent(NetworkManager.LocalClientId, _worldLockParentInstance);
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
                // since a NetworkObject is not destroyed with parent, we need to destroy them manually
                for (var i = 0; i < holder.transform.childCount; i++)
                {
                    foreach (var n in holder.transform.GetChild(i).GetComponentsInChildren<NetworkObject>())
                    {
                        Destroy(n.gameObject);
                    }
                }

                foreach (var heldObject in holder.GetComponent<TableHolder>().heldObjects)
                {
                    if (heldObject == null)
                    {
                        // already destroyed
                        continue;
                    }

                    // most objects are already covered by the above Destroy, but we assume repeated calls won't hurt
                    // since actual destruction is delayed until the end of the current update loop
                    Destroy(heldObject);
                }

                Destroy(holder);
            }

            SpawnTable();
        }

        private void SpawnTable()
        {
            if (!IsServer) return;
            var holder = Instantiate(tableHolder);
            var holderNetworkObject = holder.GetComponent<NetworkObject>();
            holderNetworkObject.Spawn();
            holderNetworkObject.TrySetParent(_worldLockParentInstance.transform, false);
            var holderTransform = holder.transform;
            var heldObjects = new List<GameObject>();
            for (var i = 0; i < tableReference.transform.childCount; i++)
            {
                var referencePart = tableReference.transform.GetChild(i).gameObject;
                var partPrefab = tableParts.First(o => o.name == referencePart.name);
                var part = Instantiate(partPrefab, referencePart.transform.position, referencePart.transform.rotation);
                heldObjects.Add(part);
                part.transform.localScale = referencePart.transform.localScale;
                var partNetworkObject = part.GetComponent<NetworkObject>();
                if (partNetworkObject == null) continue;
                partNetworkObject.Spawn();
                partNetworkObject.TrySetParent(holderTransform, false);
            }

            holder.heldObjects = heldObjects.ToArray();
        }

        private void SpawnResetButton()
        {
            if (!IsServer) return;
            var resetButtonNetworkObject = Instantiate(resetButton).GetComponent<NetworkObject>();
            resetButtonNetworkObject.Spawn();
            resetButtonNetworkObject.TrySetParent(_worldLockParentInstance, false);
        }
    }
}