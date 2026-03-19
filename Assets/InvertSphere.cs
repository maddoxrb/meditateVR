using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class InteriorSphereSetup : MonoBehaviour
{
    [Header("Material culling mode")]
    public bool renderBothSides = false;   // safest fallback
    public bool disableShadows = true;

    void Awake()
    {
        MakeMeshFaceInward();
        ConfigureMaterial();
        ConfigureRenderer();
    }

    void MakeMeshFaceInward()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null)
        {
            Debug.LogError("No mesh found on MeshFilter.");
            return;
        }

        // Create a unique runtime copy
        Mesh mesh = Instantiate(mf.sharedMesh);
        mesh.name = mf.sharedMesh.name + "_InteriorCopy";
        mf.mesh = mesh;

        // Reverse triangle winding on every submesh
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            int[] tris = mesh.GetTriangles(s);
            for (int i = 0; i < tris.Length; i += 3)
            {
                int temp = tris[i];
                tris[i] = tris[i + 1];
                tris[i + 1] = temp;
            }
            mesh.SetTriangles(tris, s);
        }

        // Rebuild normals, then invert them
        mesh.RecalculateNormals();
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        mesh.normals = normals;

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
    }

    void ConfigureMaterial()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            Debug.LogWarning("No material found on MeshRenderer.");
            return;
        }

        // Create a unique runtime material instance
        Material mat = new Material(mr.sharedMaterial);
        mat.name = mr.sharedMaterial.name + "_InteriorInstance";
        mr.material = mat;

        // Try common culling property names used by Unity shaders/pipelines
        // Off = show both sides, Front = show inside of non-inverted meshes
        int cullValue = renderBothSides ? (int)CullMode.Off : (int)CullMode.Back;

        if (mat.HasProperty("_Cull"))
            mat.SetInt("_Cull", cullValue);

        if (mat.HasProperty("_CullMode"))
            mat.SetInt("_CullMode", cullValue);

        if (mat.HasProperty("_RenderFace"))
        {
            // URP commonly uses:
            // 0 = Front, 1 = Back, 2 = Both
            mat.SetFloat("_RenderFace", renderBothSides ? 2f : 1f);
        }
    }

    void ConfigureRenderer()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();

        if (disableShadows)
        {
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        // Optional for large interior domes
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }
}