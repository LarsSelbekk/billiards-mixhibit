// Adapted from https://forum.unity.com/threads/is-it-possible-to-split-submeshes-into-different-game-objects.1203928/

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Resources
{
    [RequireComponent(typeof(Renderer),typeof(MeshFilter))]
    public class ExtractSubmeshes : MonoBehaviour
    {

        public bool CreateMeshes { get; set; }

        private static Mesh ExtractSubmesh(Mesh mesh,int submesh)
        {
            Mesh newMesh = new Mesh();
            SubMeshDescriptor descriptor = mesh.GetSubMesh(submesh);
            newMesh.vertices = mesh.vertices[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];

            if(mesh.tangents != null && mesh.tangents.Length == mesh.vertices.Length)
            {
                newMesh.tangents = mesh.tangents[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if(mesh.boneWeights != null && mesh.boneWeights.Length == mesh.vertices.Length)
            {
                newMesh.boneWeights = mesh.boneWeights[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if(mesh.uv != null && mesh.uv.Length == mesh.vertices.Length)
            {
                newMesh.uv = mesh.uv[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if(mesh.uv2 != null && mesh.uv2.Length == mesh.vertices.Length)
            {
                newMesh.uv2 = mesh.uv2[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if(mesh.uv3 != null && mesh.uv3.Length == mesh.vertices.Length)
            {
                newMesh.uv3 = mesh.uv3[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if(mesh.uv4 != null && mesh.uv4.Length == mesh.vertices.Length)
            {
                newMesh.uv4 = mesh.uv4[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if(mesh.uv5 != null && mesh.uv5.Length == mesh.vertices.Length)
            {
                newMesh.uv5 = mesh.uv5[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if(mesh.uv6 != null && mesh.uv6.Length == mesh.vertices.Length)
            {
                newMesh.uv6 = mesh.uv6[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if (mesh.uv7 != null && mesh.uv7.Length == mesh.vertices.Length)
            {
                newMesh.uv7 = mesh.uv7[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if (mesh.uv8 != null && mesh.uv8.Length == mesh.vertices.Length)
            {
                newMesh.uv8 = mesh.uv8[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if (mesh.colors != null && mesh.colors.Length == mesh.vertices.Length)
            {
                newMesh.colors = mesh.colors[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            if(mesh.colors32 != null && mesh.colors32.Length == mesh.vertices.Length)
            {
                newMesh.colors32 = mesh.colors32[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }

            var triangles = mesh.triangles[descriptor.indexStart..(descriptor.indexStart+descriptor.indexCount)];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] -= descriptor.firstVertex;
            }

            newMesh.triangles = triangles;

            if (mesh.normals != null && mesh.normals.Length == mesh.vertices.Length)
            {
                newMesh.normals = mesh.normals[descriptor.firstVertex..(descriptor.firstVertex+descriptor.vertexCount)];
            }
            else
            {
                newMesh.RecalculateNormals();
            }

            newMesh.Optimize();
            newMesh.OptimizeIndexBuffers();
            newMesh.RecalculateBounds();
            newMesh.name = mesh.name + $" Submesh {submesh}";
            return newMesh;
        }

        public static string LastFilePath = "";

        private void OnValidate()
        {
            if (!CreateMeshes) return;
            CreateMeshes = false;
            Create();
        }

        void Create()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if(meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning("No mesh exists on this gameObject");
                return;
            }

            if(meshFilter.sharedMesh.subMeshCount <= 1)
            {
                Debug.LogWarning("Mesh has <= 1 submesh components. No additional extraction required.");
                return;
            }

            if(LastFilePath == "")
            {
                LastFilePath = Application.dataPath;
            }

            for(int i = 0;i < meshFilter.sharedMesh.subMeshCount;i++)
            {
                string filePath = EditorUtility.SaveFilePanelInProject("Save Procedural Mesh", "Procedural Mesh", "asset", "", LastFilePath);
                if (filePath == "") continue;

                LastFilePath = Directory.GetDirectoryRoot(filePath);
                Debug.Log(LastFilePath);

                Mesh mesh = ExtractSubmesh(meshFilter.sharedMesh, i);
                AssetDatabase.CreateAsset(mesh, filePath);
            }
        }
    }
}
#else
using UnityEngine;
public class ExtractSubmeshes : MonoBehaviour
{

}
#endif
