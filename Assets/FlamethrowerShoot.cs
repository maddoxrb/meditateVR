using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(ParticleSystem))]
public class FlamethrowerShoot : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private OVRInput.Button shootButton = OVRInput.Button.PrimaryIndexTrigger;
    [Tooltip("Optional keyboard key to fire for testing without VR controllers.")]
    [SerializeField] private KeyCode keyboardFireKey = KeyCode.P;

    [Header("Particles")]
    [SerializeField] private ParticleSystem flameParticles;

    [Header("Optional anchors for distance fallback")]
    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private Transform rightHandAnchor;

    private Grabbable grabbable;
    private OVRInput.Controller holdingController = OVRInput.Controller.None;
    private bool isHeld;
    private bool isEmitting;

    private void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        if (flameParticles == null)
        {
            flameParticles = GetComponent<ParticleSystem>();
        }

        if (flameParticles != null)
        {
            flameParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnEnable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }
    }

    private void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }

        StopEmission();
    }

    private void Update()
    {
        bool wantsEmit = false;

        if (isHeld)
        {
            if (holdingController != OVRInput.Controller.None)
            {
                wantsEmit = OVRInput.Get(shootButton, holdingController);
            }
            else
            {
                bool leftPressed = OVRInput.Get(shootButton, OVRInput.Controller.LTouch);
                bool rightPressed = OVRInput.Get(shootButton, OVRInput.Controller.RTouch);
                wantsEmit = leftPressed || rightPressed;
            }
        }

        if (!wantsEmit && keyboardFireKey != KeyCode.None)
        {
            wantsEmit = Input.GetKey(keyboardFireKey);
        }

        if (wantsEmit)
        {
            StartEmission();
        }
        else
        {
            StopEmission();
        }
    }

    private void StartEmission()
    {
        if (isEmitting || flameParticles == null)
            return;

        flameParticles.Play();
        isEmitting = true;
    }

    private void StopEmission()
    {
        if (!isEmitting || flameParticles == null)
            return;

        flameParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        isEmitting = false;
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                isHeld = true;
                holdingController = ResolveControllerFromPointer(evt);
                break;
            case PointerEventType.Unselect:
                isHeld = false;
                holdingController = OVRInput.Controller.None;
                StopEmission();
                break;
        }
    }

    private OVRInput.Controller ResolveControllerFromPointer(PointerEvent evt)
    {
        var evtType = evt.GetType();
        var selectingProp = evtType.GetProperty("SelectingInteractor");
        var interactorProp = evtType.GetProperty("Interactor");
        object selectingObj = null;

        if (selectingProp != null)
        {
            selectingObj = selectingProp.GetValue(evt, null);
        }
        else if (interactorProp != null)
        {
            selectingObj = interactorProp.GetValue(evt, null);
        }

        OVRInput.Controller controller = TryFromHandedness(selectingObj);
        if (controller != OVRInput.Controller.None)
            return controller;

        GameObject interactorGo = ResolveGameObject(selectingObj);
        if (interactorGo != null)
        {
            controller = TryFromName(interactorGo.name);
            if (controller != OVRInput.Controller.None)
                return controller;
        }

        controller = MapInteractorToController(evt.Identifier);
        if (controller != OVRInput.Controller.None)
            return controller;

        return DetectNearestAnchorTo(transform.position);
    }

    private static GameObject ResolveGameObject(object selectingObj)
    {
        if (selectingObj == null)
            return null;

        var goProp = selectingObj.GetType().GetProperty("gameObject");
        if (goProp != null)
        {
            var go = goProp.GetValue(selectingObj, null) as GameObject;
            if (go != null)
                return go;
        }

        var transformProp = selectingObj.GetType().GetProperty("transform");
        if (transformProp != null)
        {
            var t = transformProp.GetValue(selectingObj, null) as Transform;
            if (t != null)
                return t.gameObject;
        }

        if (selectingObj is Component comp)
            return comp.gameObject;

        return null;
    }

    private static OVRInput.Controller TryFromHandedness(object selectingObj)
    {
        if (selectingObj == null)
            return OVRInput.Controller.None;

        var prop = selectingObj.GetType().GetProperty("Handedness", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (prop != null)
        {
            var val = prop.GetValue(selectingObj, null)?.ToString();
            if (!string.IsNullOrEmpty(val))
            {
                if (val.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return OVRInput.Controller.LTouch;
                if (val.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return OVRInput.Controller.RTouch;
            }
        }

        if (selectingObj is Component comp)
        {
            foreach (var behaviour in comp.GetComponents<MonoBehaviour>())
            {
                var handednessProp = behaviour.GetType().GetProperty("Handedness", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (handednessProp != null)
                {
                    var val = handednessProp.GetValue(behaviour, null)?.ToString();
                    if (!string.IsNullOrEmpty(val))
                    {
                        if (val.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return OVRInput.Controller.LTouch;
                        if (val.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return OVRInput.Controller.RTouch;
                    }
                }
            }
        }

        return OVRInput.Controller.None;
    }

    private static OVRInput.Controller TryFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return OVRInput.Controller.None;

        var lower = name.ToLower();
        if (lower.Contains("left"))
            return OVRInput.Controller.LTouch;
        if (lower.Contains("right"))
            return OVRInput.Controller.RTouch;

        return OVRInput.Controller.None;
    }

    private static OVRInput.Controller MapInteractorToController(int identifier)
    {
        if (identifier == 0)
            return OVRInput.Controller.LTouch;
        if (identifier == 1)
            return OVRInput.Controller.RTouch;
        return OVRInput.Controller.None;
    }

    private OVRInput.Controller DetectNearestAnchorTo(Vector3 worldPos)
    {
        if (leftHandAnchor == null && rightHandAnchor == null)
            return OVRInput.Controller.None;

        float leftDist = float.PositiveInfinity;
        float rightDist = float.PositiveInfinity;

        if (leftHandAnchor != null)
            leftDist = Vector3.SqrMagnitude(worldPos - leftHandAnchor.position);
        if (rightHandAnchor != null)
            rightDist = Vector3.SqrMagnitude(worldPos - rightHandAnchor.position);

        if (leftDist < rightDist)
            return OVRInput.Controller.LTouch;
        if (rightDist < leftDist)
            return OVRInput.Controller.RTouch;

        return OVRInput.Controller.None;
    }
}
