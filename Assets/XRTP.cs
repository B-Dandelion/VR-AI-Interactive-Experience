using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

// ? ????? ??? AudioSource? ???? ????.
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

    [Header("Audio Settings (??)")] // --- [???] ---
    public AudioClip specialSound;   // ??? MP3? ??? ??? (? ??? ???)
    public bool playAsBGM = true;    // ???? ??? ???? ?? ?? (2D)

    private XROrigin origin;
    private AudioSource audioSource; // --- [???] ---

    void Start()
    {
        origin = FindObjectOfType<XROrigin>();

        // ??? ?? ????
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
                // 1. ???? ??
                if (teleportManager != null)
                {
                    teleportManager.currentStageIndex = targetStageIndex;
                    Debug.Log($"? ???? ??? ???: {targetStageIndex}");
                }

                // 2. [???] ?? ??? ??
                if (specialSound != null)
                {
                    PlaySpecialSound();
                }

                // 3. ????
                Teleport();
                break;
            }
        }
    }

    void PlaySpecialSound()
    {
        // ?? ?? ??? ?? ?? ?? (??)
        if (audioSource.isPlaying && audioSource.clip == specialSound) return;

        audioSource.clip = specialSound;

        // ????? ????? ?? ???? ???
        // ??? ???? ?? 2D(0)? ??? ?? ?????.
        if (playAsBGM) audioSource.spatialBlend = 0f;
        else audioSource.spatialBlend = 1f;

        audioSource.Play();
        Debug.Log("?? ?? ??? ?? ??!");
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