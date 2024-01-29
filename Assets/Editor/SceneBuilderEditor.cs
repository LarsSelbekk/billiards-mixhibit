using System.Collections.Generic;
using Components;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(SceneBuilder))]
    public class SceneBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var sceneBuilder = (SceneBuilder)target;

            var tableReference = sceneBuilder.tableReference;
            if (tableReference == null)
            {
                sceneBuilder.tableParts = null;
                return;
            }

            var parts = new List<GameObject>();
            var tableTransform = tableReference.transform;
            for (var i = 0; i < tableTransform.childCount; i++)
            {
                parts.Add(PrefabUtility.GetCorrespondingObjectFromSource(tableTransform.GetChild(i)).gameObject);
            }

            sceneBuilder.tableParts = parts.ToArray();
        }
    }
}