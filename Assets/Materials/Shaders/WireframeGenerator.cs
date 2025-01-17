using System.Collections.Generic; // For List<>
using UnityEngine;
using System.Linq; // Add this for Enumerable

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WireframeGenerator : MonoBehaviour
{
    public Material wireframeMaterial;

    void Start()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh mesh = filter.mesh;

        // Generate wireframe using edge data
        var lines = new List<Vector3>();
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            int i0 = mesh.triangles[i];
            int i1 = mesh.triangles[i + 1];
            int i2 = mesh.triangles[i + 2];

            lines.Add(mesh.vertices[i0]);
            lines.Add(mesh.vertices[i1]);

            lines.Add(mesh.vertices[i1]);
            lines.Add(mesh.vertices[i2]);

            lines.Add(mesh.vertices[i2]);
            lines.Add(mesh.vertices[i0]);
        }

        Mesh lineMesh = new Mesh();
        lineMesh.vertices = lines.ToArray();
        lineMesh.SetIndices(
            Enumerable.Range(0, lineMesh.vertices.Length).ToArray(), // Ensure Enumerable is recognized
            MeshTopology.Lines,
            0
        );

        GameObject wireframeObj = new GameObject("Wireframe");
        wireframeObj.transform.SetParent(transform);
        wireframeObj.transform.localPosition = Vector3.zero;
        wireframeObj.transform.localRotation = Quaternion.identity;
        wireframeObj.AddComponent<MeshFilter>().mesh = lineMesh;
        wireframeObj.AddComponent<MeshRenderer>().material = wireframeMaterial;
    }
}
