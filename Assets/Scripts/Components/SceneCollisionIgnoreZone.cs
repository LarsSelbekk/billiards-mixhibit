using UnityEngine;

public class SceneCollisionIgnoreZone : MonoBehaviour
{
    private Collider _sceneCollider;

    void Update()
    {
        if (!_sceneCollider)
        {
            _sceneCollider = GameObject.FindGameObjectWithTag("SceneAnchor")?.GetComponent<Collider>();
        }
    }

    private void SetIgnoreScene(Collider other, bool ignoreScene)
    {
        if (!_sceneCollider) return;
        Physics.IgnoreCollision(other, _sceneCollider, ignoreScene);
    }

    private void OnTriggerEnter(Collider other)
    {
        SetIgnoreScene(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        SetIgnoreScene(other, true);
    }

    private void OnTriggerExit(Collider other)
    {
        SetIgnoreScene(other, false);
    }
}