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
    public Animator leftHandAnimator, rightHandAnimator;
    public InputActionProperty LeftActivateAction, LeftGripAction, RightActivateAction, RightGripAction;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            // Map XR rig references
            headRig = GameObject.Find("[BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor").transform;
            leftHandRig = GameObject.Find("[BuildingBlock] Camera Rig/TrackingSpace/LeftHandAnchor/LeftControllerAnchor").transform;
            rightHandRig = GameObject.Find("[BuildingBlock] Camera Rig/TrackingSpace/RightHandAnchor/RightControllerAnchor").transform;
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
            MapPoseToRig(head, headRig);
            MapPoseToRig(leftHand, leftHandRig);
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
        rig.position = target.position;
        rig.rotation = target.rotation;

        if (target == headRig)
        {
            Vector3 GroundContactPostition = target.position;
            Vector3 HeadRotation = target.rotation.eulerAngles;
            HeadRotation.x = 0;
            HeadRotation.z = 0;
            if (HeadRotation.y > 180.0f)
                HeadRotation.y -= 360.0f;

            GroundContactPostition.y = 0;
            GroundContact.transform.position = GroundContactPostition;
            GroundContact.transform.rotation = Quaternion.Euler(HeadRotation);
            
            Neck.transform.rotation = Quaternion.Euler(HeadRotation);
            Vector3 offset = new Vector3(target.position.x, target.position.y + (NeckLocalPosition.y), target.position.z);
            Neck.transform.position = offset;
        }
    }
}
