using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight runtime-only visual cleanup for portfolio capture.
/// It does not modify imported model/material assets on disk.
/// </summary>
public static class PortfolioVisualPolish
{
    private static readonly string[] OrganNameTokens =
    {
        "human_mouth_detailed",
        "realistic_human_stomach",
        "small_and_large_intestine",
        "esophagus",
        "stomach",
        "intestine"
    };

    private static readonly string[] ExplicitBlockerTokens =
    {
        "start wall",
        "invisible wall",
        "transparent wall",
        "blocker"
    };

    private static readonly string[] TransparentExclusions =
    {
        "water",
        "glass",
        "window"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyForPortfolioCapture()
    {
        if (!Application.isPlaying) return;

        int polishedMaterials = 0;
        int hiddenBlockers = 0;

        Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.gameObject.scene.IsValid()) continue;
            if (renderer.gameObject.scene != SceneManager.GetActiveScene()) continue;

            string hierarchyPath = BuildHierarchyPath(renderer.transform).ToLowerInvariant();

            if (ContainsAny(hierarchyPath, OrganNameTokens))
            {
                polishedMaterials += ReduceOrganReflections(renderer);
            }

            if (ShouldHideColliderOnlyVisual(renderer, hierarchyPath))
            {
                renderer.enabled = false;
                hiddenBlockers++;
            }
        }

        Debug.Log($"[PortfolioVisualPolish] reduced reflections on {polishedMaterials} material(s), hidden {hiddenBlockers} blocker renderer(s).");
    }

    private static int ReduceOrganReflections(Renderer renderer)
    {
        int changed = 0;

        // renderer.materials creates runtime instances, so imported assets remain untouched.
        Material[] materials = renderer.materials;
        foreach (Material material in materials)
        {
            if (material == null) continue;

            bool didChange = false;

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
                didChange = true;
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.12f);
                didChange = true;
            }

            // Standard shader compatibility.
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.12f);
                didChange = true;
            }

            // URP Lit toggles. Zero removes the strong sky/environment sheen
            // that can read as blue plastic on anatomical meshes.
            if (material.HasProperty("_SpecularHighlights"))
            {
                material.SetFloat("_SpecularHighlights", 0f);
                didChange = true;
            }

            if (material.HasProperty("_EnvironmentReflections"))
            {
                material.SetFloat("_EnvironmentReflections", 0f);
                didChange = true;
            }

            if (didChange) changed++;
        }

        return changed;
    }

    private static bool ShouldHideColliderOnlyVisual(Renderer renderer, string hierarchyPath)
    {
        if (ContainsAny(hierarchyPath, TransparentExclusions)) return false;

        bool hasCollider = renderer.GetComponent<Collider>() != null ||
                           renderer.GetComponentInParent<Collider>() != null;
        if (!hasCollider) return false;

        // Known collision guides used as walls in the restored class project.
        if (ContainsAny(hierarchyPath, ExplicitBlockerTokens)) return true;

        // Also hide very low-alpha transparent meshes when they exist mainly as
        // collision geometry. Decorative water/glass/window objects are excluded above.
        foreach (Material material in renderer.sharedMaterials)
        {
            if (material == null) continue;

            bool transparentSurface = material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f;
            float alpha = 1f;

            if (material.HasProperty("_BaseColor"))
                alpha = material.GetColor("_BaseColor").a;
            else if (material.HasProperty("_Color"))
                alpha = material.GetColor("_Color").a;

            if (transparentSurface && alpha <= 0.35f) return true;
        }

        return false;
    }

    private static bool ContainsAny(string source, string[] tokens)
    {
        foreach (string token in tokens)
        {
            if (source.Contains(token)) return true;
        }
        return false;
    }

    private static string BuildHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
