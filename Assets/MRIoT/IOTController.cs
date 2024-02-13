#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using Exact;
using NaughtyAttributes;
using UnityEngine;

namespace MRIoT
{
    [RequireComponent(typeof(ExactManager))]
    public class IOTController : MonoBehaviour
    {
        [SerializeField, Required] private Pocket pocketPrefab = null!;
        [SerializeField] private Pocket.PocketCommonConfig pocketsCommonConfig = null!;
        [SerializeField] private Pocket.PocketDeviceConfig[] pocketsDeviceConfigs = { };

        private ExactManager _exactManager = null!;
        private readonly Dictionary<PocketEnum, Pocket> _pockets = new();
        private readonly Dictionary<PocketEnum, Coroutine> _coroutines = new();
        private BallDefinition? _lastBall = null;

        private void Awake()
        {
            if (pocketPrefab == null)
            {
                throw new ArgumentNullException();
            }

            _exactManager = GetComponent<ExactManager>();
            if (_exactManager == null)
            {
                throw new MissingComponentException();
            }

            if (pocketsDeviceConfigs.Length < PocketDefinition.PocketDefinitions.Length)
            {
                Debug.LogWarning($"{PocketDefinition.PocketDefinitions.Length} devices recommended, only {pocketsDeviceConfigs.Length} configured");
            }
        }

        private void Start()
        {
            foreach (var config in pocketsDeviceConfigs)
            {
                var pocket = Instantiate(pocketPrefab);
                pocket.Initialize(config, pocketsCommonConfig, _exactManager);
                _pockets.Add(config.pocketLocation, pocket);
            }
        }

        public void Scored(BallEnum ballEnum, PocketEnum pocketEnum)
        {
            var ballDefinition = BallDefinition.BallDefinitions[(int)ballEnum];
            var pocketDefinition = PocketDefinition.PocketDefinitions[(int)pocketEnum];
            Debug.Log($"IOTController Scored: {ballDefinition.Name} shot down in {pocketDefinition.Name}");

            // Start coroutine, stop if one is already running for this pocket, then add to map
            Debug.Log("IOTController Scored starting coroutine");
            lock (_coroutines)
            {
                Debug.Log("IOTController Scored locked _coroutines");

                // If scored black, stop all coroutines
                if (ballDefinition.Enum == BallEnum.Black)
                {
                    StopAllCoroutines();
                }
                // If scored after black, reset all pockets
                else if(_lastBall?.Enum == BallEnum.Black)
                {
                    foreach (var e in _pockets.Values)
                    {
                        StartCoroutine(ResetBlack(e));
                    }
                }
                // Otherwise remove if any previously running coroutines for this pocket
                else if (_coroutines.Remove(pocketDefinition.Enum, out var previous))
                {
                    Debug.Log($"IOTController Scored stopping old coroutine {previous}");
                    StopCoroutine(previous);
                }

                var coroutine = StartCoroutine(ScoredCoroutine(pocketDefinition, ballDefinition, _lastBall));
                _lastBall = ballDefinition;
                Debug.Log($"IOTController Scored adding coroutine {coroutine}");
                _coroutines.Add(pocketDefinition.Enum, coroutine);
            }
        }

        private IEnumerator ResetBlack(Pocket pocket)
        {
            pocket.ResetBlack();
            yield break;
        }

        private IEnumerator ScoredCoroutine(PocketDefinition pocketDefinition, BallDefinition ballDefinition, BallDefinition? lastBall)
        {
            Debug.Log("IOTController Coroutine entered");

            // Scored black: Set state for all pockets, then break
            if (ballDefinition.Enum == BallEnum.Black)
            {
                foreach (var e in _pockets.Values)
                {
                    e.ScoreBlack();
                }
                yield break;
            }

            var pocket = _pockets[pocketDefinition.Enum];
            // If no configured pocket, break
            if (pocket is null)
            {
                Debug.LogWarning($"IOTController Coroutine scored in pocket {pocketDefinition.Enum} with no configured device, ignoring");
                yield break;
            }

            // Update pocket, break if aborted (returned false)
            if (!pocket.Scored(ballDefinition))
            {
                yield break;
            }

            Debug.Log($"IOTController Coroutine [{pocketDefinition.Name}, {ballDefinition.Name}] waiting for {pocketsCommonConfig.scoredFadeTime} seconds");
            yield return new WaitForSeconds(pocketsCommonConfig.scoredFadeTime);
            Debug.Log($"IOTController Coroutine for [{pocketDefinition.Name}, {ballDefinition.Name}] resuming and shutting down");
            pocket.ResetScored();
        }
    }
}
