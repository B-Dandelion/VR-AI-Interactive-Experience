using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Portfolio-capture navigation polish.
/// Reuses the restored arrow objects only as direction/placement anchors,
/// hides their meshes, and replaces them with a restrained animated light trail.
/// </summary>
public class PortfolioLightGuide : MonoBehaviour
{
    private static readonly Color GuideGold = new Color(1.00f, 0.78f, 0.38f, 1.00f);
    private static readonly Color GuidePale = new Color(1.00f, 0.90f, 0.62f, 1.00f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!Application.isPlaying) return;
        GameObject runner = new GameObject("[Portfolio Light Guide Bootstrap]");
        DontDestroyOnLoad(runner);
        runner.AddComponent<PortfolioLightGuide>();
    }

    private IEnumerator Start()
    {
        // Let the restored scene and other portfolio polish scripts finish first.
        yield return null;
        yield return null;
        yield return null;

        Scene activeScene = SceneManager.GetActiveScene();
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        int replaced = 0;

        foreach (Transform transform in transforms)
        {
            if (transform == null || !transform.gameObject.scene.IsValid()) continue;
            if (transform.gameObject.scene != activeScene) continue;

            string lower = transform.name.ToLowerInvariant();
            if (!lower.Contains("3d rightarrow")) continue;
            if (HasArrowNamedParent(transform)) continue;

            if (CreateGuideFromArrow(transform, activeScene))
                replaced++;
        }

        Debug.Log($"[PortfolioLightGuide] replaced {replaced} 3D arrow guide(s) with animated light trails.");
        Destroy(gameObject);
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

    private static bool CreateGuideFromArrow(Transform arrowRoot, Scene activeScene)
    {
        Renderer[] renderers = arrowRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return false;

        bool hasBounds = false;
        Bounds combined = new Bounds(arrowRoot.position, Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            if (!hasBounds)
            {
                combined = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(renderer.bounds);
            }
        }
        if (!hasBounds) return false;

        float maxDimension = Mathf.Max(combined.size.x, combined.size.y, combined.size.z);
        float length = Mathf.Max(0.55f, maxDimension * 0.86f);
        float beadSize = Mathf.Clamp(length * 0.055f, 0.035f, length * 0.11f);

        // Disable the old mesh and its old pulse behaviour. We keep transforms intact
        // because their rotations encode the intended navigation direction.
        foreach (Renderer renderer in renderers)
            if (renderer != null) renderer.enabled = false;

        ArrowPulse[] pulses = arrowRoot.GetComponentsInChildren<ArrowPulse>(true);
        foreach (ArrowPulse pulse in pulses)
            if (pulse != null) pulse.enabled = false;

        GameObject root = new GameObject($"[Portfolio Light Guide] {arrowRoot.name}");
        SceneManager.MoveGameObjectToScene(root, activeScene);
        root.transform.position = combined.center;
        root.transform.rotation = arrowRoot.rotation;
        root.transform.localScale = Vector3.one;

        Material beadMaterial = CreateUnlitMaterial("Portfolio Guide Bead", GuideGold);
        Material lineMaterial = CreateUnlitMaterial("Portfolio Guide Line", GuidePale);

        // A subtle baseline keeps the path legible while the moving beads provide direction.
        GameObject lineObject = new GameObject("Guide Line");
        lineObject.transform.SetParent(root.transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(-length * 0.48f, 0f, 0f));
        line.SetPosition(1, new Vector3(length * 0.48f, 0f, 0f));
        line.startWidth = beadSize * 0.18f;
        line.endWidth = beadSize * 0.18f;
        line.startColor = new Color(GuidePale.r, GuidePale.g, GuidePale.b, 0.28f);
        line.endColor = new Color(GuidePale.r, GuidePale.g, GuidePale.b, 0.06f);
        line.material = lineMaterial;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        const int beadCount = 5;
        Transform[] beads = new Transform[beadCount];
        for (int i = 0; i < beadCount; i++)
        {
            GameObject bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bead.name = $"Flow Light {i + 1}";
            bead.transform.SetParent(root.transform, false);
            bead.transform.localScale = Vector3.one * beadSize;

            Collider collider = bead.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            Renderer renderer = bead.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(beadMaterial);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            beads[i] = bead.transform;
        }

        PortfolioLightGuideAnimator animator = root.AddComponent<PortfolioLightGuideAnimator>();
        animator.Configure(beads, length, beadSize);
        return true;
    }

    private static Material CreateUnlitMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");

        Material material = new Material(shader);
        material.name = name;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        return material;
    }
}

public class PortfolioLightGuideAnimator : MonoBehaviour
{
    private Transform[] beads;
    private float length;
    private float baseSize;

    public void Configure(Transform[] flowBeads, float guideLength, float beadSize)
    {
        beads = flowBeads;
        length = guideLength;
        baseSize = beadSize;
    }

    private void Update()
    {
        if (beads == null || beads.Length == 0 || length <= 0f) return;

        float travelSpeed = 0.34f;
        for (int i = 0; i < beads.Length; i++)
        {
            Transform bead = beads[i];
            if (bead == null) continue;

            float phase = Mathf.Repeat(Time.time * travelSpeed + i / (float)beads.Length, 1f);
            float x = Mathf.Lerp(-length * 0.43f, length * 0.43f, phase);

            // A tiny lateral wave makes the light feel alive without becoming decorative noise.
            float wave = Mathf.Sin((phase + i * 0.17f) * Mathf.PI * 2f) * length * 0.012f;
            bead.localPosition = new Vector3(x, wave, 0f);

            float fade = Mathf.Sin(Mathf.Clamp01(phase) * Mathf.PI);
            float scale = baseSize * Mathf.Lerp(0.55f, 1.20f, fade);
            bead.localScale = Vector3.one * scale;
        }
    }
}
