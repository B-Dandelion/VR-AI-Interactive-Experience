using System.Collections;
using UnityEngine;

/// <summary>
/// Normalizes the runtime-generated portfolio portal sizes after the portal bootstrap runs.
/// Portal1 (the start-area portal) is kept as the visual reference.
/// Portal2..Portal6 are forced to a clearly visible size based on Portal1, regardless of how
/// tiny their restored legacy particle bounds were.
/// </summary>
public class PortfolioPortalScaleNormalizer : MonoBehaviour
{
    // Internal portals should read as obvious traversal gates, not small markers.
    private const float InternalPortalToStartRatio = 1.45f;

    // Some restored Portal2..6 particle objects have extremely tiny renderer bounds.
    // The previous x12 cap was still far too small, so allow a much larger correction.
    private const float MinScaleMultiplier = 1f;
    private const float MaxScaleMultiplier = 100f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!Application.isPlaying) return;

        GameObject runner = new GameObject("[Portfolio Portal Scale Normalizer]");
        DontDestroyOnLoad(runner);
        runner.AddComponent<PortfolioPortalScaleNormalizer>();
    }

    private IEnumerator Start()
    {
        // Wait two frames so the portal bootstrap has definitely created all generated rings.
        yield return null;
        yield return null;

        Transform startPortal = FindGeneratedPortal("Portal1");
        if (startPortal == null)
        {
            Debug.LogWarning("[PortfolioPortalScale] Portal1 reference was not found; skipping portal size normalization.");
            Destroy(gameObject);
            yield break;
        }

        float startRadius = GetMainRingRadius(startPortal);
        if (startRadius <= 0.001f)
        {
            Debug.LogWarning("[PortfolioPortalScale] Could not read Portal1 radius; skipping portal size normalization.");
            Destroy(gameObject);
            yield break;
        }

        float targetInternalRadius = startRadius * InternalPortalToStartRatio;
        int resized = 0;

        for (int portalIndex = 2; portalIndex <= 6; portalIndex++)
        {
            Transform portal = FindGeneratedPortal($"Portal{portalIndex}");
            if (portal == null) continue;

            float currentRadius = GetMainRingRadius(portal);
            if (currentRadius <= 0.001f) continue;

            float multiplier = targetInternalRadius / currentRadius;
            multiplier = Mathf.Clamp(multiplier, MinScaleMultiplier, MaxScaleMultiplier);

            // Generated portal roots always start at scale 1, so this directly maps the
            // visible ring to the target world-space size without touching teleport logic.
            portal.localScale = Vector3.one * multiplier;
            resized++;

            Debug.Log(
                $"[PortfolioPortalScale] Portal{portalIndex}: radius {currentRadius:F3} -> " +
                $"target {targetInternalRadius:F3}, scale x{multiplier:F2}");
        }

        Debug.Log(
            $"[PortfolioPortalScale] normalized {resized} internal portal(s) to " +
            $"{InternalPortalToStartRatio:P0} of Portal1 size.");

        Destroy(gameObject);
    }

    private static Transform FindGeneratedPortal(string legacyName)
    {
        string expected = $"[Portfolio Portal] {legacyName}";
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform transform in transforms)
        {
            if (transform != null && transform.name == expected && transform.gameObject.scene.IsValid())
                return transform;
        }

        return null;
    }

    private static float GetMainRingRadius(Transform portalRoot)
    {
        Transform mainRingTransform = portalRoot.Find("Main Ring");
        if (mainRingTransform == null) return 0f;

        LineRenderer line = mainRingTransform.GetComponent<LineRenderer>();
        if (line == null || line.positionCount == 0) return 0f;

        // Main Ring points are generated in local XY around the origin.
        return line.GetPosition(0).magnitude;
    }
}
