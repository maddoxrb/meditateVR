using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class LowPolyTerrain : MonoBehaviour
{
    [Header("Grid")]
    public int xVerts = 201;          // resolution (>=2)
    public int zVerts = 201;
    public float cellSize = 1f;       // world units between verts

    [Header("Height")]
    public float height = 12f;        // max hill height
    public float noiseScale = 0.06f;  // larger = smoother hills
    public int octaves = 3;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int seed = 0;
    public Vector2 offset;            // slide noise

    [Header("UVs")] public float uvScale = 0.1f; // texture tiling control

    Mesh _mesh;
    Vector3[] _baseVerts;             // smooth source before flatting

    void OnEnable() { Build(); }
    void OnValidate() { xVerts = Mathf.Max(2, xVerts); zVerts = Mathf.Max(2, zVerts); cellSize = Mathf.Max(0.01f, cellSize); Build(); }

    void Build()
    {
        var mf = GetComponent<MeshFilter>();
        if (_mesh == null)
        {
            _mesh = new Mesh { name = "LowPolyTerrain" };
            _mesh.indexFormat = IndexFormat.UInt32; // allow >65k vertices for flat shading
            mf.sharedMesh = _mesh;
        }
        GenerateHeights();
        ApplyFlatShading(); // duplicates verts per triangle for crisp low-poly look
        GetComponent<MeshCollider>().sharedMesh = _mesh;
    }

    void GenerateHeights()
    {
        // build smooth grid vertices first
        _baseVerts = new Vector3[xVerts * zVerts];
        var prng = new System.Random(seed);
        var octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
            octaveOffsets[i] = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));

        float ox = (xVerts - 1) * cellSize * 0.5f;
        float oz = (zVerts - 1) * cellSize * 0.5f;

        for (int z = 0; z < zVerts; z++)
            for (int x = 0; x < xVerts; x++)
            {
                float amp = 1f, freq = 1f, n = 0f, norm = 0f;
                for (int o = 0; o < octaves; o++)
                {
                    float sx = (x + offset.x + octaveOffsets[o].x) * noiseScale * freq;
                    float sz = (z + offset.y + octaveOffsets[o].y) * noiseScale * freq;
                    n += (Mathf.PerlinNoise(sx, sz) * 2f - 1f) * amp;
                    norm += amp;
                    amp *= persistence;
                    freq *= lacunarity;
                }
                n /= Mathf.Max(0.0001f, norm);
                float y = n * height;
                _baseVerts[z * xVerts + x] = new Vector3(x * cellSize - ox, y, z * cellSize - oz);
            }

        // index buffer for smooth grid (used briefly)
        int quadsX = xVerts - 1, quadsZ = zVerts - 1;
        int[] tris = new int[quadsX * quadsZ * 6];
        int t = 0;
        for (int z = 0; z < quadsZ; z++)
            for (int x = 0; x < quadsX; x++)
            {
                int i0 = z * xVerts + x;
                int i1 = i0 + 1;
                int i2 = i0 + xVerts;
                int i3 = i2 + 1;

                tris[t++] = i0; tris[t++] = i2; tris[t++] = i1; // tri 1
                tris[t++] = i1; tris[t++] = i2; tris[t++] = i3; // tri 2
            }

        _mesh.Clear();
        _mesh.vertices = _baseVerts;
        _mesh.triangles = tris;
        _mesh.RecalculateBounds();
    }

    void ApplyFlatShading()
    {
        // duplicate vertices so each triangle has unique normals
        var tri = _mesh.triangles;
        var verts = _mesh.vertices;

        Vector3[] flatVerts = new Vector3[tri.Length];
        Vector3[] flatNormals = new Vector3[tri.Length];
        Vector2[] flatUV = new Vector2[tri.Length];

        for (int i = 0; i < tri.Length; i += 3)
        {
            Vector3 v0 = verts[tri[i]];
            Vector3 v1 = verts[tri[i + 1]];
            Vector3 v2 = verts[tri[i + 2]];
            Vector3 n = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            flatVerts[i] = v0; flatVerts[i + 1] = v1; flatVerts[i + 2] = v2;
            flatNormals[i] = n; flatNormals[i + 1] = n; flatNormals[i + 2] = n;

            // simple planar UVs
            flatUV[i]     = new Vector2(v0.x, v0.z) * uvScale;
            flatUV[i + 1] = new Vector2(v1.x, v1.z) * uvScale;
            flatUV[i + 2] = new Vector2(v2.x, v2.z) * uvScale;
            tri[i] = i; tri[i + 1] = i + 1; tri[i + 2] = i + 2;
        }

        _mesh.vertices = flatVerts;
        _mesh.triangles = tri;
        _mesh.normals = flatNormals;
        _mesh.uv = flatUV;
        _mesh.RecalculateTangents();
        _mesh.RecalculateBounds();
    }
}