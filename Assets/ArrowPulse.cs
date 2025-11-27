using UnityEngine;

public class ArrowPulse : MonoBehaviour
{
    public Renderer[] renderers;
    public Color baseColor = new Color(1f, 0.1f, 0.1f);  // Bright red
    public float pulseSpeed = 3f;
    public float minIntensity = 0.5f;
    public float maxIntensity = 4f;

    void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);

        foreach (Renderer r in renderers)
        {
            if (r != null && r.material != null)
                r.material.SetColor("_EmissionColor", baseColor * intensity);
        }
    }
}
