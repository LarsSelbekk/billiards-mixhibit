using System.Collections;
using Components;
using Unity.Netcode;
using UnityEngine;

public class ClientConnectedEventEmitter : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        StartCoroutine(WaitForIsConnected());
    }

    private IEnumerator WaitForIsConnected()
    {
        while (!NetworkManager.IsConnectedClient)
        { 
            Debug.Log($"[SVANESJO] ⏳ client {OwnerClientId} connecting ...");
            yield return null;
        }
        Debug.Log($"[SVANESJO] ✅ client {OwnerClientId} connected");
        GameManager.ClientConnected(OwnerClientId);
    }
}
