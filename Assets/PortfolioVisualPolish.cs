using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime-only visual cleanup for portfolio capture.
/// Imported model/material assets are never modified on disk.
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

            // Keep VFX / line-style renderers out of the anatomical material override.
            bool canPolishAsMesh = !(renderer is ParticleSystemRenderer) &&
                                   !(renderer is TrailRenderer) &&
                                   !(renderer is LineRenderer);

            if (canPolishAsMesh && ContainsAny(hierarchyPath, OrganNameTokens))
            {
                polishedMaterials += ReplaceWithPortfolioMatteMaterials(renderer);
            }

            if (ShouldHideColliderOnlyVisual(renderer, hierarchyPath))
            {
                renderer.enabled = false;
                hiddenBlockers++;
            }
        }

        Debug.Log($"[PortfolioVisualPolish] matte organ materials: {polishedMaterials}, hidden blocker renderers: {hiddenBlockers}.");
    }

    /// <summary>
    /// Replaces only the runtime material instances on anatomical renderers.
    /// The original base texture/color are reused, while metallic/specular/environment
    /// reflection data are intentionally discarded to remove the blue plastic sheen.
    /// </summary>
    private static int ReplaceWithPortfolioMatteMaterials(Renderer renderer)
    {
        Material[] sourceMaterials = renderer.sharedMaterials;
        if (sourceMaterials == null || sourceMaterials.Length == 0) return 0;

        Shader matteShader = Shader.Find("Universal Render Pipeline/Lit");
        if (matteShader == null)
        {
            Debug.LogWarning("[PortfolioVisualPolish] URP/Lit shader not found. Falling back to in-place reflection reduction.");
            return ReduceExistingMaterialReflections(renderer);
        }

        Material[] replacements = new Material[sourceMaterials.Length];
        int changed = 0;

        for (int i = 0; i < sourceMaterials.Length; i++)
        {
            Material source = sourceMaterials[i];
            if (source == null)
            {
                replacements[i] = null;
                continue;
            }

            Material matte = new Material(matteShader);
            matte.name = source.name + " [Portfolio Matte]";

            Texture baseTexture = GetBaseTexture(source);
            Color baseColor = GetBaseColor(source);

            if (matte.HasProperty("_BaseColor"))
                matte.SetColor("_BaseColor", baseColor);

            if (baseTexture != null && matte.HasProperty("_BaseMap"))
            {
                matte.SetTexture("_BaseMap", baseTexture);

                // Preserve texture tiling/offset when the source exposes a common albedo slot.
                string sourceTextureProperty = GetBaseTextureProperty(source);
                if (!string.IsNullOrEmpty(sourceTextureProperty))
                {
                    matte.SetTextureScale("_BaseMap", source.GetTextureScale(sourceTextureProperty));
                    matte.SetTextureOffset("_BaseMap", source.GetTextureOffset(sourceTextureProperty));
                }
            }

            // Keep useful surface detail, but do not copy metallic/specular maps.
            Texture normalMap = GetFirstTexture(source, "_BumpMap", "_NormalMap");
            if (normalMap != null && matte.HasProperty("_BumpMap"))
            {
                matte.SetTexture("_BumpMap", normalMap);
                if (matte.HasProperty("_BumpScale")) matte.SetFloat("_BumpScale", 0.65f);
                matte.EnableKeyword("_NORMALMAP");
            }

            // Hard matte settings. These intentionally ignore the source asset's
            // gloss/metallic/specular maps that caused strong blue sky reflections.
            if (matte.HasProperty("_Metallic")) matte.SetFloat("_Metallic", 0f);
            if (matte.HasProperty("_Smoothness")) matte.SetFloat("_Smoothness", 0.025f);
            if (matte.HasProperty("_Glossiness")) matte.SetFloat("_Glossiness", 0.025f);
            if (matte.HasProperty("_SpecColor")) matte.SetColor("_SpecColor", Color.black);
            if (matte.HasProperty("_SpecularHighlights")) matte.SetFloat("_SpecularHighlights", 0f);
            if (matte.HasProperty("_EnvironmentReflections")) matte.SetFloat("_EnvironmentReflections", 0f);

            // URP uses keywords for the two checkboxes above; setting only the float
            // is not sufficient for every shader/material state.
            matte.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            matte.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");

            replacements[i] = matte;
            changed++;

            Debug.Log($"[PortfolioVisualPolish] {renderer.name}: '{source.shader.name}' -> URP matte, baseTexture={(baseTexture != null ? baseTexture.name : "none")}");
        }

        renderer.materials = replacements;
        return changed;
    }

    private static int ReduceExistingMaterialReflections(Renderer renderer)
    {
        int changed = 0;
        Material[] materials = renderer.materials;

        foreach (Material material in materials)
        {
            if (material == null) continue;

            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.025f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.025f);
            if (material.HasProperty("_SpecColor")) material.SetColor("_SpecColor", Color.black);
            if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 0f);
            if (material.HasProperty("_EnvironmentReflections")) material.SetFloat("_EnvironmentReflections", 0f);

            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            changed++;
        }

        return changed;
    }

    private static Texture GetBaseTexture(Material material)
    {
        string property = GetBaseTextureProperty(material);
        return string.IsNullOrEmpty(property) ? material.mainTexture : material.GetTexture(property);
    }

    private static string GetBaseTextureProperty(Material material)
    {
        string[] candidates = { "_BaseMap", "_MainTex", "_BaseColorMap", "_AlbedoMap", "_DiffuseMap" };
        foreach (string property in candidates)
        {
            if (material.HasProperty(property) && material.GetTexture(property) != null)
                return property;
        }
        return null;
    }

    private static Texture GetFirstTexture(Material material, params string[] properties)
    {
        foreach (string property in properties)
        {
            if (material.HasProperty(property))
            {
                Texture texture = material.GetTexture(property);
                if (texture != null) return texture;
            }
        }
        return null;
    }

    private static Color GetBaseColor(Material material)
    {
        if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color")) return material.GetColor("_Color");
        return Color.white;
    }

    private static bool ShouldHideColliderOnlyVisual(Renderer renderer, string hierarchyPath)
    {
        if (ContainsAny(hierarchyPath, TransparentExclusions)) return false;

        bool hasCollider = renderer.GetComponent<Collider>() != null ||
                           renderer.GetComponentInParent<Collider>() != null;
        if (!hasCollider) return false;

        if (ContainsAny(hierarchyPath, ExplicitBlockerTokens)) return true;

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
