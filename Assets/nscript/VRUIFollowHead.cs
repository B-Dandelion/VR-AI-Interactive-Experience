using UnityEngine;

public class VRUIFollowHead : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("플레이어의 메인 카메라 (연결 안 하면 자동으로 찾음)")]
    public Transform headCamera;

    [Tooltip("눈앞에서 얼마나 떨어질지 (미터)")]
    public float distance = 1.5f;

    [Tooltip("따라오는 속도 (낮을수록 부드럽고 느림, 높을수록 빠름)")]
    public float smoothSpeed = 5.0f; // 5~10 추천

    [Tooltip("위아래 고개 끄덕임도 따라갈 것인가? (자막은 true 추천)")]
    public bool followPitch = true;

    void OnEnable()
    {
        // 캔버스가 켜질 때마다 카메라가 없으면 다시 찾음
        if (headCamera == null)
        {
            if (Camera.main != null)
                headCamera = Camera.main.transform;
            else
                // XR Origin의 Main Camera 태그가 MainCamera인지 확인하세요
                Debug.LogWarning("[VRUIFollowHead] 메인 카메라를 찾을 수 없습니다.");
        }

        // 켜지는 순간에는 바로 눈앞으로 텔레포트 (안 그러면 멀리서 날아옴)
        if (headCamera != null)
        {
            SnapToFront();
        }
    }

    void LateUpdate()
    {
        if (headCamera == null) return;

        // 1. 목표 위치 계산
        Vector3 targetPos = headCamera.position + (headCamera.forward * distance);

        // 높이(Pitch) 고정 옵션이 꺼져있다면 Y축은 내 현재 높이 유지
        if (!followPitch)
        {
            targetPos.y = transform.position.y;
        }

        // 2. 부드럽게 이동 (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);

        // 3. 항상 카메라를 바라보게 회전
        // (UI가 뒤집히지 않도록 카메라와 같은 회전값을 주되, 좌우 반전 고려하여 LookRotation 사용)
        transform.rotation = Quaternion.LookRotation(transform.position - headCamera.position);
    }

    // 즉시 눈앞으로 이동시키는 함수
    private void SnapToFront()
    {
        transform.position = headCamera.position + (headCamera.forward * distance);
        transform.rotation = Quaternion.LookRotation(transform.position - headCamera.position);
    }
}