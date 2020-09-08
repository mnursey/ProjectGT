using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratedTrackTile : MonoBehaviour
{
    private MeshFilter filter;

    public float size = 100;
    private static int gridSize = 3;

    public float grassUVColourPercent = 1.0f;
    public float cliffUVColourPercent = 0.1f;
    public float waterEdgeUVColourPercent = 0.1f;

    private TrackGenerator trackGenerator;

    private bool hasSetup = false;

    public int offsetX;
    public int offsetY;
    public float cliffAngle = 0.5f;

    private bool createdTrigs = false;

    void Start()
    {

    }

    public void Setup(int x, int y, float grassUVColourPercent, float cliffUVColourPercent, float waterEdgeUVColourPercent, TrackGenerator tg)
    {
        if(!createdTrigs)
        {
            filter = GetComponent<MeshFilter>();
            filter.mesh = GenerateMesh();
            createdTrigs = true;
        }

        trackGenerator = tg;

        offsetX = x;
        offsetY = y;

        this.grassUVColourPercent = grassUVColourPercent;
        this.cliffUVColourPercent = cliffUVColourPercent;
        this.waterEdgeUVColourPercent = waterEdgeUVColourPercent;

        UpdateTerrainHeight();
        hasSetup = true;
    }

    void AddQuad(float offsetX, float offsetY, List<Vector3> verticies, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
    {
        /*
        for (int y = 0; y < 2; ++y)
        {
            for (int x = 0; x < 2; ++x)
            {
                verticies.Add(new Vector3(-size * 0.5f + size * (x / 1) / gridSize + (offsetX / gridSize * size), 0, -size * 0.5f + size * (y / 1) / gridSize + (offsetY / gridSize * size)));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(grassUVColourPercent, 0.5f));
            }
        }*/

        verticies.Add(new Vector3(-size * 0.5f + (offsetX / gridSize * size), 0, -size * 0.5f + (offsetY / gridSize * size)));
        verticies.Add(new Vector3(-size * 0.5f + size / gridSize + (offsetX / gridSize * size), 0, -size * 0.5f + (offsetY / gridSize * size)));
        verticies.Add(new Vector3(-size * 0.5f + (offsetX / gridSize * size), 0, -size * 0.5f + size / gridSize + (offsetY / gridSize * size)));

        verticies.Add(new Vector3(-size * 0.5f + size / gridSize + (offsetX / gridSize * size), 0, -size * 0.5f + (offsetY / gridSize * size)));
        verticies.Add(new Vector3(-size * 0.5f + (offsetX / gridSize * size), 0, -size * 0.5f + size / gridSize + (offsetY / gridSize * size)));
        verticies.Add(new Vector3(-size * 0.5f + size / gridSize + (offsetX / gridSize * size), 0, -size * 0.5f + size / gridSize + (offsetY / gridSize * size)));

        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);

        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);

        uvs.Add(new Vector2(grassUVColourPercent, 0.5f));
        uvs.Add(new Vector2(grassUVColourPercent, 0.5f));
        uvs.Add(new Vector2(grassUVColourPercent, 0.5f));

        uvs.Add(new Vector2(grassUVColourPercent, 0.5f));
        uvs.Add(new Vector2(grassUVColourPercent, 0.5f));
        uvs.Add(new Vector2(grassUVColourPercent, 0.5f));

        triangles.AddRange(new List<int>() {
            verticies.Count - 6, verticies.Count - 4, verticies.Count - 5,
            verticies.Count - 2, verticies.Count - 1, verticies.Count - 3
        });
    }

    Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> verticies = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int y = 0; y < gridSize; ++y)
        {
            for (int x = 0; x < gridSize; ++x)
            {
                AddQuad(x, y, verticies, normals, uvs, triangles);
            }
        }

        mesh.SetVertices(verticies);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);

        return mesh;
    }

    void UpdateTerrainHeight()
    {
        Vector3[] verticies = filter.mesh.vertices;
        Vector2[] uvs = filter.mesh.uv;

        for (int i = 0; i < verticies.Length; i++)
        {
            verticies[i].y = trackGenerator.GetHeightFromMap(verticies[i].x + offsetX * size, verticies[i].z + offsetY * size);
        }

        for (int i = 0; i < verticies.Length; i += 3)
        {
            Vector3 a = verticies[i];
            Vector3 b = verticies[i + 1];
            Vector3 c = verticies[i + 2];

            Vector3 normal = Vector3.Cross(b - a, c - a);

            float an = Vector3.Dot(Vector3.up, normal.normalized);

            if (Mathf.Abs(an) > cliffAngle)
            {
                uvs[i] = new Vector2(grassUVColourPercent, 0.5f);
                uvs[i + 1] = new Vector2(grassUVColourPercent, 0.5f);
                uvs[i + 2] = new Vector2(grassUVColourPercent, 0.5f);
            } else
            {
                uvs[i] = new Vector2(cliffUVColourPercent, 0.5f);
                uvs[i + 1] = new Vector2(cliffUVColourPercent, 0.5f);
                uvs[i + 2] = new Vector2(cliffUVColourPercent, 0.5f);
            }

            if(a.y < trackGenerator.waterHeight || b.y < trackGenerator.waterHeight || c.y < trackGenerator.waterHeight)
            {
                uvs[i] = new Vector2(waterEdgeUVColourPercent, 0.5f);
                uvs[i + 1] = new Vector2(waterEdgeUVColourPercent, 0.5f);
                uvs[i + 2] = new Vector2(waterEdgeUVColourPercent, 0.5f);
            }
        }

        filter.mesh.vertices = verticies;
        filter.mesh.SetUVs(0, uvs);
        filter.mesh.RecalculateNormals();
        filter.mesh.RecalculateBounds();
        filter.mesh.Optimize();

        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshCollider>().sharedMesh = filter.mesh;
    }
}
