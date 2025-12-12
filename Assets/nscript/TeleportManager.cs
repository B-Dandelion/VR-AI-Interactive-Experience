using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TeleportManager : MonoBehaviour
{
    [Header("UI ????")]
    public GameObject uiCanvas;
    public Image fillGauge;

    [Header("???? ????")]
    public InputActionProperty globalResetInput; // X????
    public InputActionProperty stageResetInput;  // Y????

    [Header("???? ???? (???? ??????)")]
    public Transform xrOrigin;          // ???????? (???????? ????)
    public Transform globalStartPoint;  // ???? ?????? ????
    public Transform[] stageStartPoints; // ?????????? ?????? (????)

    [Header("???? ????")]
    public int currentStageIndex = 0;   // ???? ?? ????????????? (0???? ????)
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
                    // 5?? ????! ???? ???? ???? ????
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

    // --- ???? ???? ???? ---

    public void TeleportToGlobal()
    {
        Debug.Log(">>> ???? ??????!");
        currentStageIndex = 0; // ???????? 1?? ??????
        StopAllSounds();
        MovePlayer(globalStartPoint);
    }
    private void StopAllSounds()
    {
        // 1. AI 마이크(MicRecorder) 끄기
        MicRecorder mic = FindObjectOfType<MicRecorder>();
        if (mic != null)
        {
            if (mic.audioSource != null && mic.audioSource.isPlaying) mic.audioSource.Stop();
            if (mic.statusUI != null) mic.statusUI.HideImmediate();
        }

        // 2. 맵에 있는 텔레포트 패드들 소리 끄기
        XRTeleportPad_CC[] allPads = FindObjectsOfType<XRTeleportPad_CC>();
        foreach (var pad in allPads)
        {
            AudioSource padAudio = pad.GetComponent<AudioSource>();
            if (padAudio != null && padAudio.isPlaying)
            {
                padAudio.Stop();
            }
        }
    }

    public void TeleportToCurrentStage()
    {
        Debug.Log($">>> ???????? {currentStageIndex + 1} ??????!");

        // ???????? ?????? ???????? ????
        if (currentStageIndex >= 0 && currentStageIndex < stageStartPoints.Length)
        {
            MovePlayer(stageStartPoints[currentStageIndex]);
        }
        else
        {
            Debug.LogWarning("?????? ???????? ?????? ???????? ??????????!");
        }
    }

    void MovePlayer(Transform target)
    {
        if (xrOrigin == null || target == null) return;

        // 1. ???? ???? ?????? ???? ?????? ???????? ???? ???? (????)
        CharacterController cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 2. ?????? ???? ????
        xrOrigin.position = target.position;
        xrOrigin.rotation = target.rotation;

        // 3. ???? ????
        if (cc != null) cc.enabled = true;
    }
}
