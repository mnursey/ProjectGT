using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratedCube : MonoBehaviour
{
    private MeshFilter filter;

    void Start()
    {
        Setup();
    }

    public void Setup()
    {
        filter = GetComponent<MeshFilter>();
        filter.mesh = GenerateMesh();
        Destroy(GetComponent<MeshCollider>());
        MeshCollider meshCollider = this.gameObject.AddComponent<MeshCollider>();
        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshCollider>().sharedMesh = filter.mesh;
    }

    Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> verticies = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        verticies.AddRange(new List<Vector3>() {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1)
        });

        triangles.AddRange(new List<int>() {
            0, 2, 1, //face front
			0, 3, 2,
            2, 3, 4, //face top
			2, 4, 5,
            1, 2, 5, //face right
			1, 5, 6,
            0, 7, 4, //face left
			0, 4, 3,
            5, 4, 7, //face back
			5, 7, 6,
            0, 6, 7, //face bottom
			0, 1, 6
        });

        mesh.SetVertices(verticies);
        mesh.SetTriangles(triangles, 0);
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        return mesh;
    }
}
