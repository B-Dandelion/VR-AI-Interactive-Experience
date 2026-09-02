using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

// 텔레포트 패드 기능 + 오디오 관리 + 이전 소리 끄기 기능 포함
[RequireComponent(typeof(AudioSource))]
public class XRTeleportPad_CC : MonoBehaviour
{
    [Header("Existing Settings")]
    public Transform destination;
    public TeleportationProvider teleportProvider;

    public float detectRadius = 0.4f;
    public LayerMask padLayer;

    [Header("New Stage Settings")]
    public TeleportManager teleportManager;
    public int targetStageIndex;

    [Header("Audio Settings (음향)")]
    public AudioClip specialSound;   // 해당 패드를 밟았을 때 나올 안내 음성
    public bool playAsBGM = true;    // 체크하면 2D(배경음)처럼 들림

    private XROrigin origin;
    private Transform playerHead;
    private AudioSource audioSource;

    void Start()
    {
        origin = FindObjectOfType<XROrigin>();
        audioSource = GetComponent<AudioSource>();

        // XR Device Simulator can move the HMD camera without moving the XROrigin root.
        // Use the actual camera position for pad detection so editor simulation and HMD
        // play both detect the player's physical location correctly.
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerHead = mainCamera.transform;
        }

        if (teleportManager == null)
        {
            teleportManager = FindObjectOfType<TeleportManager>();
        }
    }

    void Update()
    {
        if (origin == null || destination == null || teleportProvider == null)
            return;

        Vector3 footPos = GetPlayerFootPosition();
        Collider[] hits = Physics.OverlapSphere(footPos, detectRadius, padLayer);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == this.gameObject)
            {
                // 1. 스테이지 인덱스 업데이트
                if (teleportManager != null)
                {
                    teleportManager.currentStageIndex = targetStageIndex;
                    Debug.Log($"새 스테이지 인덱스 설정: {targetStageIndex}");
                }

                // 2. 텔레포트 전에 기존에 재생 중인 소리 정리
                StopAllPreviousSound();

                // 3. 해당 스테이지 안내 음성 재생
                if (specialSound != null)
                {
                    PlaySpecialSound();
                }

                // 4. 텔레포트 실행
                Teleport();
                break;
            }
        }
    }

    Vector3 GetPlayerFootPosition()
    {
        // In the editor simulator the camera may move relative to XROrigin.
        // Use camera X/Z while keeping the origin floor height for a stable foot point.
        if (playerHead != null)
        {
            Vector3 footPos = playerHead.position;
            footPos.y = origin.transform.position.y;
            return footPos;
        }

        return origin.transform.position;
    }

    void StopAllPreviousSound()
    {
        // 1. AI 마이크(MicRecorder)가 말하고 있다면 끄기
        MicRecorder mic = FindObjectOfType<MicRecorder>();
        if (mic != null)
        {
            if (mic.audioSource != null && mic.audioSource.isPlaying)
            {
                mic.audioSource.Stop();
            }

            if (mic.statusUI != null)
            {
                mic.statusUI.HideImmediate();
            }
        }

        // 2. 다른 텔레포트 패드에서 재생 중인 소리 정리
        XRTeleportPad_CC[] allPads = FindObjectsOfType<XRTeleportPad_CC>();
        foreach (var pad in allPads)
        {
            if (pad != this)
            {
                AudioSource padAudio = pad.GetComponent<AudioSource>();
                if (padAudio != null && padAudio.isPlaying)
                {
                    padAudio.Stop();
                }
            }
        }

        Debug.Log("이전 사운드 및 AI 음성을 모두 종료했습니다.");
    }

    void PlaySpecialSound()
    {
        if (audioSource.isPlaying && audioSource.clip == specialSound) return;

        audioSource.clip = specialSound;

        if (playAsBGM) audioSource.spatialBlend = 0f;
        else audioSource.spatialBlend = 1f;

        audioSource.Play();
        Debug.Log("새로운 안내 음성 재생!");
    }

    void Teleport()
    {
        var req = new TeleportRequest()
        {
            destinationPosition = destination.position,
            destinationRotation = destination.rotation,
            matchOrientation = MatchOrientation.WorldSpaceUp
        };

        teleportProvider.QueueTeleportRequest(req);
    }
}
