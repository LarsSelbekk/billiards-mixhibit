using System.Data;
using Components;
using UnityEngine;

namespace ResetButton
{
    public class ResetButton : MonoBehaviour
    {
        public void Reset()
        {
            var gameManager = GameObject.FindGameObjectWithTag("GameManager")?.GetComponent<GameManager>();
            if (gameManager == null) throw new NoNullAllowedException("GameManager not found");
            gameManager.Reset();
        }
    }
}