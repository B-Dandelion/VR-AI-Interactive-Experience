using UnityEngine;

public class ColorBlinker3D : MonoBehaviour
{
    public Renderer targetRenderer;
    public float speed = 2.0f;

    [Header("변경할 두 가지 색상")]
    public Color colorA = Color.red;    // 첫 번째 색
    public Color colorB = Color.yellow; // 두 번째 색

    void Update()
    {
        if (targetRenderer != null)
        {
            // 0에서 1 사이를 오르락내리락하는 값(t)을 만듦
            float t = Mathf.PingPong(Time.time * speed, 1.0f);

            // A와 B 사이를 t만큼 섞어서 현재 색을 결정 (Lerp: 선형 보간)
            Color currentColor = Color.Lerp(colorA, colorB, t);

            // 재질 색상 적용
            targetRenderer.material.color = currentColor;
        }
    }
}