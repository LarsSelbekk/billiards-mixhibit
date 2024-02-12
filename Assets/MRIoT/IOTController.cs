#nullable enable

using System;
using System.Collections.Generic;
using Attributes;
using Exact;
using Exact.Example;
using NaughtyAttributes;
using UnityEngine;

namespace MRIoT
{
    [RequireComponent(typeof(ExactManager))]
    public class IOTController : MonoBehaviour
    {
        [SerializeField, Required] private Hole prefab = null!;

        [SerializeField] private int scoreFadeTime = 3;

        [SerializeField, ReadOnlyInInspector] private List<Hole> holes = new();

        [SerializeField] private string[] deviceNames = { };

        [SerializeField] private Color[] connectColors = { };

        private ExactManager _exactManager = null!;

        private void Awake()
        {
            if (prefab == null)
            {
                throw new ArgumentNullException();
            }

            _exactManager = GetComponent<ExactManager>();
            if (_exactManager == null)
            {
                throw new MissingComponentException();
            }

            if (deviceNames.Length < PocketDefinition.PocketDefinitions.Length)
            {
                Debug.LogError($"At least {PocketDefinition.PocketDefinitions.Length} devices required");
                // throw new ArgumentException($"At least {PocketDefinition.PocketDefinitions.Length} devices required");
            }
        }

        private void Start()
        {
            foreach (var id in deviceNames)
            {
                var hole = Instantiate(prefab);
                hole.Device.SetDeviceName(id);
                _exactManager.AddDevice(hole.Device);
                if (hole == null)
                {
                    throw new MissingComponentException();
                }
                holes.Add(hole);
            }

            for (var i = 0; i < holes.Count; i++)
            {
                var hole = holes[i];
                // Set color to the corresponding index, wrapping if necessary
                var color = connectColors[i % connectColors.Length];
                hole.LedRing.SetColor(color);
                // hole.LedRing.SetColorPartial(color, (i + 1) / holes.Count)

                hole.SetConnectedColor(color);
            }
        }

        public void Scored(BallDefinition ball, PocketDefinition pocket)
        {
            Debug.Log($"IoTController Scored: {ball.Name} shot down in {pocket.Name}");
            if (holes.Count >= pocket.Index)
            {
                Debug.LogError($"IoTController Scored: {pocket.Index} out of bounds for {holes.Count} devices");
                return;
            }

            var ledRing = holes[pocket.Index].GetComponent<LedRing>();
            var lowestIntensity = ball.IsStriped ? 0f : 0.3f;
            ledRing.StartFading(ball.Color, lowestIntensity, 1f, scoreFadeTime);
        }
    }
}
