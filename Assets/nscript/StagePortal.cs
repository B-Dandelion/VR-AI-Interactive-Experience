using UnityEngine;

public class StagePortal : MonoBehaviour
{
    [Header("설정")]
    public int targetStageIndex; // 이동할 스테이지 번호 (0=1탄, 1=2탄...)
    public TeleportManager manager; // 아까 만든 매니저 연결

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어(Player 태그)가 닿았을 때만 실행
        if (other.CompareTag("Player"))
        {
            Debug.Log($" 스테이지 {targetStageIndex + 1} 진입! 저장 지점 갱신됨.");

            // 1. 매니저에게 "현재 스테이지는 여기야"라고 알려줌 (핵심 ⭐)
            manager.currentStageIndex = targetStageIndex;

            // 2. 해당 스테이지 시작점으로 텔레포트
            // (매니저에 있는 배열 위치를 가져와서 이동)
            if (targetStageIndex < manager.stageStartPoints.Length)
            {
                Transform targetPoint = manager.stageStartPoints[targetStageIndex];

                // 캐릭터 컨트롤러 껐다 켜기 (물리 충돌 방지)
                CharacterController cc = other.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                other.transform.position = targetPoint.position;
                other.transform.rotation = targetPoint.rotation;

                if (cc != null) cc.enabled = true;
            }
        }
    }
}