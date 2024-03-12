// #define DEBUG_AUTHORITY_COLORS
// #define DEBUG_LOGS

using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit;
using Utils;

namespace Networking
{
    /**
     *  Based on Unity-UltimateGloveBall by oculus-samples
     *
     *      Copyright (c) Meta Platforms, Inc. and affiliates.
     *      Use of the material below is subject to the terms of the MIT License
     *      https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE
     */
    
    [RequireComponent(typeof(Rigidbody))]
    public class ObjectStateSync : NetworkBehaviour
    {
        private struct ObjectStateUpdate : INetworkSerializable
        {
            public bool IsGrabbed;

            public Vector3 Position;
            public Quaternion Orientation;
            public bool SyncVelocity;
            public Vector3 LinearVelocity;
            public Vector3 AngularVelocity;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref IsGrabbed);

                serializer.SerializeValue(ref Position);
                serializer.SerializeValue(ref Orientation);
                serializer.SerializeValue(ref SyncVelocity);

                // Serialize velocity only if objects are moving
                if (!SyncVelocity) return;

                serializer.SerializeValue(ref LinearVelocity);
                serializer.SerializeValue(ref AngularVelocity);
            }
        }

        private struct ObjectPacket : INetworkSerializable
        {
            public ushort OwnershipSequence;
            public uint OwnershipTimestamp;
            public ushort AuthoritySequence;
            public ulong AuthorityClientId;

            public ObjectStateUpdate StateUpdate;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref OwnershipSequence);
                serializer.SerializeValue(ref OwnershipTimestamp);
                serializer.SerializeValue(ref AuthoritySequence);
                serializer.SerializeValue(ref AuthorityClientId);
                serializer.SerializeValue(ref StateUpdate);
            }
        }

        // TODO: adjust update and flush rates
        [Header("Synchronization")]
        [Tooltip("This value determines how often the server sends updates. The update rate is per FixedUpdate.")]
        [SerializeField]
        private uint updateRate = 1;

        [Tooltip(
            "This value determines how often a client will flush its jitter buffer. The update rate is per FixedUpdate.")]
        [SerializeField]
        private uint bufferFlushRate = 1;

        private Rigidbody _rigidbody;

        private XRGrabInteractable _grabInteractable;

        // remembering `m_grabInteractable != null` from startup to avoid repeating expensive null checks
        private bool _isGrabbable;

        private ushort _ownershipSequence;
        private uint _ownershipTimestamp;
        private ushort _authoritySequence;
        private ulong _authorityClientId = Authority.NoClientID;
        private ulong _grabberClientId = Authority.NoClientID;

        private bool IsGrabbedByLocalClient => _grabberClientId == NetworkManager.LocalClientId;

        private bool IsGrabbedOnNetwork => _grabberClientId != Authority.NoClientID;

        private readonly CircularBuffer<ObjectPacket> _jitterBuffer = new(64);

        private uint _frameCount;

        private bool _isPendingDefaultAuthorityCommit;

        private bool _isAuthorityConfirmed;

        private uint _lastActiveFrame;

        private bool _velocitySynced;

#if DEBUG_AUTHORITY_COLORS
        private Renderer[] _renderers;
#endif

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            Assert.IsNotNull(_rigidbody, $"Could not find script of type {typeof(Rigidbody)}");

            _grabInteractable = GetComponent<XRGrabInteractable>();
            if (_grabInteractable == null)
            {
                Debug.LogWarning($"Could not find script of type {typeof(XRGrabInteractable)}");
                return;
            }

            _grabInteractable.selectEntered.AddListener(_ => OnGrabObject());
            _grabInteractable.selectExited.AddListener(_ => OnReleaseObject());

            _isGrabbable = true;

#if DEBUG_AUTHORITY_COLORS
            _renderers = GetComponentsInChildren<Renderer>();
