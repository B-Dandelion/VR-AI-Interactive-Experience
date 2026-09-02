using System.Collections;
using UnityEngine;

/// <summary>
/// Resizes generated portfolio portals using each legacy Portal2..Portal6 visual's
/// own world-space renderer bounds. This intentionally does NOT normalize against Portal1,
/// because the anatomical stages use dramatically different world scales.
/// </summary>
public class PortfolioPortalLegacySize : MonoBehaviour
{
    // The legacy fire rings were useful as spatial references but visually too dominant.
    // Keep the replacement magical-circle slightly smaller than the old footprint.
    private const float LegacyFootprintRatio = 0.72f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!Application.isPlaying) return;

        GameObject runner = new GameObject("[Portfolio Portal Legacy Size]");
        DontDestroyOnLoad(runner);
        runner.AddComponent<PortfolioPortalLegacySize>();
    }

    private IEnumerator Start()
    {
        // Wait until PortfolioPortalGuideBootstrap has created the replacement rings.
        yield return null;
        yield return null;

        int resized = 0;
        for (int portalIndex = 2; portalIndex <= 6; portalIndex++)
        {
            string legacyName = $"Portal{portalIndex}";
            Transform legacyPortal = FindSceneTransform(legacyName);
            Transform generatedPortal = FindSceneTransform($"[Portfolio Portal] {legacyName}");

            if (legacyPortal == null || generatedPortal == null)
            {
                Debug.LogWarning($"[PortfolioPortalLegacySize] Could not resolve {legacyName} or its generated replacement.");
                continue;
            }

            float legacyRadius = MeasureLegacyVisualRadius(legacyPortal);
            float generatedRadius = MeasureGeneratedMainRingRadius(generatedPortal);

            if (legacyRadius <= 0.001f || generatedRadius <= 0.001f)
            {
                Debug.LogWarning($"[PortfolioPortalLegacySize] Invalid radius for {legacyName}: legacy={legacyRadius:F3}, generated={generatedRadius:F3}");
                continue;
            }

            float targetRadius = legacyRadius * LegacyFootprintRatio;
            float multiplier = targetRadius / generatedRadius;
            generatedPortal.localScale = Vector3.one * multiplier;
            resized++;

            Debug.Log($"[PortfolioPortalLegacySize] {legacyName}: legacyRadius {legacyRadius:F2}, targetRadius {targetRadius:F2}, generatedRadius {generatedRadius:F2}, scale x{multiplier:F2}");
        }

        Debug.Log($"[PortfolioPortalLegacySize] resized {resized} internal portal(s) to {LegacyFootprintRatio:P0} of their legacy visual footprint.");
        Destroy(gameObject);
    }

    private static Transform FindSceneTransform(string exactName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform transform in transforms)
        {
            if (transform == null || !transform.gameObject.scene.IsValid()) continue;
            if (transform.name.Trim() == exactName) return transform;
        }
        return null;
    }

    private static float MeasureLegacyVisualRadius(Transform legacyPortal)
    {
        Renderer[] renderers = legacyPortal.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds combined = new Bounds(legacyPortal.position, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            Bounds bounds = renderer.bounds;
            if (bounds.size.sqrMagnitude <= 0.000001f) continue;

            if (!hasBounds)
            {
                combined = bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(bounds);
            }
        }

        if (hasBounds)
        {
            // Legacy portal particles form a ring; the largest world-space extent is a useful
            // approximation of the visual radius and preserves per-stage scale.
            return Mathf.Max(combined.extents.x, combined.extents.y, combined.extents.z);
        }

        Vector3 scale = legacyPortal.lossyScale;
        return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
    }

    private static float MeasureGeneratedMainRingRadius(Transform generatedPortal)
    {
        Transform ringTransform = generatedPortal.Find("Main Ring");
        if (ringTransform == null) return 0f;

        LineRenderer line = ringTransform.GetComponent<LineRenderer>();
        if (line == null || line.positionCount == 0) return 0f;

        return line.GetPosition(0).magnitude;
    }
}
