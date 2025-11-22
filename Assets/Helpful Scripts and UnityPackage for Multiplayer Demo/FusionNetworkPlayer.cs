using UnityEngine;
using Fusion;
using Photon.Voice.Fusion;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(VoiceNetworkObject))]
public class FusionNetworkPlayer : NetworkBehaviour
{
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public GameObject GroundContact, Neck;
    private Vector3 NeckLocalPosition;

    private Transform headRig;
    private Transform leftHandRig;
    private Transform rightHandRig;
    private Transform cameraRigRoot;
    public Animator leftHandAnimator, rightHandAnimator;
    public InputActionProperty LeftActivateAction, LeftGripAction, RightActivateAction, RightGripAction;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            // Map XR rig references
            var headRigObj = GameObject.Find("[BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor");
            var leftHandRigObj = GameObject.Find("[BuildingBlock] Camera Rig/TrackingSpace/LeftHandAnchor/LeftControllerAnchor");
            var rightHandRigObj = GameObject.Find("[BuildingBlock] Camera Rig/TrackingSpace/RightHandAnchor/RightControllerAnchor");
            var cameraRigRootObj = GameObject.Find("[BuildingBlock] Camera Rig");

            // Null checks with error logging
            if (headRigObj == null)
                Debug.LogError("[FusionNetworkPlayer] Could not find CenterEyeAnchor. Avatar head tracking will not work.");
            else
                headRig = headRigObj.transform;

            if (leftHandRigObj == null)
                Debug.LogError("[FusionNetworkPlayer] Could not find LeftControllerAnchor. Left hand tracking will not work.");
            else
                leftHandRig = leftHandRigObj.transform;

            if (rightHandRigObj == null)
                Debug.LogError("[FusionNetworkPlayer] Could not find RightControllerAnchor. Right hand tracking will not work.");
            else
                rightHandRig = rightHandRigObj.transform;

            if (cameraRigRootObj == null)
                Debug.LogError("[FusionNetworkPlayer] Could not find Camera Rig root. Avatar positioning may be incorrect.");
            else
                cameraRigRoot = cameraRigRootObj.transform;

            NeckLocalPosition = Neck.transform.localPosition;

            // --- Hide all renderers on head, hands, and neck for the local player ---
            DisableAllRenderers(head);
            DisableAllRenderers(leftHand);
            DisableAllRenderers(rightHand);
            DisableAllRenderers(Neck.transform);
        }
    }

    private void DisableAllRenderers(Transform root)
    {
        // includeInactive:true makes sure we catch children that might be disabled
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            // Debug.Log($"Disabling renderer: {r.gameObject.name}");
            r.enabled = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority)
        {
            // Only update tracking if rig references are valid
            if (headRig != null)
                MapPoseToRig(head, headRig);
            if (leftHandRig != null)
                MapPoseToRig(leftHand, leftHandRig);
            if (rightHandRig != null)
                MapPoseToRig(rightHand, rightHandRig);

            UpdateHandAnimation(leftHand, 1);
            UpdateHandAnimation(rightHand, 2);
        }
    }

    private void UpdateHandAnimation(Transform Hand, int value)
    {
        float triggervalue = 0;
        float gripValue = 0;

        if (Hand == leftHand)
        {
            triggervalue = LeftActivateAction.action.ReadValue<float>();
            gripValue = LeftGripAction.action.ReadValue<float>();
        }
        else
        {
            triggervalue = RightActivateAction.action.ReadValue<float>();
            gripValue = RightGripAction.action.ReadValue<float>();
        }

        TriggerAndGripSetter(triggervalue, gripValue, value);
    }

    private void TriggerAndGripSetter(float triggervalue, float gripValue, int value)
    {
        Animator handAnimator = null;

        if (value == 1)
            handAnimator = leftHandAnimator;
        else if (value == 2)
            handAnimator = rightHandAnimator;

        if (triggervalue > 0)
            handAnimator.SetFloat("Trigger", triggervalue);
        else
            handAnimator.SetFloat("Trigger", 0);

        if (gripValue > 0)
            handAnimator.SetFloat("Grip", gripValue);
        else
            handAnimator.SetFloat("Grip", 0);
    }

    private void MapPoseToRig(Transform rig, Transform target)
    {
        if (rig == null || target == null)
            return;

        rig.position = target.position;
        rig.rotation = target.rotation;

        // Update ground contact position based on head position
        if (target == headRig)
        {
            Vector3 GroundContactPostition = target.position;
            Vector3 HeadRotation = target.rotation.eulerAngles;
            HeadRotation.x = 0;
            HeadRotation.z = 0;
            if (HeadRotation.y > 180.0f)
                HeadRotation.y -= 360.0f;

            // Align avatar root with camera rig base position, fallback to world y=0
            GroundContactPostition.y = cameraRigRoot != null ? cameraRigRoot.position.y : 0;
            GroundContact.transform.position = GroundContactPostition;
            GroundContact.transform.rotation = Quaternion.Euler(HeadRotation);
            
            Neck.transform.rotation = Quaternion.Euler(HeadRotation);
            Vector3 offset = new Vector3(target.position.x, target.position.y + (NeckLocalPosition.y), target.position.z);
            Neck.transform.position = offset;
        }
    }
}
