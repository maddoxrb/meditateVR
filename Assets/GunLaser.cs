using UnityEngine;

public class GunLaser : MonoBehaviour
{
    [Header("Refs")]
    public Transform muzzle;                 // barrel tip
    public LineRenderer line;               // 2 positions, world space

    [Header("Laser")]
    public float maxDistance = 30f;
    public LayerMask hitMask = ~0;          // everything by default
    public float endSmooth = 0.0f;          // 0 = no smoothing, 0.1–0.2 to damp jitter

    [Header("Optional dot")]
    public Transform dot;                   // small red quad/sprite; optional
    public float dotSize = 0.01f;

    Vector3 _endPos;

    void Reset()
    {
        line = GetComponent<LineRenderer>();
        if (!muzzle) muzzle = transform;
    }

    void OnEnable()
    {
        if (line)
        {
            line.positionCount = 2;
            line.enabled = true;
        }
    }

    void Update()
    {
        if (!muzzle || !line) return;

        Vector3 start = muzzle.position;
        Vector3 dir = muzzle.forward;

        Vector3 targetEnd;
        if (Physics.Raycast(start, dir, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            targetEnd = hit.point;
        }
        else
        {
            targetEnd = start + dir * maxDistance;
        }

        // Smooth end to reduce VR jitter (optional)
        if (endSmooth > 0f)
            _endPos = Vector3.Lerp(_endPos == Vector3.zero ? targetEnd : _endPos, targetEnd, 1f - Mathf.Exp(-Time.deltaTime / endSmooth));
        else
            _endPos = targetEnd;

        line.SetPosition(0, start);
        line.SetPosition(1, _endPos);

        if (dot)
        {
            dot.position = _endPos;
            dot.localScale = Vector3.one * dotSize;
            // Face the camera for a crisp dot
            if (Camera.main) dot.forward = -Camera.main.transform.forward;
            dot.gameObject.SetActive(true);
        }
    }
}