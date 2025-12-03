using UnityEngine;
using UnityEngine.InputSystem; // �� Input System ����

public class MicRecorderInput : MonoBehaviour
{
    [Tooltip(" ")]
    public MicRecorder mic;
    [Tooltip(" ")]
    public InputActionProperty recordAction;

    void OnEnable()
    {
        if (recordAction != null)
            recordAction.action.Enable();
    }

    void OnDisable()
    {
        if (recordAction != null)
            recordAction.action.Disable();
    }

    void Update()
    {
        if (recordAction == null || mic == null)
            return;

        var action = recordAction.action;

        //
        if (action.WasPressedThisFrame())
        {
            mic.StartRecord();
        }

        // 
        if (action.WasReleasedThisFrame())
        {
            mic.StopRecordAndSend();
        }
    }
}
