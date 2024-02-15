using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Networking.Player
{
    public enum PlayerType
    {
        Server,
        Host,
        Client
    }

    public class PlayerPainter : NetworkBehaviour
    {
        public Material serverMaterial;
        public Material hostMaterial;
        public Material clientMaterial;

        private readonly NetworkVariable<PlayerType> _playerType = new(PlayerType.Client);

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                // either player type is ready and we paint,
                // or we paint default now and repaint when player type is ready
                PaintPlayer();
                return;
            }

            RegisterPlayerTypeServerRpc(IsHost ? PlayerType.Host : IsServer ? PlayerType.Server : PlayerType.Client);
        }

        [ServerRpc(RequireOwnership = false)]
        void RegisterPlayerTypeServerRpc(PlayerType playerType)
        {
            _playerType.Value = playerType;
            PaintPlayerClientRpc();
        }

        [ClientRpc]
        void PaintPlayerClientRpc()
        {
            PaintPlayer();
        }

        private void PaintPlayer()
        {
            var materials = new List<Material>
            {
                _playerType.Value switch
                {
                    PlayerType.Host => hostMaterial,
                    PlayerType.Server => serverMaterial,
                    _ => clientMaterial
                }
            };
            foreach (var mr in GetComponentsInChildren<MeshRenderer>())
            {
                mr.SetMaterials(materials);
            }
        }
    }
}