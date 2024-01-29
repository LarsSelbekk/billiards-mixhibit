using System.Data;
using UnityEngine;

public class ResetButton : MonoBehaviour
{
    public void Reset()
    {
        var gameManager = GameObject.FindGameObjectWithTag("GameManager")?.GetComponent<GameManager>();
        if (gameManager == null) throw new NoNullAllowedException("GameManager not found");
        gameManager.Reset();
    }
}