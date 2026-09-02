using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Portfolio-capture-only fix for the mouth -> esophagus transition.
/// F8 probing identified a large "Door 1" renderer overlapping the camera
/// while the flat blue surface is visible. Hide only its Renderer(s); keep
/// the GameObject and any Collider/trigger components untouched.
/// </summary>
public static class PortfolioEsophagusCaptureFix
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
            if (transform.name.Trim() != "Door 1") continue;

            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.enabled = false;
                hidden++;
            }
        }

        Debug.Log($"[PortfolioEsophagusCaptureFix] hid {hidden} renderer(s) under 'Door 1'. Colliders and teleport logic are untouched.");
    }
}
