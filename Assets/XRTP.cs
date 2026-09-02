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
    public AudioClip specialSound;
    public bool playAsBGM = true;

    private XROrigin origin;
    private Transform playerHead;
    private AudioSource audioSource;

    void Start()
    {
        origin = FindObjectOfType<XROrigin>();
        audioSource = GetComponent<AudioSource>();

        // XR Device Simulator moves the tracked HMD independently from the XROrigin root.
        // The portal is a 3D trigger volume, so detection should follow the actual tracked head
        // position on every axis. This also supports vertical passages such as esophagus -> stomach.
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

        Vector3 playerPosition = GetPlayerDetectionPosition();
        Collider[] hits = Physics.OverlapSphere(playerPosition, detectRadius, padLayer);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == this.gameObject)
            {
                if (teleportManager != null)
                {
                    teleportManager.currentStageIndex = targetStageIndex;
                    Debug.Log($"새 스테이지 인덱스 설정: {targetStageIndex}");
                }

                StopAllPreviousSound();

                if (specialSound != null)
                {
                    PlaySpecialSound();
                }

                Teleport();
                break;
            }
        }
    }

    Vector3 GetPlayerDetectionPosition()
    {
        // For editor simulation, use the tracked HMD's complete world position.
        // Using X/Z from the camera but Y from XROrigin prevented vertically stacked
        // portal volumes from ever being detected when the simulator moved downward.
        if (playerHead != null)
        {
            return playerHead.position;
        }

        return origin.transform.position;
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
