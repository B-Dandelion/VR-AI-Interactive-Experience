using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// UI 버튼이나 3D 오브젝트에 붙여서 사용합니다.
public class LongPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Settings")]
    [Tooltip("버튼을 눌러야 하는 시간 (초)")]
    [SerializeField] private float requiredHoldTime = 5.0f;

    [Header("UI Feedback")]
    [Tooltip("진행 상황을 보여줄 이미지 (Image Type을 Filled로 설정하세요)")]
    [SerializeField] private Image progressImage;

    [Header("Events")]
    public UnityEvent OnLongPressComplete; // 5초 완료 시 실행

    private bool isPressed = false;
    private float currentTimer = 0f;
    private bool isCompleted = false;

    private void Start()
    {
        if (progressImage != null)
        {
            progressImage.fillAmount = 0f;
        }
    }

    private void Update()
    {
        if (isPressed && !isCompleted)
        {
            // 타이머 증가
            currentTimer += Time.deltaTime;

            // UI 업데이트
            if (progressImage != null)
            {
                progressImage.fillAmount = currentTimer / requiredHoldTime;
            }

            // 5초 도달 체크
            if (currentTimer >= requiredHoldTime)
            {
                ExecuteFunction();
            }
        }
    }

    private void ExecuteFunction()
    {
        isCompleted = true;
        OnLongPressComplete?.Invoke();

        // 완료 후 피드백 (선택사항: 진동, 소리 등 추가 가능)
        Debug.Log("5초 누름 완료: 이동 실행");

        // 완료 후 즉시 리셋할지, 손을 뗐을 때 리셋할지는 기획에 따라 결정
        // 여기서는 유지하다가 손을 떼면 리셋되도록 둠
    }

    private void ResetButton()
    {
        isPressed = false;
        isCompleted = false;
        currentTimer = 0f;
        if (progressImage != null)
        {
            progressImage.fillAmount = 0f;
        }
    }

    // --- 인터페이스 구현 (XR Ray Interactor가 UI를 클릭할 때 작동) ---

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetButton();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 버튼 밖으로 포인터가 나가면 취소
        ResetButton();
    }
}