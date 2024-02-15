using Unity.Netcode;
using UnityEngine;

namespace Networking.Player
{
    public class PlayerCameraFollow : NetworkBehaviour
    {
        private GameObject _camera;

        private void Update()
        {
            if (!_camera)
            {
                _camera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            if (!_camera) return;
            var cameraPosition = _camera.transform.position;
            transform.position = new Vector3(
                cameraPosition.x,
                cameraPosition.y + 0.5f,
                cameraPosition.z
            );
            var eulerAngles = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(new Vector3(
                eulerAngles.x,
                _camera.transform.eulerAngles.y,
                eulerAngles.z
            ));
        }
    }
}