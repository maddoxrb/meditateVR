using UnityEngine;
using Fusion;
using Photon.Voice.Fusion;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(VoiceNetworkObject))]
public class FusionNetworkPlayerSimple : NetworkBehaviour
{
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    private Transform headRig;
    private Transform leftHandRig;
    private Transform rightHandRig;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            // Map XR rig references
            headRig = GameObject.Find("[BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor").transform;
            leftHandRig = GameObject.Find("[BuildingBlock] Camera Rig/TrackingSpace/LeftHandAnchor/LeftControllerAnchor").transform;
            rightHandRig = GameObject.Find("[BuildingBlock] Camera Rig/TrackingSpace/RightHandAnchor/RightControllerAnchor").transform;

            // --- Hide all renderers on head, hands, and neck for the local player ---
            DisableAllRenderers(head);
            DisableAllRenderers(leftHand);
            DisableAllRenderers(rightHand);
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
        }
    }

    private void MapPoseToRig(Transform rig, Transform target)
    {
        rig.position = target.position;
        rig.rotation = target.rotation;
    }
}
