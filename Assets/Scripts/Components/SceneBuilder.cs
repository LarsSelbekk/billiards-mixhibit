using System;
using System.Collections.Generic;
using System.Linq;
using Attributes;
using Unity.Netcode;
using UnityEngine;
using Utils;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace Components
{
    internal enum WallFixture
    {
        Art,
        ResetButton
    }

    internal class Rectangle
    {
        public float CenterX { get; }
        public float CenterY { get; }
        public float Width { get; }
        public float Height { get; }

        public Rectangle(float centerX, float centerY, float width, float height)
        {
            CenterX = centerX;
            CenterY = centerY;
            Width = width;
            Height = height;
        }

        public bool Overlaps(Rectangle other, float requiredMargin = 0.0f)
        {
            var x1Min = CenterX - Width / 2;
            var x1Max = CenterX + Width / 2;
            var y1Min = CenterY - Height / 2;
            var y1Max = CenterY + Height / 2;

            var x2Min = other.CenterX - other.Width / 2;
            var x2Max = other.CenterX + other.Width / 2;
            var y2Min = other.CenterY - other.Height / 2;
            var y2Max = other.CenterY + other.Height / 2;

            return !(x1Max + requiredMargin < x2Min || x2Max + requiredMargin < x1Min ||
                     y1Max + requiredMargin < y2Min || y2Max + requiredMargin < y1Min);
        }

        public override string ToString()
        {
            return $"{GetType()}(X={CenterX},Y={CenterY},Width={Width},Height={Height})";
        }
    }

    public class SceneBuilder : NetworkBehaviour
    {
        [Header("World Lock")]
        // holds everything that should be physically collocated between clients
        public GameObject worldLockParent;

        private GameObject _worldLockParentInstance;

        [Header("Wall Art")] public float wallArtPercentGap = 10;
        public float wallArtPercentWallPadding = 20;
        public float wallArtOutFromWallOffset = 0.05f;
        public float minWallArtScale = 0.5f;
        public float maxWallArtScale = 1.0f;
        public int maxWallArtPlacementAttempts = 100;
        public GameObject[] wallArtPieces;
        private readonly List<GameObject> _wallArtPieceInstances = new();

        private readonly Dictionary<int, List<(WallFixture type, Rectangle rectangle, GameObject gameObject)>>
            _placedWallFixtures = new();

        private bool _wallArtPlaced;
        private bool WallArtSpawned => _wallArtPieceInstances.Count == wallArtPieces.Length;

        [Header("Reset Button")] public float resetButtonYPosition = 1.5f;
        public float resetButtonOutFromWallOffset = 0.1f;
        public GameObject resetButton;
        private GameObject _resetButtonInstance;
        private bool _resetButtonPlaced;
        private bool ResetButtonSpawned => _resetButtonInstance != null;


        [Header("Pool Table")] public TableHolder tableHolder;

        public GameObject tableReference;

        // these are the prefabs identified in `tableReference`, which will be used to build the actual table
        [ReadOnlyInInspector] public GameObject[] tableParts;

        private OVRScenePlane[] _wallAnchorPlanes;
        private bool WallAnchorsReady => _wallAnchorPlanes is { Length: > 0 };

        private void Awake()
        {
            GameManager.OnReset += Reset;
            GameManager.OnClientConnected += _OnClientConnected;
        }

        private void Update()
        {
            if (!WallAnchorsReady)
            {
                _wallAnchorPlanes = GameObject.FindGameObjectsWithTag("WallAnchor")
                    .Select(a => a.GetComponent<OVRScenePlane>())
                    .Where(p => p != null).ToArray();
                return;
            }

            if (!_resetButtonPlaced && ResetButtonSpawned)
            {
                PlaceResetButton();
            }

            if (!_wallArtPlaced && WallArtSpawned)
            {
                PlaceWallArt();
            }
        }

        private void _OnClientConnected(ulong clientId)
        {
            if (!IsServer || clientId == NetworkManager.ServerClientId)
            {
                // ignore if not server, or if clientId is server (already handled in OnNetworkSpawn)
                return;
            }

            SetPlayerParent(clientId, _worldLockParentInstance);
        }

        private void SetPlayerParent(ulong clientId, GameObject parent)
        {
            if (!IsSpawned || !IsServer) return;
            if (!NetworkManager.ConnectedClients.ContainsKey(clientId))
            {
                Debug.LogError($"[SVANESJO] could not find clientId {clientId} in NetworkManager.ConnectedClients");
                return;
            }

            NetworkManager.ConnectedClients[clientId].PlayerObject.TrySetParent(parent, false);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            _worldLockParentInstance = Instantiate(worldLockParent);
            _worldLockParentInstance.GetComponent<NetworkObject>().Spawn();
            SetPlayerParent(NetworkManager.LocalClientId, _worldLockParentInstance);
            SpawnTable();
            SpawnResetButton();
            SpawnWallArt();
        }

        public void Reset()
        {
            if (!IsServer)
            {
                ResetServerRpc();
                return;
            }

            DoReset();
        }

        private void DoReset()
        {
            ResetTable();
            ResetWallArt();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ResetServerRpc()
        {
            DoReset();
        }

        private void ResetTable()
        {
            foreach (var holder in GameObject.FindGameObjectsWithTag("TableHolder"))
            {
                // since a NetworkObject is not destroyed with parent, we need to destroy them manually
                for (var i = 0; i < holder.transform.childCount; i++)
                {
                    foreach (var n in holder.transform.GetChild(i).GetComponentsInChildren<NetworkObject>())
                    {
                        Destroy(n.gameObject);
                    }
                }

                foreach (var heldObject in holder.GetComponent<TableHolder>().heldObjects)
                {
                    if (heldObject == null)
                    {
                        // already destroyed
                        continue;
                    }

                    // most objects are already covered by the above Destroy, but we assume repeated calls won't hurt
                    // since actual destruction is delayed until the end of the current update loop
                    Destroy(heldObject);
                }

                Destroy(holder);
            }

            SpawnTable();
        }

        private void SpawnTable()
        {
            if (!IsServer) return;
            var holder = Instantiate(tableHolder);
            var holderNetworkObject = holder.GetComponent<NetworkObject>();
            holderNetworkObject.Spawn();
            holderNetworkObject.TrySetParent(_worldLockParentInstance.transform, false);
            var holderTransform = holder.transform;
            var heldObjects = new List<GameObject>();
            for (var i = 0; i < tableReference.transform.childCount; i++)
            {
                var referencePart = tableReference.transform.GetChild(i).gameObject;
                GameObject partPrefab;
                try
                {
                    partPrefab = tableParts.Single(o => o.name == referencePart.name);
                }
                catch (Exception)
                {
                    Debug.LogError(
                        $"[SVANESJO] failed to determine prefab for table reference part '{referencePart.name}'");
                    throw;
                }

                var part = Instantiate(partPrefab, referencePart.transform.position, referencePart.transform.rotation);
                heldObjects.Add(part);
                part.transform.localScale = referencePart.transform.localScale;
                var partNetworkObject = part.GetComponent<NetworkObject>();
                if (partNetworkObject == null) continue;
                partNetworkObject.Spawn();
                partNetworkObject.TrySetParent(holderTransform, false);
            }

            holder.heldObjects = heldObjects.ToArray();
        }

        private void SpawnResetButton()
        {
            if (!IsServer) return;
            _resetButtonInstance = Instantiate(resetButton);
            var resetButtonNetworkObject = _resetButtonInstance.GetComponent<NetworkObject>();
            resetButtonNetworkObject.Spawn();
            resetButtonNetworkObject.TrySetParent(_worldLockParentInstance, false);
        }

        private void PlaceResetButton()
        {
            // choose the widest wall
            if (_wallAnchorPlanes == null) return;
            var wall = _wallAnchorPlanes.Aggregate((agg, next) => next.Width > agg.Width ? next : agg);
            var wallTransform = wall.transform;
            _resetButtonInstance.transform.rotation =
                Quaternion.LookRotation(-wallTransform.up, wallTransform.forward);
            var wallPosition = wallTransform.position;
            var bounds = BoundUtils.GetObjectAndChildrenComponentBounds<Renderer>(_resetButtonInstance);
            var buttonPosition = new Vector3(
                wallPosition.x,
                resetButtonYPosition,
                wallPosition.z
            ) + (resetButtonOutFromWallOffset + bounds.extents.x) * _resetButtonInstance.transform.up.normalized;
            var resetButtonRectangle = new Rectangle(
                buttonPosition.x,
                buttonPosition.y,
                bounds.size.y,
                bounds.size.z
            );
            _resetButtonInstance.transform.position = buttonPosition;
            var wallId = wall.GetInstanceID();
            if (!_placedWallFixtures.ContainsKey(wallId))
            {
                _placedWallFixtures[wallId] =
                    new List<(WallFixture type, Rectangle rectangle, GameObject gameObject)>();
            }
            else
            {
                // make sure any existing overlapping fixtures are removed 
                foreach (var p in _placedWallFixtures[wallId])
                {
                    if (p.type == WallFixture.ResetButton)
                    {
                        Debug.LogError("[SVANESJO] unexpected duplicate reset button, abort PlaceResetButton");
                        _resetButtonPlaced = true;
                        return;
                    }

                    if (resetButtonRectangle.Overlaps(p.rectangle))
                    {
                        Debug.LogWarning(
                            "[SVANESJO] overlapping wall fixture removed to make space for reset button");
                        Destroy(p.gameObject);
                    }
                }
            }

            _placedWallFixtures[wallId].Add((WallFixture.ResetButton, resetButtonRectangle, _resetButtonInstance));
            _resetButtonPlaced = true;
        }

        private void ResetWallArt()
        {
            foreach (var key in _placedWallFixtures.Keys)
            {
                _placedWallFixtures[key].RemoveAll(p => p.type == WallFixture.Art);
            }

            foreach (var piece in _wallArtPieceInstances)
            {
                Destroy(piece);
            }

            _wallArtPieceInstances.Clear();
            _wallArtPlaced = false;
            SpawnWallArt();
        }

        private void SpawnWallArt()
        {
            if (!IsServer) return;
            foreach (var piece in wallArtPieces)
            {
                var pieceInstance = Instantiate(piece);
                pieceInstance.transform.localScale = Random.Range(minWallArtScale, maxWallArtScale) * Vector3.one;
                _wallArtPieceInstances.Add(pieceInstance);
                var pieceNetworkObject = pieceInstance.GetComponent<NetworkObject>();
                pieceNetworkObject.Spawn();
                pieceNetworkObject.TrySetParent(_worldLockParentInstance, false);
            }
        }

        private void PlaceWallArt()
        {
            var rand = new System.Random();
            foreach (var piece in _wallArtPieceInstances.OrderBy(_ => rand.Next()))
            {
                var bounds = BoundUtils.GetObjectAndChildrenComponentBounds<Renderer>(piece);
                var placementResult =
                    FindRandomNonOverlappingWallPlacement(bounds.size.x, bounds.size.y, bounds.size.z);
                if (placementResult == null)
                {
                    Destroy(piece);
                    continue;
                }

                var (wallId, position, rotation, rectangle) = placementResult.Value;
                piece.transform.position = position;
                piece.transform.rotation = rotation;
                if (!_placedWallFixtures.ContainsKey(wallId))
                {
                    _placedWallFixtures[wallId] =
                        new List<(WallFixture type, Rectangle rectangle, GameObject gameObject)>();
                }

                _placedWallFixtures[wallId].Add((WallFixture.Art, rectangle, piece));
            }

            _wallArtPlaced = true;
        }

        private (int wallId, Vector3 position, Quaternion rotation, Rectangle rectangle)?
            FindRandomNonOverlappingWallPlacement(
                float width, float height, float depth)
        {
            var wallPadding = 1 - 2 * wallArtPercentWallPadding / 100;
            var attempts = 0;
            var candidateWalls = _wallAnchorPlanes.ToList();
            while (candidateWalls.Count > 0 && attempts < maxWallArtPlacementAttempts)
            {
                var wall = candidateWalls[Random.Range(0, candidateWalls.Count)];
#if UNITY_EDITOR
                // use plane bounds since dimensions are missing in the simulated OVRScenePlanes
                var wallBounds = BoundUtils.GetObjectAndChildrenComponentBounds<Renderer>(wall.gameObject);
                var wallWidth = wallBounds.size.x;
                var wallHeight = wallBounds.size.y;
#else
                var wallWidth = wall.Width;
                var wallHeight = wall.Height;
#endif
                var maxWidthOffset = (wallPadding * wallWidth - width) / 2;
                var maxHeightOffset = (wallPadding * wallHeight - height) / 2;
                if (maxWidthOffset < 0 || maxHeightOffset < 0)
                {
                    // wall is not big enough to fit the given dimensions (width, height)
                    candidateWalls.Remove(wall);
                }
                else
                {
                    var rightOffset = Random.Range(-maxWidthOffset, maxWidthOffset);
                    var upOffset = Random.Range(-maxHeightOffset, maxHeightOffset);
                    var wallTransform = wall.transform;
                    var placementPosition =
                        wallTransform.position
                        + wallTransform.forward.normalized * (wallArtOutFromWallOffset + depth / 2.0f)
                        + wallTransform.right.normalized * rightOffset
                        + wallTransform.up.normalized * upOffset;
                    var placementRectangle = new Rectangle(placementPosition.x, placementPosition.y, width, height);
                    var wallId = wall.GetInstanceID();
                    var maxDimension = Math.Max(placementRectangle.Width, placementRectangle.Height);
                    if (!_placedWallFixtures.ContainsKey(wallId) || !_placedWallFixtures[wallId].Any(p =>
                            p.rectangle.Overlaps(
                                placementRectangle,
                                wallArtPercentGap / 100.0f * Math.Max(
                                    Math.Max(p.rectangle.Width, p.rectangle.Height),
                                    maxDimension
                                )
                            )))
                    {
                        return (wallId, placementPosition, wall.transform.rotation, placementRectangle);
                    }
                }

                attempts++;
            }

            return null;
        }
    }
}