using UnityEngine;
using Fusion;
using Oculus.Interaction;
using UnityEngine.InputSystem;

[RequireComponent(typeof(InteractableUnityEventWrapper))]
public class BulletSpawner : NetworkBehaviour
{
    [Header("Bullet Prefab to Spawn")]
    public GameObject BulletPrefab;
    private InteractableUnityEventWrapper interactable;
    [Header("Reference to Grab Interactable object")]
    public GrabInteractable grabInteractable;
    [Header("Input Action Property for Trigger Button")]
    public InputActionProperty leftActivateAction, rightActivateAction;
    private bool isPressed = false;
    [Header("Speed of the Bullet")]
    public float bulletSpeed = 10f;

    private void Awake()
    {
        interactable = GetComponent<InteractableUnityEventWrapper>();
        grabInteractable = this.transform.GetChild(0).GetComponent<GrabInteractable>();
        interactable.InjectInteractableView(grabInteractable);
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.WhenSelect.AddListener(OnSelectEntered);
            interactable.WhenUnselect.AddListener(OnSelectExited);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.WhenSelect.RemoveListener(OnSelectEntered);
            interactable.WhenUnselect.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered()
    {
        // Get the local player's PlayerRef
        var localPlayer = Runner.LocalPlayer;
        if (localPlayer == PlayerRef.None) return;

        if (!isPressed)
            isPressed = true;
    }

    private void OnSelectExited()
    {
        // Only the current owner should reset the color
        if (Object.HasStateAuthority)
        {
            if (isPressed)
                isPressed = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;  // Prevent spawning without authority

        float leftTriggerInputValue = leftActivateAction.action.ReadValue<float>();
        float rightTriggerInputValue = rightActivateAction.action.ReadValue<float>();
        float triggerInputValue = Mathf.Max(leftTriggerInputValue, rightTriggerInputValue);

        if (isPressed && triggerInputValue > 0.1f)
        {
            Vector3 spawnPos = this.transform.position + this.transform.forward * 0.05f;
            Quaternion spawnRot = this.transform.rotation;

            // Spawn the bullet with input authority to the local player
            var bullet = Runner.Spawn(BulletPrefab, spawnPos, spawnRot, inputAuthority: Runner.LocalPlayer);
            bullet.GetComponent<Rigidbody>().linearVelocity = this.transform.forward * bulletSpeed;
        }
    }
}
