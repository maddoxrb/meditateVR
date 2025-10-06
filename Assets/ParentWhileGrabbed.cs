using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(Rigidbody))]
public class ParentWhileGrabbed : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;

    private Transform originalParent;
    private Rigidbody rb;
    private Transform follow;   // interactor/hand anchor we parent to

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!grabbable) grabbable = GetComponent<Grabbable>();
    }

    // Hook these from Grabbable/Interactor events in the Inspector:
    public void OnGrabbed(Transform interactorTransform)
    {
        originalParent = transform.parent;
        follow = interactorTransform;              // e.g., interactor's grab anchor
        rb.isKinematic = true;                     // avoid physics lag
        transform.SetParent(follow, true);         // parent to hand
        // keep interpolation on
    }

    public void OnReleased(Vector3 releaseVel, Vector3 releaseAngVel)
    {
        transform.SetParent(originalParent, true);
        rb.isKinematic = false;
        rb.linearVelocity = releaseVel;                  // hand linear velocity
        rb.angularVelocity = releaseAngVel;        // hand angular velocity
        follow = null;
    }
}