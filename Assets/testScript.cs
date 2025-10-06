using UnityEngine;
using UnityEngine.InputSystem;

public class TestScript : MonoBehaviour
{
    [SerializeField] float jumpHeight = 1.5f;
    [SerializeField] float gravity = 9.81f; // positive constant
    [SerializeField] float coyoteTime = 0.1f;

    float vY;
    float groundedTimer;
    CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cc.minMoveDistance = 0f; // important when idle
    }

    void Update()
    {
        // gravity
        if (groundedTimer > 0f && vY < 0f) vY = -2f;
        if (vY > -53f) // terminal velocity clamp
            vY -= gravity * Time.deltaTime;

        // move (add your horizontal delta to x/z here)
        Vector3 delta = new Vector3(0f, vY, 0f) * Time.deltaTime;
        cc.Move(delta);

        // update grounded AFTER Move
        if (cc.isGrounded) groundedTimer = coyoteTime;
        else groundedTimer -= Time.deltaTime;
    }

    void OnJump(InputValue _)
    {
        if (groundedTimer > 0f)
            vY = Mathf.Sqrt(2f * jumpHeight * gravity);
    }
}