using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Portfolio-capture-only visual upgrade for teleport guides.
///
/// The real teleport colliders and XRTP logic are left untouched. At runtime this script:
/// 1) uses the scene's existing Portal1..Portal6 objects as the visual anchors,
/// 2) hides their old fire-style renderers,
/// 3) draws a lightweight layered amber portal in the exact legacy pose,
/// 4) replaces bright-green direction-arrow materials with a muted gold guide material.
///
/// No imported prefab/material/model asset is modified on disk.
/// </summary>
public static class PortfolioPortalGuideBootstrap
{
    private static readonly Color PortalGold = new Color(1.00f, 0.73f, 0.30f, 0.96f);
    private static readonly Color PortalPaleGold = new Color(1.00f, 0.90f, 0.62f, 0.78f);
    private static readonly Color PortalDeepAmber = new Color(1.00f, 0.45f, 0.12f, 0.72f);
    private static readonly Color ArrowGold = new Color(0.78f, 0.62f, 0.32f, 1.00f);

    private static Material arrowMaterial;

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

            string objectName = transform.name.Trim();

            if (IsLegacyPortalObjectName(objectName))
            {
                UpgradeLegacyPortal(transform, activeScene);
                portalCount++;
                continue;
            }

            if (objectName.ToLowerInvariant().Contains("3d rightarrow") &&
                !HasArrowNamedParent(transform))
            {
                arrowCount += StyleDirectionArrow(transform);
            }
        }

        Debug.Log($"[PortfolioPortalGuide] upgraded {portalCount} legacy portal visual(s), styled {arrowCount} arrow renderer(s).");
    }

    private static bool IsLegacyPortalObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return false;

        string trimmed = objectName.Trim();
        if (!trimmed.StartsWith("Portal", System.StringComparison.OrdinalIgnoreCase)) return false;

        string suffix = trimmed.Substring("Portal".Length);
        if (suffix.Length == 0) return false;

        foreach (char c in suffix)
        {
            if (!char.IsDigit(c)) return false;
        }

        return true;
    }

    private static void UpgradeLegacyPortal(Transform legacyPortal, Scene activeScene)
    {
        Renderer[] renderers = legacyPortal.GetComponentsInChildren<Renderer>(true);

        bool hasBounds = false;
        Bounds combinedBounds = new Bounds(legacyPortal.position, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        Vector3 worldCenter = hasBounds ? combinedBounds.center : legacyPortal.position;
        Quaternion worldRotation = legacyPortal.rotation;
        float radius = EstimatePortalRadius(legacyPortal, combinedBounds, hasBounds);

        // Hide only the legacy visual. Teleport triggers live on separate tp-pad objects.
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null) renderer.enabled = false;
        }

        CreatePortalVisual(
            activeScene,
            legacyPortal.name.Trim(),
            worldCenter,
            worldRotation,
            radius);
    }

    private static float EstimatePortalRadius(Transform legacyPortal, Bounds bounds, bool hasBounds)
    {
        if (hasBounds)
        {
            // The old portal is already positioned and sized correctly in the restored scene.
            // Use its renderer bounds only for scale, while preserving its own rotation for pose.
            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            if (maxExtent > 0.05f)
                return Mathf.Clamp(maxExtent * 0.94f, 0.52f, 1.55f);
        }

        Vector3 scale = legacyPortal.lossyScale;
        float fallback = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return Mathf.Clamp(fallback * 0.72f, 0.52f, 1.25f);
    }

    private static void CreatePortalVisual(
        Scene activeScene,
        string legacyName,
        Vector3 worldCenter,
        Quaternion worldRotation,
        float radius)
    {
        GameObject root = new GameObject($"[Portfolio Portal] {legacyName}");
        SceneManager.MoveGameObjectToScene(root, activeScene);
        root.transform.position = worldCenter;
        root.transform.rotation = worldRotation;
        root.transform.localScale = Vector3.one;

        Material lineMaterial = CreateLineMaterial();

        LineRenderer mainRing = CreateArc(
            root.transform, "Main Ring", radius,
            0f, 360f, 112, 0.024f,
            PortalGold, lineMaterial, true);

        // A quiet inner ring gives the portal a designed UI-like structure instead of
        // reading as a single fire hoop.
        LineRenderer innerRing = CreateArc(
            root.transform, "Inner Ring", radius * 0.82f,
            0f, 360f, 104, 0.008f,
            PortalPaleGold, lineMaterial, true);

        Transform outerArcRoot = CreateRotatingLayer(root.transform, "Outer Broken Ring", +15f);
        LineRenderer outerArc = CreateArc(
            outerArcRoot, "Arc", radius * 1.08f,
            14f, 298f, 88, 0.012f,
            PortalPaleGold, lineMaterial, false);

        Transform innerArcRoot = CreateRotatingLayer(root.transform, "Inner Broken Ring", -22f);
        LineRenderer innerArc = CreateArc(
            innerArcRoot, "Arc", radius * 0.91f,
            194f, 482f, 82, 0.010f,
            PortalDeepAmber, lineMaterial, false);

        // Short rotating arc segments create a restrained mystical-circle silhouette.
        Transform glyphRoot = CreateRotatingLayer(root.transform, "Glyph Arcs", +8f);
        LineRenderer[] glyphArcs = new LineRenderer[8];
        for (int i = 0; i < glyphArcs.Length; i++)
        {
            float start = i * 45f + 7f;
            float span = (i % 2 == 0) ? 15f : 9f;
            glyphArcs[i] = CreateArc(
                glyphRoot,
                $"Glyph {i + 1}",
                radius * 1.16f,
                start,
                start + span,
                8,
                0.008f,
                i % 2 == 0 ? PortalPaleGold : PortalDeepAmber,
                lineMaterial,
                false);
        }

        LineRenderer[] secondary = new LineRenderer[4 + glyphArcs.Length];
        secondary[0] = innerRing;
        secondary[1] = outerArc;
        secondary[2] = innerArc;
        secondary[3] = mainRing;
        for (int i = 0; i < glyphArcs.Length; i++)
            secondary[4 + i] = glyphArcs[i];

        PortfolioPortalAnimator animator = root.AddComponent<PortfolioPortalAnimator>();
        animator.Configure(mainRing, secondary);
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
        line.numCapVertices = 5;
        line.numCornerVertices = 5;
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
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        Material material = new Material(shader);
        material.name = "Portfolio Portal Line Material";

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        return material;
    }

    private static bool HasArrowNamedParent(Transform transform)
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.name.ToLowerInvariant().Contains("3d rightarrow")) return true;
            parent = parent.parent;
        }
        return false;
    }

    private static int StyleDirectionArrow(Transform arrowRoot)
    {
        int styled = 0;
        Renderer[] renderers = arrowRoot.GetComponentsInChildren<Renderer>(true);
        Material solidArrowMaterial = GetArrowMaterial();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            Material[] replacements = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
            for (int i = 0; i < replacements.Length; i++)
                replacements[i] = solidArrowMaterial;

            renderer.materials = replacements;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            styled++;
        }

        // These arrows are navigation cues, not hero props. Make them less dominant.
        arrowRoot.localScale *= 0.72f;
        return styled;
    }

    private static Material GetArrowMaterial()
    {
        if (arrowMaterial != null) return arrowMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

        arrowMaterial = new Material(shader);
        arrowMaterial.name = "Portfolio Direction Arrow";

        if (arrowMaterial.HasProperty("_BaseColor")) arrowMaterial.SetColor("_BaseColor", ArrowGold);
        if (arrowMaterial.HasProperty("_Color")) arrowMaterial.SetColor("_Color", ArrowGold);
        if (arrowMaterial.HasProperty("_Metallic")) arrowMaterial.SetFloat("_Metallic", 0f);
        if (arrowMaterial.HasProperty("_Smoothness")) arrowMaterial.SetFloat("_Smoothness", 0.16f);
        if (arrowMaterial.HasProperty("_Glossiness")) arrowMaterial.SetFloat("_Glossiness", 0.16f);
        if (arrowMaterial.HasProperty("_EnvironmentReflections")) arrowMaterial.SetFloat("_EnvironmentReflections", 0f);

        arrowMaterial.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        return arrowMaterial;
    }
}

/// <summary>
/// Adds a subtle breathing pulse to generated portal lines.
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
        mainBaseWidth = main != null ? main.startWidth : 0.024f;

        secondaryBaseWidths = new float[secondaryLines != null ? secondaryLines.Length : 0];
        for (int i = 0; i < secondaryBaseWidths.Length; i++)
            secondaryBaseWidths[i] = secondaryLines[i] != null ? secondaryLines[i].startWidth : 0.010f;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * 2.0f) * 0.08f;

        if (mainRing != null)
        {
            float width = mainBaseWidth * pulse;
            mainRing.startWidth = width;
            mainRing.endWidth = width;
        }

        if (secondaryLines == null || secondaryBaseWidths == null) return;

        for (int i = 0; i < secondaryLines.Length && i < secondaryBaseWidths.Length; i++)
        {
            LineRenderer line = secondaryLines[i];
            if (line == null || line == mainRing) continue;

            float width = secondaryBaseWidths[i] * (1f + Mathf.Sin(Time.time * 1.55f + i * 0.37f) * 0.05f);
            line.startWidth = width;
            line.endWidth = width;
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
