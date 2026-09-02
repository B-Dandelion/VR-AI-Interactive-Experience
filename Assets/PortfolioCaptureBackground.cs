using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime-only background cleanup for portfolio capture.
/// Prevents the default blue skybox from flashing through when the XR camera briefly
/// looks outside the restored anatomical meshes during stage transitions.
/// </summary>
public class PortfolioCaptureBackground : MonoBehaviour
{
    private static readonly Color CaptureBackground = new Color(0.105f, 0.035f, 0.045f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!Application.isPlaying) return;

        GameObject runner = new GameObject("[Portfolio Capture Background]");
        Object.DontDestroyOnLoad(runner);
        runner.AddComponent<PortfolioCaptureBackground>();
    }

    private IEnumerator Start()
    {
        // Let XR camera components finish their normal scene initialization first.
        yield return null;

        Scene activeScene = SceneManager.GetActiveScene();
        Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
        int updated = 0;

        foreach (Camera camera in cameras)
        {
            if (camera == null || !camera.gameObject.scene.IsValid()) continue;
            if (camera.gameObject.scene != activeScene) continue;

            // Only alter gameplay cameras. Scene/preview/editor cameras are ignored.
            if (!camera.CompareTag("MainCamera") && camera.name != "Main Camera") continue;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CaptureBackground;
            updated++;
        }

        Debug.Log($"[PortfolioCaptureBackground] applied dark anatomical background to {updated} gameplay camera(s).");
        Destroy(gameObject);
    }
}
