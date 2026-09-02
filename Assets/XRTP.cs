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
    private AudioSource audioSource;

    void Start()
    {
        origin = FindObjectOfType<XROrigin>();
        audioSource = GetComponent<AudioSource>();

        if (teleportManager == null)
        {
            teleportManager = FindObjectOfType<TeleportManager>();
        }
    }

    void Update()
    {
        if (origin == null || destination == null || teleportProvider == null)
            return;

        Vector3 footPos = origin.transform.position;
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

                // 2. 텔레포트 전에 기존 오디오 종료
                StopAllPreviousSound();

                // 3. 현재 스테이지 안내 음성 재생
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

    void StopAllPreviousSound()
    {
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
        audioSource.spatialBlend = playAsBGM ? 0f : 1f;
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
