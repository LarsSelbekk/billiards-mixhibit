using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HostOnlyEnforcer : NetworkBehaviour
{

    public List<MonoBehaviour> subjects;

    private void Awake()
    {
        foreach (var s in subjects)
        {
            s.enabled = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!NetworkManager.IsHost) return;
        foreach (var s in subjects)
        {
            s.enabled = true;
        }
    }
}
