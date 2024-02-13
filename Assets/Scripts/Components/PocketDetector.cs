#nullable enable

using MRIoT;
using UnityEngine;

public class PocketDetector : MonoBehaviour
{
    [SerializeField] private PocketEnum pocketEnum;

    private void OnTriggerEnter(Collider other)
    {
        var ball = other.gameObject;
        var substring = ball.name.Substring("Ball".Length, 2); 
        var index = ball.name.Contains("BallCue") ? 0 : int.Parse(substring);

        var iotController = FindFirstObjectByType<IOTController>();
        iotController.Scored((BallEnum)index, pocketEnum);
    }
}
