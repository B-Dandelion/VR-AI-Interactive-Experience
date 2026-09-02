using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Portfolio-capture-only visual upgrade for teleport guides.
///
/// The real teleport colliders and XRTP logic are left untouched. At runtime this script:
/// 1) hides the old portal renderers below each "tp pad" object,
/// 2) draws a lightweight amber portal made from animated LineRenderers,
/// 3) tones down the bright-green 3D direction arrows.
///
/// No imported prefab/material/model asset is modified on disk.
/// </summary>
public static class PortfolioPortalGuideBootstrap
{
    private static readonly Color PortalGold = new Color(1.00f, 0.72f, 0.28f, 0.92f);
    private static readonly Color PortalPaleGold = new Color(1.00f, 0.88f, 0.56f, 0.72f);
    private static readonly Color PortalDeepAmber = new Color(1.00f, 0.43f, 0.12f, 0.68f);
    private static readonly Color ArrowGold = new Color(0.86f, 0.69f, 0.38f, 1.00f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        if (!Application.isPlaying) return;

        Scene activeScene = SceneManager.GetActiveScene();
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        int portalCount = 0;
        int arrowCount = 0;

        foreach (Transform transform in transforms)
        {
            if (transform == null || !transform.gameObject.scene.IsValid()) continue;
            if (transform.gameObject.scene != activeScene) continue;

            string objectName = transform.name.Trim().ToLowerInvariant();

            if (objectName.StartsWith("tp pad"))
            {
                HideLegacyPortalRenderers(transform);
                CreatePortalVisual(transform, activeScene);
                portalCount++;
            }

            if (objectName.Contains("3d rightarrow"))
            {
                arrowCount += StyleDirectionArrow(transform);
            }
        }

        Debug.Log($"[PortfolioPortalGuide] upgraded {portalCount} teleport guide(s), styled {arrowCount} arrow renderer(s).");
    }