#endif
        }

        private void OnDisable()
        {
            ResetObject();
        }

        /// <summary>
        ///     Call this to reset all states, local and networked.
        /// </summary>
        private void ResetObject()
        {
            EnablePhysics(false);
            _ownershipSequence = default;
            _ownershipTimestamp = default;
            _authoritySequence = default;
            _authorityClientId = Authority.NoClientID;
            _grabberClientId = Authority.NoClientID;
        }

        private void TakeOwnershipOfObjectFromGrab()
        {
            _grabberClientId = NetworkManager.LocalClientId;
            _ownershipSequence++;
            _ownershipTimestamp = _frameCount;
            _authorityClientId = NetworkManager.LocalClientId;
            _authoritySequence = 0;
            _isAuthorityConfirmed = IsServer;
        }

        private void TakeAuthorityOverObject()
        {
            _authorityClientId = NetworkManager.LocalClientId;
            _authoritySequence++;
            _isAuthorityConfirmed = IsServer;
        }

        private void OnGrabObject()
        {
            if (IsGrabbedOnNetwork)
            {
                if (IsGrabbedByLocalClient)
                {
                    // we are already grabbing, no further actions required
                    // (this can occur if switching hands)
                    return;
                }

                // already grabbed by other client, force drop object
                ForceReleaseObjectGrab();
                return;
            }

            TakeOwnershipOfObjectFromGrab();
        }

        private void ForceReleaseObjectGrab()
        {
            if (!_isGrabbable) return;
            _grabInteractable.interactionManager.CancelInteractableSelection(
                (IXRSelectInteractable)_grabInteractable
            );
        }

        private void OnReleaseObject()
        {
            _grabberClientId = Authority.NoClientID;
            _ownershipTimestamp = _frameCount;
        }

        public void OnCollisionEnter(Collision other)
        {
            // ignore if we don't have authority of this object
            if (_authorityClientId != NetworkManager.LocalClientId) return;
            var objectState = other.gameObject.GetComponent<ObjectStateSync>();
            // ignore if other object is grabbed or not part of the authority system
            if (objectState == null || objectState.IsGrabbedOnNetwork) return;
            // ignore if we are not grabbing and another client has authority of the other object with a more recent grab
            if (!IsGrabbedByLocalClient && objectState._authorityClientId != Authority.NoClientID &&
                objectState._ownershipTimestamp > _ownershipTimestamp && objectState._isAuthorityConfirmed) return;
            if (IsGrabbedByLocalClient) _ownershipTimestamp = _frameCount;
            objectState.TakeAuthorityOverObject();
            objectState._ownershipTimestamp = _ownershipTimestamp;
        }

        public void FixedUpdate()
        {
            if (_frameCount % bufferFlushRate == 0 || IsGrabbedOnNetwork)
            {
#if DEBUG_LOGS
                Debug.Log($"[SVANESJO] {name} ready to apply {_jitterBuffer.Count()} packets");
#endif
                while (_jitterBuffer.Any())
                {
                    var packet = _jitterBuffer.GetLast();
                    if (!_isAuthorityConfirmed && packet.AuthorityClientId == NetworkManager.LocalClientId)
                    {
                        _isAuthorityConfirmed = true;
                    }

                    if (!Authority.ShouldApplyObjectStateUpdate(
                            _ownershipSequence,
                            _authoritySequence,
                            packet.OwnershipSequence,
                            packet.AuthoritySequence,
                            packet.AuthorityClientId,
                            _authorityClientId,
                            NetworkManager.ServerClientId,
                            NetworkManager.LocalClientId,
                            NetworkManager.ServerClientId
                        ))
                    {
#if DEBUG_LOGS
                        Debug.Log("[SVANESJO] " + name + " rejected packet ApplyUpdate after authority check");
#endif
                        continue;
                    }

                    ApplyPacket(packet);
                }
            }

            if (IsServer || _authorityClientId == NetworkManager.LocalClientId ||
                (_authorityClientId == Authority.NoClientID && _isPendingDefaultAuthorityCommit))
            {
                if (_frameCount % updateRate == 0 || IsGrabbedOnNetwork)
                {
                    // We send packets with a given frequency, but sometimes we override this to send packets right away.
                    SendPacket();
                }
            }

            _frameCount++;
        }

        /// <summary>
        ///     Used to both create and send packets with transform and rigidbody status.
        /// </summary>
        private void SendPacket()
        {
            var objectTransform = transform;
            var parentTransform = objectTransform.parent;

            var velocity = _rigidbody.velocity;
            var angularVelocity = _rigidbody.angularVelocity;
            // TODO: tweak this
            var notAtRest = angularVelocity.magnitude > 0.1f || velocity.magnitude > 0.01f;

            var update = new ObjectStateUpdate
            {
                IsGrabbed = IsGrabbedOnNetwork,
                Position = objectTransform.localPosition,
                Orientation = objectTransform.localRotation,
                SyncVelocity = notAtRest,
                LinearVelocity = parentTransform.InverseTransformDirection(velocity),
                AngularVelocity = parentTransform.InverseTransformDirection(angularVelocity),
            };

            if (notAtRest)
            {
                _lastActiveFrame = _frameCount;
            }

            if (!update.IsGrabbed)
            {
                // TODO: check if this can be omitted
                if (IsServer)
                {
                    // object has been released, so re-enable physics
                    EnablePhysics(true);
                }

                if (_isAuthorityConfirmed && _authorityClientId != Authority.NoClientID &&
                    _authorityClientId == NetworkManager.LocalClientId &&
                    _lastActiveFrame < _frameCount - 1 / Time.fixedDeltaTime)
                {
                    // transfer authority back to default when object has come to rest
                    _authorityClientId = Authority.NoClientID;
                    _authoritySequence++;
                    if (!IsServer)
                    {
                        _isPendingDefaultAuthorityCommit = true;
                    }
                }
            }

            var packet = new ObjectPacket
            {
                OwnershipSequence = _ownershipSequence,
                OwnershipTimestamp = _ownershipTimestamp,
                AuthoritySequence = _authoritySequence,
                AuthorityClientId = _authorityClientId,
                StateUpdate = update
            };

#if DEBUG_AUTHORITY_COLORS
            var authorityClientId = packet.AuthorityClientId;
            Color debugColor;
            if (authorityClientId == Authority.NoClientID)
                debugColor = Color.white;
            else if (!update.IsGrabbed && _lastActiveFrame < _frameCount)
                if (_authorityClientId != Authority.NoClientID &&
                    _authorityClientId == NetworkManager.LocalClientId && !_isAuthorityConfirmed)
                    // is ready to return to default authority, but has not been confirmed
                    debugColor = Color.magenta;
                else
                    // waiting for some frames before returning to default authority
                    debugColor = Color.cyan;
            else if (_isPendingDefaultAuthorityCommit)
                // waiting for server to confirm return to default authority
                debugColor = Color.black;
            else
            {
                var debugColors = new List<Color>
                {
                    Color.red, Color.blue, Color.green, Color.yellow
                };
                debugColor = debugColors[(int)(authorityClientId % (ulong)debugColors.Count)];
            }

            foreach (var r in _renderers)
            {
                r.material.color = debugColor;
            }
#endif

#if DEBUG_LOGS
            Debug.Log(
                "[SVANESJO] " + name + " SendPacket"
                + " OwnershipSequence=" + packet.OwnershipSequence
                + " AuthoritySequence=" + packet.AuthoritySequence
                + " AuthorityClientId=" + packet.AuthorityClientId
                + " IsGrabbed=" + packet.StateUpdate.IsGrabbed
                + " Position=" + packet.StateUpdate.Position
                + " Orientation=" + packet.StateUpdate.Orientation
                + " SyncVelocity=" + packet.StateUpdate.SyncVelocity
                + " LinearVelocity=" + packet.StateUpdate.LinearVelocity
                + " AngularVelocity=" + packet.StateUpdate.AngularVelocity
            );
#endif
            SendPacketServerRpc(packet);
        }

        /// <summary>
        ///     Apply incoming status updates. Also applies lerp on position and velocity as well as slerp on rotation.
        ///     What data is applied and not depends on the object being grabbed or not.
        /// </summary>
        /// <param name="packet"></param>
        private void ApplyPacket(ObjectPacket packet)
        {
#if DEBUG_LOGS
            Debug.Log(
                "[SVANESJO] " + name + " ApplyPacket"
                + " OwnershipSequence=" + packet.OwnershipSequence
                + " AuthoritySequence=" + packet.AuthoritySequence
                + " AuthorityClientId=" + packet.AuthorityClientId
                + " IsGrabbed=" + packet.StateUpdate.IsGrabbed
                + " Position=" + packet.StateUpdate.Position
                + " Orientation=" + packet.StateUpdate.Orientation
                + " SyncVelocity=" + packet.StateUpdate.SyncVelocity
                + " LinearVelocity=" + packet.StateUpdate.LinearVelocity
                + " AngularVelocity=" + packet.StateUpdate.AngularVelocity
            );
#endif

            if (IsGrabbedByLocalClient)
            {
                // since we are applying, it means we don't have ownership or authority
                ForceReleaseObjectGrab();
            }

            var authorityClientId = packet.AuthorityClientId;
            _ownershipSequence = packet.OwnershipSequence;
            _authoritySequence = packet.AuthoritySequence;
            _authorityClientId = authorityClientId;
            if (_isPendingDefaultAuthorityCommit && authorityClientId != NetworkManager.LocalClientId)
            {
                _isPendingDefaultAuthorityCommit = false;
            }

#if DEBUG_AUTHORITY_COLORS
            if (!IsServer)
            {
                Color debugColor;
                if (authorityClientId == Authority.NoClientID)
                    debugColor = Color.white;
                else if (_isPendingDefaultAuthorityCommit)
                    // waiting for server to confirm return to default authority
                    debugColor = Color.black;
                else
                {
                    var debugColors = new List<Color>
                    {
                        Color.red, Color.blue, Color.green, Color.yellow
                    };
                    debugColor = debugColors[(int)(authorityClientId % (ulong)debugColors.Count)];
                }

                foreach (var r in _renderers)
                {
                    r.material.color = debugColor;
                }
            }
#endif

            var update = packet.StateUpdate;

            _grabberClientId = update.IsGrabbed ? packet.AuthorityClientId : Authority.NoClientID;

            var parentTransform = transform.parent;

            if (update.IsGrabbed)
            {
                if (packet.AuthorityClientId != NetworkManager.LocalClientId)
                {
                    // disable physics and follow transform if we are not the grabber
                    var t = transform;
                    EnablePhysics(false);
                    t.localPosition = update.Position;
                    t.localRotation = update.Orientation;
                    _rigidbody.position = t.position;
                    _rigidbody.rotation = t.rotation;
                    // TODO: check if below is required
                    _rigidbody.velocity = parentTransform.TransformDirection(update.LinearVelocity);
                    _rigidbody.angularVelocity = parentTransform.TransformDirection(update.AngularVelocity);
                }

                _velocitySynced = false;
                return;
            }

            _rigidbody.position = parentTransform.TransformPoint(update.Position);
            _rigidbody.rotation = parentTransform.rotation * update.Orientation;

            if (!update.SyncVelocity)
            {
                // at rest
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            var targetVelocity = parentTransform.TransformDirection(update.LinearVelocity);

            if (!_velocitySynced)
            {
                EnablePhysics(true);
                _rigidbody.velocity = targetVelocity;
                _velocitySynced = true;
            }

            _rigidbody.angularVelocity = parentTransform.TransformDirection(update.AngularVelocity);

            // TODO: check if this is beneficial
            // Introduced this check to battle the sharp change in velocity that results from crashing
            // into a wall. If object hits something and changes direction we don't apply velocity until
            // they are aligned again.
            if (Vector3.Dot(_rigidbody.velocity, targetVelocity) > 0f)
            {
                _rigidbody.velocity = targetVelocity;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendPacketServerRpc(ObjectPacket packet, ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId == NetworkManager.ServerClientId)
            {
                // broadcast server state to clients
                SendPacketClientRpc(packet);
                return;
            }

            if (!Authority.ShouldApplyObjectStateUpdate(
                    _ownershipSequence,
                    _authoritySequence,
                    packet.OwnershipSequence,
                    packet.AuthoritySequence,
                    packet.AuthorityClientId,
                    _authorityClientId,
                    serverRpcParams.Receive.SenderClientId,
                    NetworkManager.LocalClientId,
                    NetworkManager.ServerClientId
                ))
            {
#if DEBUG_LOGS
                Debug.Log("[SVANESJO] " + name + " rejected packet on server after authority check " + (
                    _ownershipSequence,
                    _authoritySequence,
                    packet.OwnershipSequence,
                    packet.AuthoritySequence,
                    packet.AuthorityClientId,
                    _authorityClientId,
                    serverRpcParams.Receive.SenderClientId,
                    NetworkManager.LocalClientId,
                    NetworkManager.ServerClientId
                ));
#endif
                return;
            }

            _jitterBuffer.Put(packet);
        }

        [ClientRpc]
        private void SendPacketClientRpc(ObjectPacket packet)
        {
            if (IsServer)
            {
                // the server has already applied this packet
                return;
            }

            _jitterBuffer.Put(packet);
        }

        private void EnablePhysics(bool enable)
        {
            _rigidbody.isKinematic = !enable;
            _rigidbody.useGravity = enable;
        }

        public void Reset()
        {
            _jitterBuffer.Clear();
            _frameCount = 0;
            _velocitySynced = false;
        }
    }
}
