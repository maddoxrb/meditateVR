using StarterAssets;
using UnityEngine;

public class AstronautAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Tuning")]
    [Tooltip("Horizontal speed (m/s) above which the running animation should play.")]
    [SerializeField] private float runSpeedThreshold = 0.25f;
    [Tooltip("Extra time to keep jump animation active after regaining ground contact (seconds).")]
    [SerializeField] private float landingBufferDuration = 0.05f;

    private CharacterController characterController;
    private FirstPersonController firstPersonController;

    private float landingBufferTimer;
    private bool initializedAnimator;
    private bool previousGrounded = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        firstPersonController = GetComponent<FirstPersonController>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isJumping", false);
            initializedAnimator = true;
        }
    }

    private void Update()
    {
        if (!initializedAnimator)
        {
            return;
        }

        bool grounded = EvaluateGrounded();
        float horizontalSpeed = GetHorizontalSpeed();

        bool isRunning = grounded && horizontalSpeed > runSpeedThreshold;
        animator.SetBool("isRunning", isRunning);

        if (grounded)
        {
            landingBufferTimer += Time.deltaTime;

            if (!previousGrounded)
            {
                landingBufferTimer = 0f;
            }

            if (landingBufferTimer >= landingBufferDuration)
            {
                animator.SetBool("isJumping", false);
            }
        }
        else
        {
            landingBufferTimer = 0f;
            animator.SetBool("isJumping", true);
        }

        previousGrounded = grounded;
    }

    private bool EvaluateGrounded()
    {
        if (firstPersonController != null)
        {
            return firstPersonController.Grounded;
        }

        if (characterController != null)
        {
            return characterController.isGrounded;
        }

        return Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, out _, 0.1f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
    }

    private float GetHorizontalSpeed()
    {
        if (characterController != null)
        {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }

        return 0f;
    }

}
