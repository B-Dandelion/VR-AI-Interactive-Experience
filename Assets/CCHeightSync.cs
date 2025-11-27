using UnityEngine;
using Unity.XR.CoreUtils;

public class CCHeightSync : MonoBehaviour
{
    public XROrigin origin;
    public CharacterController controller;

    void Update()
    {
        if (origin == null || controller == null)
            return;

        float h = origin.CameraInOriginSpaceHeight;
        controller.height = Mathf.Clamp(h, 1.0f, 2.2f);
        controller.center = new Vector3(0, controller.height / 2f, 0);
    }
}
