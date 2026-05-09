using UnityEngine;

/// <summary>
/// Runtime visual for enemy vision using a generated 3D cone mesh.
/// Visual-only: does not change enemy behavior.
/// </summary>
[RequireComponent(typeof(EnemyLineOfSight))]
public class EnemyVisionConeMesh : MonoBehaviour
{
    [Header("Rendering")]
    [Range(6, 64)]
    public int radialSegments = 20;

    public Color cannotSeeColor = new Color(0f, 1f, 0f, 0.2f);
    public Color canSeeColor = new Color(1f, 0f, 0f, 0.25f);

    private EnemyLineOfSight lineOfSight;
    private GameObject coneVisualObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material runtimeMaterial;
    private Mesh coneMesh;

    private float lastRange;
    private float lastAngle;
    private int lastSegments;

    private void Awake()
    {
        lineOfSight = GetComponent<EnemyLineOfSight>();
        EnsureConeVisualObject();
        SetupMaterial();
        RebuildMesh();
    }

    private void LateUpdate()
    {
        if (lineOfSight == null)
        {
            return;
        }

        if (coneVisualObject == null)
        {
            EnsureConeVisualObject();
            SetupMaterial();
            RebuildMesh();
        }

        coneVisualObject.transform.localPosition = Vector3.up * lineOfSight.eyeHeight;
        coneVisualObject.transform.localRotation = Quaternion.identity;

        if (NeedsMeshRebuild())
        {
            RebuildMesh();
        }

        if (runtimeMaterial != null)
        {
            runtimeMaterial.color = lineOfSight.CanSeePlayer ? canSeeColor : cannotSeeColor;
        }
    }

    private void EnsureConeVisualObject()
    {
        Transform existing = transform.Find("VisionConeVisual");

        if (existing != null)
        {
            coneVisualObject = existing.gameObject;
        }
        else
        {
            coneVisualObject = new GameObject("VisionConeVisual");
            coneVisualObject.transform.SetParent(transform, false);
        }

        meshFilter = coneVisualObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = coneVisualObject.AddComponent<MeshFilter>();
        }

        meshRenderer = coneVisualObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = coneVisualObject.AddComponent<MeshRenderer>();
        }
    }

    private bool NeedsMeshRebuild()
    {
        return !Mathf.Approximately(lastRange, lineOfSight.visionRange)
            || !Mathf.Approximately(lastAngle, lineOfSight.visionAngle)
            || lastSegments != radialSegments;
    }

    private void RebuildMesh()
    {
        if (lineOfSight == null || meshFilter == null)
        {
            return;
        }

        if (coneMesh != null)
        {
            Destroy(coneMesh);
        }

        coneMesh = CreateConeMesh(lineOfSight.visionRange, lineOfSight.visionAngle, Mathf.Max(6, radialSegments));
        coneMesh.name = "EnemyVisionConeMesh";
        meshFilter.sharedMesh = coneMesh;

        lastRange = lineOfSight.visionRange;
        lastAngle = lineOfSight.visionAngle;
        lastSegments = radialSegments;
    }

    private void SetupMaterial()
    {
        if (meshRenderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        runtimeMaterial = new Material(shader);
        runtimeMaterial.color = cannotSeeColor;

        runtimeMaterial.SetFloat("_Surface", 1f);
        runtimeMaterial.SetFloat("_Blend", 0f);
        runtimeMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        runtimeMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        runtimeMaterial.SetFloat("_ZWrite", 0f);
        runtimeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        runtimeMaterial.renderQueue = 3000;

        meshRenderer.sharedMaterial = runtimeMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private static Mesh CreateConeMesh(float range, float angleDegrees, int segments)
    {
        Mesh mesh = new Mesh();

        float halfAngleRad = (angleDegrees * 0.5f) * Mathf.Deg2Rad;
        float radius = Mathf.Tan(halfAngleRad) * range;

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float theta = t * Mathf.PI * 2f;
            float x = Mathf.Cos(theta) * radius;
            float y = Mathf.Sin(theta) * radius;
            vertices[i + 1] = new Vector3(x, y, range);
        }

        int triIndex = 0;

        for (int i = 1; i <= segments; i++)
        {
            int next = i + 1;

            triangles[triIndex++] = 0;
            triangles[triIndex++] = i;
            triangles[triIndex++] = next;

            triangles[triIndex++] = i;
            triangles[triIndex++] = 0;
            triangles[triIndex++] = next;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void OnDestroy()
    {
        if (coneMesh != null)
        {
            Destroy(coneMesh);
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
