using UnityEngine;
using Oculus.Interaction;
public class testGrab : MonoBehaviour
{
    private Grabbable grabbable;

    void Awake()
    {
        grabbable = GetComponent<Grabbable>();
    }

    void OnEnable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    void OnDisable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                Debug.LogError("[GrabCheck] Grabbed!");
                break;
            case PointerEventType.Unselect:
                Debug.LogError("[GrabCheck] Released!");
                break;
        }
    }
}
