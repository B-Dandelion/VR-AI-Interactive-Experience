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

                // ★ [추가됨] 2. 텔레포트 전에 기존에 떠들던 애들 입 다물게 하기
                StopAllPreviousSound();

                // 3. 내 안내 음성 재생
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

    // ★ [핵심 기능] 이전 소리 강제 종료 함수
    void StopAllPreviousSound()
    {
        // 1. AI 마이크(MicRecorder)가 말하고 있다면 끄기
        MicRecorder mic = FindObjectOfType<MicRecorder>();
        if (mic != null)
        {
            // 오디오 끄기
            if (mic.audioSource != null && mic.audioSource.isPlaying)
            {
                mic.audioSource.Stop();
            }
            // "Speaking..." 같은 UI도 즉시 숨기기
            if (mic.statusUI != null)
            {
                mic.statusUI.HideImmediate();
            }
        }

        // 2. 다른 텔레포트 패드에서 나오고 있던 소리 끄기 (혹시 겹칠까봐)
        XRTeleportPad_CC[] allPads = FindObjectsOfType<XRTeleportPad_CC>();
        foreach (var pad in allPads)
        {
            // 나 자신(this)은 이제 소리를 내야 하니까 끄지 말고, 남들만 끔
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
        // 이미 내가 재생 중이면 패스
        if (audioSource.isPlaying && audioSource.clip == specialSound) return;

        audioSource.clip = specialSound;

        // BGM처럼 들리게 할지(2D), 공간감 있게 들리게 할지(3D) 설정
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