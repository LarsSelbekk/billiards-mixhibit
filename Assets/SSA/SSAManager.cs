using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Utils;

namespace SSA
{
    public class SSAManager : NetworkBehaviour
    {
        public OVRSpatialAnchor worldSSA;
        public OVRSpatialAnchor originalSSA;

        // SSA anchor use to store in Cloud
        private OVRSpatialAnchor _originalWorldSSA;

        // SSA anchor loaded from Cloud
        private OVRSpatialAnchor _loadedWorldSSA;

        public float alignPositionThreshold = 0.001f;
        public float maxAlignPosition = 1.0f;

        public float alignRotationThreshold = 0.001f;
        public float maxAlignRotation = 90.0f;

        public float alignIntervalSeconds = 0.5f;

        private readonly NetworkVariable<NetworkGuid> _ssaUuid = new(Guid.Empty.ToNetworkGuid());

        private bool _ssaReady;

        private GameObject _worldLockParent;

#if UNITY_EDITOR
        private void Awake()
        {
            // don't use SSAs in editor
            Destroy(this);
        }
#endif

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                if (_ssaUuid.Value.ToGuid() == Guid.Empty)
                {
                    Debug.LogWarning("[SVANESJO] SSA not ready on client");
                    return;
                }

                LoadSSA();
                return;
            }

            StartCoroutine(SaveSSA());
        }

        [ClientRpc]
        private void LoadSSAClientRpc()
        {
            LoadSSA();
        }

        private IEnumerator SaveSSA()
        {
            if (!IsServer)
            {
                Debug.LogWarning("[SVANESJO] 🚫 attempted to run SaveSSA on client");
                yield break;
            }

            GameObject worldLockParent = null;
            while (worldLockParent == null)
            {
                worldLockParent = GameObject.FindGameObjectWithTag("WorldLockParent");
                if (worldLockParent != null) break;
                Debug.Log("[SVANESJO] ⏳ waiting for WorldLockParent ...");
                yield return new WaitForSeconds(0.5f);
            }

            var worldLockParenTransform = worldLockParent.transform;

            _originalWorldSSA =
                Instantiate(originalSSA, worldLockParenTransform.position, worldLockParenTransform.rotation);

            while (_originalWorldSSA && !_originalWorldSSA.Created)
            {
                Debug.Log("[SVANESJO] ⏳ waiting for SSA to be created ...");
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log($"[SVANESJO] ⚓ SSA {_originalWorldSSA.Uuid} created");

            _originalWorldSSA.Save(new OVRSpatialAnchor.SaveOptions
            {
                Storage = OVRSpace.StorageLocation.Cloud
            }, (anchor, success) =>
            {
                Destroy(_originalWorldSSA);

                if (success)
                {
                    Debug.Log($"[SVANESJO] ☁️ SSA {anchor.Uuid} saved to Cloud");
                    _ssaUuid.Value = anchor.Uuid.ToNetworkGuid();
                    LoadSSAClientRpc();
                }
                else
                {
                    Debug.LogError($"[SVANESJO] failed to save SSA {anchor.Uuid} to Cloud");
                }
            });
        }

        private void LoadSSA()
        {
            _ssaReady = false;

            Destroy(_loadedWorldSSA);

            var ssaUuid = _ssaUuid.Value.ToGuid();

            if (ssaUuid == Guid.Empty)
            {
                Debug.LogError("[SVANESJO] no SSA available");
                return;
            }

            Debug.Log($"[SVANESJO] 🏗️ loading SSA {ssaUuid} ...");

            var loading = OVRSpatialAnchor.LoadUnboundAnchors(new OVRSpatialAnchor.LoadOptions()
            {
                StorageLocation = OVRSpace.StorageLocation.Cloud,
                Uuids = new[] { ssaUuid }
            }, loadedAnchors =>
            {
                if (loadedAnchors == null || loadedAnchors.Length == 0)
                {
                    Debug.LogError($"[SVANESJO] failed to load SSA {ssaUuid}");
                    return;
                }

                var anchor = loadedAnchors[0];
                Debug.Log($"[SVANESJO] ✅ SSA {anchor.Uuid} loaded");
                anchor.Localize((a, success) =>
                {
                    if (!success)
                    {
                        Debug.LogError($"[SVANESJO] failed to localize SSA {anchor.Uuid}");
                        return;
                    }

                    Debug.Log($"[SVANESJO] 🎯 SSA {anchor.Uuid} localized");
                    _loadedWorldSSA = Instantiate(worldSSA);
                    a.BindTo(_loadedWorldSSA);
                    _ssaReady = true;
                });
            });

            if (!loading)
            {
                Debug.LogError("[SVANESJO] failed to send SSA load request");
            }
        }

        private float _nextAlignTime;

        private void Update()
        {
            if (!IsSpawned || Time.time < _nextAlignTime) return;
            _nextAlignTime = Time.time + alignIntervalSeconds;
            if (!_ssaReady)
            {
                Debug.Log("[SVANESJO] ⏳ waiting for SSA ready ...");
                return;
            }

            AlignWorldToSSA();
        }

        private void AlignWorldToSSA()
        {
            if (_worldLockParent == null)
            {
                _worldLockParent = GameObject.FindGameObjectWithTag("WorldLockParent");
                if (_worldLockParent == null)
                {
                    Debug.LogError("[SVANESJO] WorldLockParent not found");
                    return;
                }
            }

            var anchorTransform = _loadedWorldSSA.transform;

            // we only want to adjust rotation around the y-axis
            var anchorYOnlyRotation = Quaternion.Euler(0, anchorTransform.rotation.eulerAngles.y, 0);
            var clampedAdjustedRotation = Quaternion.RotateTowards(_worldLockParent.transform.rotation,
                anchorYOnlyRotation,
                maxAlignRotation);
            var rotationAngleAdjust = Quaternion.Angle(clampedAdjustedRotation, _worldLockParent.transform.rotation);

            var clampedPositionAdjust = Vector3.ClampMagnitude(
                anchorTransform.position - _worldLockParent.transform.position,
                maxAlignPosition);

            if (!(clampedPositionAdjust.magnitude > alignPositionThreshold) &&
                !(rotationAngleAdjust > alignRotationThreshold)) return;

            Debug.Log(
                $"[SVANESJO] ✨ adjusting position ∆{clampedPositionAdjust} and rotation ∆(y={rotationAngleAdjust})");
            _worldLockParent.transform.position += clampedPositionAdjust;
            _worldLockParent.transform.rotation = clampedAdjustedRotation;
        }
    }
}