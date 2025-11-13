using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

/// <summary>
/// Step on this pad (trigger) to teleport XR Origin to a fixed destination.
/// Attach to a GameObject with a Collider (IsTrigger = true).
/// </summary>
public class XRTeleportPad : MonoBehaviour
{
    [Header("Where to send the player")]
    public Transform destination;

    [Header("XR Origin's TeleportationProvider")]
    public TeleportationProvider teleportProvider;

    private void OnTriggerEnter(Collider other)
    {
        // Only react when the XR Origin (player) enters
        var origin = other.GetComponentInParent<XROrigin>();
        if (origin == null || destination == null || teleportProvider == null) return;

        var req = new TeleportRequest
        {
            destinationPosition = destination.position,
            destinationRotation = destination.rotation,
            matchOrientation = MatchOrientation.TargetUpAndForward
        };
        teleportProvider.QueueTeleportRequest(req);
    }
}
