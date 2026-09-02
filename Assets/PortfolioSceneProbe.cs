using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Temporary runtime diagnostic for the portfolio-restoration pass.
/// Press F8 while the unwanted blue surface is visible. The script logs the center-ray hit
/// plus the closest visible renderers/materials around the VR camera so the offending mesh
/// can be identified without blindly deleting scene geometry.
/// </summary>
public class PortfolioSceneProbe : MonoBehaviour
{
    private struct Candidate
    {
        public Renderer renderer;
        public float distance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!Application.isPlaying) return;
        GameObject runner = new GameObject("[Portfolio Scene Probe - F8]");
        DontDestroyOnLoad(runner);
        runner.AddComponent<PortfolioSceneProbe>();
        Debug.Log("[PortfolioSceneProbe] Ready. Press F8 while the unwanted blue surface is visible.");
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            Probe();
    }

    private static void Probe()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogWarning("[PortfolioSceneProbe] Main Camera not found.");
            return;
        }

        Debug.Log($"[PortfolioSceneProbe] === F8 probe at camera {camera.transform.position} ===");

        Ray ray = new Ray(camera.transform.position, camera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 5000f, ~0, QueryTriggerInteraction.Ignore))
        {
            Renderer hitRenderer = hit.collider != null ? hit.collider.GetComponentInParent<Renderer>() : null;
            string rendererInfo = hitRenderer != null ? DescribeRenderer(hitRenderer) : "no Renderer on hit hierarchy";
            Debug.Log($"[PortfolioSceneProbe] CENTER RAY -> collider='{BuildPath(hit.collider.transform)}', distance={hit.distance:F2}; {rendererInfo}");
        }
        else
        {
            Debug.Log("[PortfolioSceneProbe] CENTER RAY -> no collider hit.");
        }

        Renderer[] all = Resources.FindObjectsOfTypeAll<Renderer>();
        List<Candidate> candidates = new List<Candidate>();
        Scene activeScene = SceneManager.GetActiveScene();
        Vector3 cameraPosition = camera.transform.position;

        foreach (Renderer renderer in all)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!renderer.gameObject.scene.IsValid() || renderer.gameObject.scene != activeScene) continue;

            float distance = Mathf.Sqrt(renderer.bounds.SqrDistance(cameraPosition));
            if (distance > 80f) continue;

            candidates.Add(new Candidate { renderer = renderer, distance = distance });
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        int count = Mathf.Min(18, candidates.Count);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"[PortfolioSceneProbe] Closest visible renderers ({count}):");
        for (int i = 0; i < count; i++)
        {
            Candidate candidate = candidates[i];
            builder.Append("  #").Append(i + 1)
                   .Append(" dist=").Append(candidate.distance.ToString("F2"))
                   .Append(" | ").Append(DescribeRenderer(candidate.renderer))
                   .AppendLine();
        }

        Debug.Log(builder.ToString());
    }

    private static string DescribeRenderer(Renderer renderer)
    {
        StringBuilder materialInfo = new StringBuilder();
        Material[] materials = renderer.sharedMaterials;

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (i > 0) materialInfo.Append("; ");
            if (material == null)
            {
                materialInfo.Append("<null>");
                continue;
            }

            Color color = Color.white;
            bool hasColor = false;
            if (material.HasProperty("_BaseColor"))
            {
                color = material.GetColor("_BaseColor");
                hasColor = true;
            }
            else if (material.HasProperty("_Color"))
            {
                color = material.GetColor("_Color");
                hasColor = true;
            }

            materialInfo.Append(material.name)
                        .Append(" [")
                        .Append(material.shader != null ? material.shader.name : "no shader")
                        .Append("]");

            if (hasColor)
            {
                materialInfo.Append(" color=")
                            .Append(color.r.ToString("F2")).Append(",")
                            .Append(color.g.ToString("F2")).Append(",")
                            .Append(color.b.ToString("F2")).Append(",")
                            .Append(color.a.ToString("F2"));
            }
        }

        Vector3 size = renderer.bounds.size;
        return $"'{BuildPath(renderer.transform)}' renderer={renderer.GetType().Name} bounds=({size.x:F1},{size.y:F1},{size.z:F1}) materials=[{materialInfo}]";
    }

    private static string BuildPath(Transform transform)
    {
        if (transform == null) return "<null>";
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
