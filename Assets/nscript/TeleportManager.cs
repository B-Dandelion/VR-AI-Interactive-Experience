using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TeleportManager : MonoBehaviour
{
    [Header("UI 설정")]
    public GameObject uiCanvas;
    public Image fillGauge;

    [Header("입력 설정")]
    public InputActionProperty globalResetInput; // X버튼
    public InputActionProperty stageResetInput;  // Y버튼

    [Header("이동 설정 (여기 추가됨)")]
    public Transform xrOrigin;          // 플레이어 (이동시킬 대상)
    public Transform globalStartPoint;  // 전체 시작점 위치
    public Transform[] stageStartPoints; // 스테이지별 시작점 (배열)

    [Header("현재 상태")]
    public int currentStageIndex = 0;   // 현재 몇 스테이지인가? (0부터 시작)
    public float holdTime = 5.0f;

    private float _currentTimer = 0f;
    private bool _isTeleported = false;

    void Start()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);
    }

    void Update()
    {
        float globalValue = globalResetInput.action?.ReadValue<float>() ?? 0;
        float stageValue = stageResetInput.action?.ReadValue<float>() ?? 0;
        bool isPressed = (globalValue > 0.5f) || (stageValue > 0.5f);

        if (isPressed)
        {
            if (!_isTeleported)
            {
                if (!uiCanvas.activeSelf) uiCanvas.SetActive(true);

                _currentTimer += Time.deltaTime;
                if (fillGauge != null) fillGauge.fillAmount = _currentTimer / holdTime;

                if (_currentTimer >= holdTime)
                {
                    // 5초 완료! 실제 이동 함수 호출
                    if (globalValue > 0.5f)
                    {
                        TeleportToGlobal();
                    }
                    else
                    {
                        TeleportToCurrentStage();
                    }

                    _isTeleported = true;
                    ResetSystem();
                }
            }
        }
        else
        {
            _currentTimer = 0f;
            _isTeleported = false;
            ResetSystem();
        }
    }

    void ResetSystem()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);
        if (fillGauge != null) fillGauge.fillAmount = 0;
    }

    // --- 실제 이동 로직 ---

    void TeleportToGlobal()
    {
        Debug.Log(">>> 전체 초기화!");
        currentStageIndex = 0; // 스테이지 1로 초기화
        MovePlayer(globalStartPoint);
    }

    void TeleportToCurrentStage()
    {
        Debug.Log($">>> 스테이지 {currentStageIndex + 1} 재시작!");

        // 스테이지 번호가 안전한지 체크
        if (currentStageIndex >= 0 && currentStageIndex < stageStartPoints.Length)
        {
            MovePlayer(stageStartPoints[currentStageIndex]);
        }
        else
        {
            Debug.LogWarning("이동할 스테이지 위치가 설정되지 않았습니다!");
        }
    }

    void MovePlayer(Transform target)
    {
        if (xrOrigin == null || target == null) return;

        // 1. 물리 충돌 방지를 위해 캐릭터 컨트롤러 잠시 끄기 (중요)
        CharacterController cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 2. 위치와 회전 복사
        xrOrigin.position = target.position;
        xrOrigin.rotation = target.rotation;

        // 3. 다시 켜기
        if (cc != null) cc.enabled = true;
    }
}