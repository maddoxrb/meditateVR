using UnityEngine;
using Oculus.Interaction;
using System;
using System.Reflection;

public class VRShoot : MonoBehaviour
{
    public SimpleShoot simpleShoot;
    public OVRInput.Button shootButton = OVRInput.Button.PrimaryIndexTrigger;

    [Header("Optional anchors for distance fallback")]
    [SerializeField] private Transform leftHandAnchor;   // e.g., LeftHandAnchor or LeftControllerAnchor
    [SerializeField] private Transform rightHandAnchor;  // e.g., RightHandAnchor or RightControllerAnchor

    private Grabbable grabbable;
    private AudioSource audioSource;

    // track whether we’re held and which hand is holding
    private bool isHeld = false;
    private OVRInput.Controller holdingController;
    private string holdingInteractorName = "";   // best-effort name of the interactor object

    void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        audioSource = GetComponent<AudioSource>();
        LogHold("Awake", holdingController, "initialized");
    }

    void OnEnable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
        LogHold("OnEnable", holdingController, "subscribed to pointer events");
    }

    void OnDisable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        LogHold("OnDisable", holdingController, "unsubscribed from pointer events");
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:   // grabbed
                isHeld = true;

                var interactorController = OVRInput.Controller.None;
                string source = "unknown";
                string interactorName = "";

                // Try to get a direct interactor reference from the PointerEvent if available.
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

                // 1) Handedness property on the interactor component (most robust in newer SDKs)
                if (selectingObj != null && interactorController == OVRInput.Controller.None)
                {
                    interactorController = TryFromHandedness(selectingObj, out source);
                }

                // Attempt to resolve a GameObject for name/component checks
                GameObject interactorGO = ResolveGameObject(selectingObj);
                if (interactorGO != null)
                {
                    interactorName = interactorGO.name;
                }

                // 2) OVRGrabber component name hint (legacy grabber setups)
                if (interactorGO != null && interactorController == OVRInput.Controller.None)
                {
                    interactorController = TryFromOVRGrabber(interactorGO, out source);
                }

                // 3) Name-based fallback (Left/Right in object names)
                if (interactorGO != null && interactorController == OVRInput.Controller.None)
                {
                    interactorController = TryFromName(interactorGO.name, out source);
                }

                // 4) Identifier-based mapping as last resort
                if (interactorController == OVRInput.Controller.None)
                {
                    interactorController = MapInteractorToController(evt.Identifier);
                    if (interactorController != OVRInput.Controller.None)
                        source = $"identifier:{evt.Identifier}";
                }

                // 5) Live input-state fallback: which controller is actively pressed this frame
                if (interactorController == OVRInput.Controller.None)
                {
                    var byButtons = DetectPressingController();
                    if (byButtons != OVRInput.Controller.None)
                    {
                        interactorController = byButtons;
                        source = "buttons";
                    }
                }

                // 6) Distance fallback using optional anchors
                if (interactorController == OVRInput.Controller.None)
                {
                    var byDistance = DetectNearestAnchorTo(transform.position);
                    if (byDistance != OVRInput.Controller.None)
                    {
                        interactorController = byDistance;
                        source = "distance";
                    }
                }

                holdingController = interactorController;
                holdingInteractorName = interactorName;
                LogHold("HandlePointerEvent.Select", holdingController, $"source={source}, interactorName={interactorName}");

                break;

            case PointerEventType.Unselect: // released
                LogHold("HandlePointerEvent.Unselect", holdingController, "released");
                isHeld = false;
                holdingController = OVRInput.Controller.None;
                holdingInteractorName = "";
                break;
        }
    }

    void Update()
    {
        if (!isHeld) return;

        bool pressed = false;

        // If we know which controller is holding the gun, only check that controller's trigger.
        if (holdingController != OVRInput.Controller.None)
        {
            pressed = OVRInput.GetDown(shootButton, holdingController);
        }
        else
        {
            // Fallback: check both hands (legacy behavior)
            pressed = OVRInput.GetDown(shootButton, OVRInput.Controller.LTouch) ||
                      OVRInput.GetDown(shootButton, OVRInput.Controller.RTouch);
        }

        // Ignore other hand even if its trigger is pressed this frame
        if (holdingController == OVRInput.Controller.LTouch)
        {
            if (OVRInput.GetDown(shootButton, OVRInput.Controller.RTouch))
                LogHold("Update.IgnoredOther", OVRInput.Controller.RTouch, "other hand pressed but not holding");
        }
        else if (holdingController == OVRInput.Controller.RTouch)
        {
            if (OVRInput.GetDown(shootButton, OVRInput.Controller.LTouch))
                LogHold("Update.IgnoredOther", OVRInput.Controller.LTouch, "other hand pressed but not holding");
        }

        if (pressed)
        {
            LogHold("Update.Fire", holdingController, $"button={shootButton}, interactorName={holdingInteractorName}");

            if (simpleShoot != null)
                simpleShoot.StartShoot();
            if (audioSource != null)
                audioSource.Play();
        }
    }

    // crude mapping: left = LTouch, right = RTouch
    private OVRInput.Controller MapInteractorToController(int identifier)
    {
        if (identifier == 0) return OVRInput.Controller.LTouch;
        if (identifier == 1) return OVRInput.Controller.RTouch;
        return OVRInput.Controller.None;
    }

    // ===== Helpers =====

    private static GameObject ResolveGameObject(object selectingObj)
    {
        if (selectingObj == null) return null;

        // Try gameObject property directly
        var goProp = selectingObj.GetType().GetProperty("gameObject");
        if (goProp != null)
        {
            var go = goProp.GetValue(selectingObj, null) as GameObject;
            if (go != null) return go;
        }

        // Try transform property
        var transformProp = selectingObj.GetType().GetProperty("transform");
        if (transformProp != null)
        {
            var t = transformProp.GetValue(selectingObj, null) as Transform;
            if (t != null) return t.gameObject;
        }

        // Try component cast
        if (selectingObj is Component c)
            return c.gameObject;

        return null;
    }

    private static OVRInput.Controller TryFromName(string name, out string source)
    {
        source = "name";
        if (string.IsNullOrEmpty(name)) return OVRInput.Controller.None;
        var lower = name.ToLower();
        if (lower.Contains("left") || lower.Contains("l_touch") || lower.Contains("ltouch") || lower.Contains("lcontroller"))
            return OVRInput.Controller.LTouch;
        if (lower.Contains("right") || lower.Contains("r_touch") || lower.Contains("rtouch") || lower.Contains("rcontroller"))
            return OVRInput.Controller.RTouch;
        source = "name-unmatched";
        return OVRInput.Controller.None;
    }

    private static OVRInput.Controller TryFromHandedness(object selectingObj, out string source)
    {
        source = "handedness";
        if (selectingObj == null) return OVRInput.Controller.None;

        // Look for a public property named Handedness on the object
        var prop = selectingObj.GetType().GetProperty("Handedness", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            var val = prop.GetValue(selectingObj, null)?.ToString();
            if (!string.IsNullOrEmpty(val))
            {
                if (val.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0)
                    return OVRInput.Controller.LTouch;
                if (val.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0)
                    return OVRInput.Controller.RTouch;
            }
        }

        // If selectingObj is a Component, also try on its GameObject components
        if (selectingObj is Component comp)
        {
            foreach (var mb in comp.GetComponents<MonoBehaviour>())
            {
                var t = mb.GetType().GetProperty("Handedness", BindingFlags.Public | BindingFlags.Instance);
                if (t != null)
                {
                    var v = t.GetValue(mb, null)?.ToString();
                    if (!string.IsNullOrEmpty(v))
                    {
                        if (v.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0)
                            return OVRInput.Controller.LTouch;
                        if (v.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0)
                            return OVRInput.Controller.RTouch;
                    }
                }
            }
        }

        source = "handedness-unavailable";
        return OVRInput.Controller.None;
    }

    private static OVRInput.Controller TryFromOVRGrabber(GameObject go, out string source)
    {
        source = "ovrgrabber";
        if (go == null) return OVRInput.Controller.None;

        // We can’t reference OVRGrabber type directly without the OVR namespace in this file,
        // so scan components by name.
        var components = go.GetComponents<Component>();
        foreach (var c in components)
        {
            if (c == null) continue;
            var typeName = c.GetType().Name;
            if (typeName == "OVRGrabber")
            {
                // fall back to the GameObject name heuristic once we know we’re on a grabber GO
                var byName = TryFromName(go.name, out _);
                if (byName != OVRInput.Controller.None)
                    return byName;
            }
        }

        source = "ovrgrabber-notfound";
        return OVRInput.Controller.None;
    }

    private static string HandString(OVRInput.Controller c)
    {
        switch (c)
        {
            case OVRInput.Controller.LTouch: return "Left";
            case OVRInput.Controller.RTouch: return "Right";
            default: return "None";
        }
    }

    private void LogHold(string where, OVRInput.Controller controller, string note)
    {
        Debug.LogError($"[VRShoot] {where}: holding={HandString(controller)} controller={controller} note={note}");
    }

    private OVRInput.Controller DetectPressingController()
    {
        // Check common inputs that are down at the grab moment
        bool l =
            OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) ||
            OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) ||
            OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch) ||
            OVRInput.Get(OVRInput.Button.Three, OVRInput.Controller.LTouch);

        bool r =
            OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) ||
            OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) ||
            OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch) ||
            OVRInput.Get(OVRInput.Button.Four, OVRInput.Controller.RTouch);

        if (l && !r) return OVRInput.Controller.LTouch;
        if (r && !l) return OVRInput.Controller.RTouch;
        if (l && r)
        {
            // both pressed; prefer the one whose index trigger is down
            bool lIdx = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            bool rIdx = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            if (lIdx && !rIdx) return OVRInput.Controller.LTouch;
            if (rIdx && !lIdx) return OVRInput.Controller.RTouch;
        }
        return OVRInput.Controller.None;
    }

    private OVRInput.Controller DetectNearestAnchorTo(Vector3 worldPos)
    {
        if (leftHandAnchor == null && rightHandAnchor == null) return OVRInput.Controller.None;

        float dL = float.PositiveInfinity;
        float dR = float.PositiveInfinity;
        if (leftHandAnchor != null) dL = Vector3.SqrMagnitude(worldPos - leftHandAnchor.position);
        if (rightHandAnchor != null) dR = Vector3.SqrMagnitude(worldPos - rightHandAnchor.position);

        if (dL < dR) return OVRInput.Controller.LTouch;
        if (dR < dL) return OVRInput.Controller.RTouch;
        return OVRInput.Controller.None;
    }
}