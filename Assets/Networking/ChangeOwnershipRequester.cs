using Unity.Netcode;
using UnityEngine;

public class ChangeOwnershipRequester : NetworkBehaviour
{

    // public NetworkObject[] ownables;

    public void RequestOwnership()
    {
        RequestOwnershipServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestOwnershipServerRpc(ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("Server received RequestOwnership");
        var clientId = serverRpcParams.Receive.SenderClientId;
        foreach (var networkObject in GameObject.FindGameObjectWithTag("TableHolder").GetComponentsInChildren<NetworkObject>())
        {
            networkObject.ChangeOwnership(clientId);
        }
        // foreach (var o in ownables)
        // {
        //     o.ChangeOwnership(clientId);
        // }
        // GetComponent<NetworkObject>().ChangeOwnership(serverRpcParams.Receive.SenderClientId);
    }
}