    private static void HideLegacyPortalRenderers(Transform tpPad)
    {
        Renderer[] renderers = tpPad.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            renderer.enabled = false;
        }
    }

    private static void CreatePortalVisual(Transform tpPad, Scene activeScene)
    {
        ResolvePortalPose(tpPad, out Vector3 worldCenter, out Quaternion worldRotation, out float radius);

        // Deliberately keep the visual independent from the tp-pad hierarchy.
        // Several restored trigger objects use non-uniform scale, which would otherwise
        // stretch a generated circular portal into an ellipse.
        GameObject root = new GameObject($"[Portfolio Portal] {tpPad.name.Trim()}");
        SceneManager.MoveGameObjectToScene(root, activeScene);
        root.transform.position = worldCenter;
        root.transform.rotation = worldRotation;
        root.transform.localScale = Vector3.one;

        Material lineMaterial = CreateLineMaterial();

        LineRenderer mainRing = CreateArc(
            root.transform,
            "Main Ring",
            radius,
            0f,
            360f,
            96,
            0.026f,
            PortalGold,
            lineMaterial,
            true);

        Transform outerArcRoot = CreateRotatingLayer(root.transform, "Outer Arc", +18f);
        LineRenderer outerArc = CreateArc(
            outerArcRoot,
            "Arc",
            radius * 1.09f,
            18f,
            286f,
            76,
            0.014f,
            PortalPaleGold,
            lineMaterial,
            false);

        Transform innerArcRoot = CreateRotatingLayer(root.transform, "Inner Arc", -27f);
        LineRenderer innerArc = CreateArc(
            innerArcRoot,
            "Arc",
            radius * 0.88f,
            206f,
            494f,
            70,
            0.012f,
            PortalDeepAmber,
            lineMaterial,
            false);

        // Small partial accent arcs create the broken, layered magical-circle silhouette
        // without relying on external textures or a heavy particle asset.
        Transform accentRoot = CreateRotatingLayer(root.transform, "Accent Arcs", +9f);
        LineRenderer accentA = CreateArc(
            accentRoot,
            "Accent A",
            radius * 1.17f,
            42f,
            108f,
            22,
            0.009f,
            PortalPaleGold,
            lineMaterial,
            false);
        LineRenderer accentB = CreateArc(
            accentRoot,
            "Accent B",
            radius * 1.17f,
            222f,
            288f,
            22,
            0.009f,
            PortalPaleGold,
            lineMaterial,
            false);

        PortfolioPortalAnimator animator = root.AddComponent<PortfolioPortalAnimator>();
        animator.Configure(mainRing, new[] { outerArc, innerArc, accentA, accentB });
    }

    private static Transform CreateRotatingLayer(Transform parent, string name, float speed)
    {
        GameObject layer = new GameObject(name);
        layer.transform.SetParent(parent, false);

        PortfolioPortalSpin spin = layer.AddComponent<PortfolioPortalSpin>();
        spin.degreesPerSecond = speed;
        return layer.transform;
    }

    private static LineRenderer CreateArc(
        Transform parent,
        string name,
        float radius,
        float startDegrees,
        float endDegrees,
        int segments,
        float width,
        Color color,
        Material sharedMaterial,
        bool closeLoop)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = closeLoop;
        line.alignment = LineAlignment.TransformZ;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = new Material(sharedMaterial);
        line.startColor = color;
        line.endColor = color;
        line.startWidth = width;
        line.endWidth = width;

        int pointCount = closeLoop ? segments : segments + 1;
        line.positionCount = pointCount;

        float span = endDegrees - startDegrees;
        for (int i = 0; i < pointCount; i++)
        {
            float denominator = Mathf.Max(1, segments);
            float t = i / denominator;
            float angle = (startDegrees + span * t) * Mathf.Deg2Rad;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }

        return line;
    }

    private static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogError("[PortfolioPortalGuide] No compatible unlit shader found; portal guide was not created.");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        Material material = new Material(shader);
        material.name = "Portfolio Portal Line Material";

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        return material;
    }

    private static void ResolvePortalPose(Transform tpPad, out Vector3 center, out Quaternion rotation, out float radius)
    {
        Collider collider = tpPad.GetComponent<Collider>();
        if (collider == null) collider = tpPad.GetComponentInChildren<Collider>(true);

        if (collider is BoxCollider box)
        {
            Transform t = box.transform;
            center = t.TransformPoint(box.center);

            Vector3 lossy = t.lossyScale;
            float sx = Mathf.Abs(box.size.x * lossy.x);
            float sy = Mathf.Abs(box.size.y * lossy.y);
            float sz = Mathf.Abs(box.size.z * lossy.z);

            // The thinnest trigger dimension is treated as the portal normal.
            // Our generated circle lives in local XY (normal +Z), so rotate +Z
            // onto the collider's thin local axis.
            if (sx <= sy && sx <= sz)
            {
                rotation = t.rotation * Quaternion.Euler(0f, 90f, 0f); // +Z -> +X
                radius = Mathf.Clamp(Mathf.Min(sy, sz) * 0.43f, 0.48f, 1.15f);
            }
            else if (sy <= sx && sy <= sz)
            {
                rotation = t.rotation * Quaternion.Euler(-90f, 0f, 0f); // +Z -> +Y
                radius = Mathf.Clamp(Mathf.Min(sx, sz) * 0.43f, 0.48f, 1.15f);
            }
            else
            {
                rotation = t.rotation;
                radius = Mathf.Clamp(Mathf.Min(sx, sy) * 0.43f, 0.48f, 1.15f);
            }

            return;
        }

        if (collider != null)
        {
            center = collider.bounds.center;
            rotation = collider.transform.rotation;
            Vector3 extents = collider.bounds.extents;
            radius = Mathf.Clamp(Mathf.Max(extents.x, extents.y) * 0.72f, 0.48f, 1.15f);
            return;
        }

        center = tpPad.position;
        rotation = tpPad.rotation;
        radius = 0.72f;
    }

    private static int StyleDirectionArrow(Transform arrowRoot)
    {
        int styled = 0;
        Renderer[] renderers = arrowRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                if (material == null) continue;

                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", ArrowGold);
                if (material.HasProperty("_Color")) material.SetColor("_Color", ArrowGold);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.14f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.14f);

                // Keep it readable but stop it from looking like a neon-green debug prop.
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", ArrowGold * 0.18f);
                    material.DisableKeyword("_EMISSION");
                }
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            styled++;
        }

        return styled;
    }
}

/// <summary>
/// Adds a very small pulse to the generated portal lines.
/// </summary>
public class PortfolioPortalAnimator : MonoBehaviour
{
    private LineRenderer mainRing;
    private LineRenderer[] secondaryLines;
    private float mainBaseWidth;
    private float[] secondaryBaseWidths;

    public void Configure(LineRenderer main, LineRenderer[] secondary)
    {
        mainRing = main;
        secondaryLines = secondary;
        mainBaseWidth = main != null ? main.startWidth : 0.026f;

        secondaryBaseWidths = new float[secondaryLines != null ? secondaryLines.Length : 0];
        for (int i = 0; i < secondaryBaseWidths.Length; i++)
            secondaryBaseWidths[i] = secondaryLines[i] != null ? secondaryLines[i].startWidth : 0.012f;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * 2.15f) * 0.11f;

        if (mainRing != null)
        {
            float width = mainBaseWidth * pulse;
            mainRing.startWidth = width;
            mainRing.endWidth = width;
        }

        if (secondaryLines == null || secondaryBaseWidths == null) return;

        for (int i = 0; i < secondaryLines.Length && i < secondaryBaseWidths.Length; i++)
        {
            if (secondaryLines[i] == null) continue;
            float width = secondaryBaseWidths[i] * (2f - pulse * 0.92f);
            secondaryLines[i].startWidth = width;
            secondaryLines[i].endWidth = width;
        }
    }
}

/// <summary>
/// Simple local-Z rotation used by the broken portal arcs.
/// </summary>
public class PortfolioPortalSpin : MonoBehaviour
{
    public float degreesPerSecond = 18f;

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
