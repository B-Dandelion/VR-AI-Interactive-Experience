using System.Collections;
using UnityEngine;

/// <summary>
/// Normalizes the runtime-generated portfolio portal sizes after the portal bootstrap runs.
/// Portal1 (the start-area portal) is kept as the visual reference.
/// Portal2..Portal6 are enlarged to 125% of Portal1's radius so they remain clearly visible
/// inside large anatomical environments where the restored legacy particle bounds are tiny.
/// </summary>
public class PortfolioPortalScaleNormalizer : MonoBehaviour
{
    private const float InternalPortalToStartRatio = 1.25f;
    private const float MinScaleMultiplier = 1f;
    private const float MaxScaleMultiplier = 12f;

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
        // Wait one frame so PortfolioPortalGuideBootstrap has finished creating all rings.
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

            float multiplier = Mathf.Clamp(targetInternalRadius / currentRadius, MinScaleMultiplier, MaxScaleMultiplier);
            portal.localScale = Vector3.one * multiplier;
            resized++;

            Debug.Log($"[PortfolioPortalScale] Portal{portalIndex}: radius {currentRadius:F2} -> target {targetInternalRadius:F2}, scale x{multiplier:F2}");
        }

        Debug.Log($"[PortfolioPortalScale] normalized {resized} internal portal(s) to {InternalPortalToStartRatio:P0} of Portal1 size.");
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
