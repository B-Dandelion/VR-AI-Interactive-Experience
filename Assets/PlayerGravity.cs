using UnityEngine;

public class PlayerGravity : MonoBehaviour
{
    public float gravity = -9.81f;
    public float verticalVelocity;
    public LayerMask groundLayer;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            if (verticalVelocity < 0)
                verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 move = new Vector3(0, verticalVelocity, 0);
        controller.Move(move * Time.deltaTime);
    }
}
