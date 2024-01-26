using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    private static GameManager Instance { get; set; }

    public static event Action OnReset;

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void Reset()
    {
        OnReset?.Invoke();
    }
    
}