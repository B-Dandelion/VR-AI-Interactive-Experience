using UnityEngine;
using TMPro;
using System.Collections;

public class VoiceStatusUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject canvasObj;
    public TextMeshProUGUI statusText;

    [Header("Status Messages")]
    public string recordMsg = "Listening...";
    public Color recordColor = Color.white;

    public string processMsg = "Thinking...";
    public Color processColor = Color.yellow;

    public string playMsg = "Speaking...";
    public Color playColor = Color.green;

    [Header("Error")]
    public Color errorColor = new Color(1f, 0f, 0f);

    private Coroutine hideCoroutine;

    void Start()
    {
        if (canvasObj != null) canvasObj.SetActive(true);
        ClearText();
    }

    public void ShowRecording()
    {
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

    // Portfolio demo and other scripted UI can reuse the same status canvas.
    public void ShowMessage(string message, Color color)
    {
        StopAutoHide();
        SetStatus(message, color);
    }

    public void ShowError(string message)
    {
        StopAutoHide();
        SetStatus($"Error: {message}", errorColor);
        HideDelayed(4.0f);
    }

    public void HideImmediate()
    {
        StopAutoHide();
        ClearText();
    }

    public void HideDelayed(float seconds)
    {
        StopAutoHide();
        hideCoroutine = StartCoroutine(CoHideRoutine(seconds));
    }

    private IEnumerator CoHideRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearText();
    }

    private void SetStatus(string text, Color color)
    {
        if (canvasObj != null && !canvasObj.activeSelf) canvasObj.SetActive(true);

        if (statusText != null)
        {
            statusText.text = text;
            statusText.color = color;
        }
    }

    private void ClearText()
    {
        if (statusText != null) statusText.text = "";
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
