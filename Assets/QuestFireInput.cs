using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using TheDeveloperTrain.SciFiGuns;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class QuestFireInput : MonoBehaviour
{
    [SerializeField] private Gun gun;
    [SerializeField] private InputActionReference fireAction;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool isHeld;

    private void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (fireAction != null)
        {
            fireAction.action.Enable();
            fireAction.action.performed += OnFire;
        }

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        if (fireAction != null)
        {
            fireAction.action.performed -= OnFire;
            fireAction.action.Disable();
        }

        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args) => isHeld = true;
    private void OnRelease(SelectExitEventArgs args) => isHeld = false;

    private void OnFire(InputAction.CallbackContext ctx)
    {
        if (isHeld && gun != null)
            gun.Shoot();
    }
}