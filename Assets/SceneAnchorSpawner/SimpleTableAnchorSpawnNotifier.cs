using System;
using UnityEngine;

public class SimpleTableAnchorSpawnNotifier : MonoBehaviour
{
    public static event Action onSpawn;

    private void Start()
    {
        onSpawn?.Invoke();
    }
}