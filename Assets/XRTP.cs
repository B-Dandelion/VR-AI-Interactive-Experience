using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

public class XRTeleportPad_CC : MonoBehaviour
{
    [Header("Existing Settings")]
    public Transform destination;
    public TeleportationProvider teleportProvider;

    public float detectRadius = 0.4f;
    public LayerMask padLayer;

    [Header("New Stage Settings")] // --- [추가된 부분 1] ---
    public TeleportManager teleportManager; // 매니저 연결
    public int targetStageIndex;            // 이동 후 저장될 스테이지 번호 (0, 1, 2...)

    private XROrigin origin;

    void Start()
    {
        origin = FindObjectOfType<XROrigin>();

        // --- [추가된 부분 2] 매니저가 비어있으면 자동으로 찾음 ---
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
                // --- [추가된 부분 3] 텔레포트 직전에 매니저에게 보고 ---
                if (teleportManager != null)
                {
                    teleportManager.currentStageIndex = targetStageIndex;
                    Debug.Log($" 스테이지 인덱스 갱신됨: {targetStageIndex}");
                }

                Teleport();
                break;
            }
        }
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