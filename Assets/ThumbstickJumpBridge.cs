using UnityEngine;
using StarterAssets;   // so we can access StarterAssetsInputs
using Oculus;         // OVRInput

public class ThumbstickJumpBridge : MonoBehaviour
{
    public StarterAssetsInputs input;  // drag your StarterAssetsInputs component here

    void Update()
    {
        // left stick click
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
            input.JumpInput(true);

        // right stick click
        if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
            input.JumpInput(true);
    }
}