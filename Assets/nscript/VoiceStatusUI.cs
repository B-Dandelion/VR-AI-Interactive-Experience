using UnityEngine;
using TMPro;
using System.Collections;

public class VoiceStatusUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public GameObject canvasObj;
    public TextMeshProUGUI statusText;

    [Header("상태별 설정")]
    public string recordMsg = "Listening...";
    public Color recordColor = Color.white;

    public string processMsg = "Thinking...";
    public Color processColor = Color.yellow;

    public string playMsg = "Speaking...";
    public Color playColor = Color.green;

    [Header("에러 설정")]
    public Color errorColor = new Color(1f, 0f, 0f); // 주황/붉은 계열

    // 숨기기 타이머 관리용 코루틴 변수
    private Coroutine hideCoroutine;

    void Start()
    {
        HideImmediate();
    }

    // --- 상태 표시 함수들 ---

    public void ShowRecording()
    {
        // 새 상태가 되면 기존에 돌던 '숨기기 타이머'는 무조건 취소
        StopAutoHide();
        SetStatus(recordMsg, recordColor);
    }

    public void ShowProcessing()
    {
        StopAutoHide();
        SetStatus(processMsg, processColor);
    }

    public void ShowPlaying()
    {
        StopAutoHide();
        SetStatus(playMsg, playColor);
    }

    // ★ [추가됨] 에러 메시지 출력 (자동으로 4초 뒤 꺼짐)
    public void ShowError(string message)
    {
        StopAutoHide(); // 기존 타이머 취소
        SetStatus($"Error: {message}", errorColor);

        // 에러는 뜨자마자 4초 카운트다운 시작
        HideDelayed(4.0f);
    }

    // --- 숨기기 로직 ---

    // 1. 즉시 숨기기 (녹음 시작 전 등)
    public void HideImmediate()
    {
        StopAutoHide();
        if (canvasObj != null) canvasObj.SetActive(false);
    }

    // 2. n초 뒤 숨기기 (성공/실패 후)
    public void HideDelayed(float seconds)
    {
        StopAutoHide(); // 중복 실행 방지
        hideCoroutine = StartCoroutine(CoHideRoutine(seconds));
    }

    private IEnumerator CoHideRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (canvasObj != null) canvasObj.SetActive(false);
    }

    // --- 내부 유틸 ---

    private void SetStatus(string text, Color color)
    {
        if (canvasObj != null) canvasObj.SetActive(true);
        if (statusText != null)
        {
            statusText.text = text;
            statusText.color = color;
        }
    }

    private void StopAutoHide()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }
}