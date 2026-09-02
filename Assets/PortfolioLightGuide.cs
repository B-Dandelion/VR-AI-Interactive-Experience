using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Portfolio-capture cleanup for navigation guides.
/// The old 3D arrows are hidden, and no replacement guide is rendered for now.
/// A redesigned guide can be added later without touching the original scene assets.
/// </summary>
public static class PortfolioLightGuideBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        if (!Application.isPlaying) return;

        Scene activeScene = SceneManager.GetActiveScene();
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        int hidden = 0;

        foreach (Transform transform in transforms)
        {
            if (transform == null || !transform.gameObject.scene.IsValid()) continue;
            if (transform.gameObject.scene != activeScene) continue;

            string name = transform.name.ToLowerInvariant();
            if (!name.Contains("3d rightarrow")) continue;
            if (HasArrowNamedParent(transform)) continue;

            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.enabled = false;
                hidden++;
            }

            ArrowPulse[] pulses = transform.GetComponentsInChildren<ArrowPulse>(true);
            foreach (ArrowPulse pulse in pulses)
            {
                if (pulse != null) pulse.enabled = false;
            }
        }

        Debug.Log($"[PortfolioLightGuide] hidden {hidden} legacy arrow renderer(s); no replacement guide is shown.");
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
}
