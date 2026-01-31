using UnityEngine;

public class KeepRigOnGround : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Grounding")]
    public float desiredEyeHeight = 1.65f;
    public float raycastUpOffset = 2f;
    public float raycastDistance = 50f;
    public LayerMask groundMask;

    [Header("Motion Limits")]
    public float maxUpSpeed = 4f;     // m/s
    public float maxDownSpeed = 6f;   // m/s (poate fi mai mare ca să coboare mai rapid)

    [Header("Smoothing")]
    public bool smooth = true;
    public float smoothSpeed = 12f;  // 8–20

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Raycast under camera (X,Z)
        Vector3 origin = new Vector3(
            cameraTransform.position.x,
            cameraTransform.position.y + raycastUpOffset,
            cameraTransform.position.z
        );

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundMask, QueryTriggerInteraction.Ignore))
            return;

        float groundY = hit.point.y;

        // current camera height above ground
        float currentEyeAboveGround = cameraTransform.position.y - groundY;

        // we want camera to be at desiredEyeHeight above ground => adjust rig Y accordingly
        float neededDeltaY = desiredEyeHeight - currentEyeAboveGround;

        // limit how fast we correct up/down (prevents snapping/“flying”)
        float maxDeltaUp = maxUpSpeed * Time.deltaTime;
        float maxDeltaDown = maxDownSpeed * Time.deltaTime;

        float clampedDeltaY = neededDeltaY;
        if (clampedDeltaY > 0f) clampedDeltaY = Mathf.Min(clampedDeltaY, maxDeltaUp);
        else clampedDeltaY = Mathf.Max(clampedDeltaY, -maxDeltaDown);

        Vector3 targetPos = transform.position + new Vector3(0f, clampedDeltaY, 0f);

        if (smooth)
            transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        else
            transform.position = targetPos;
    }
}
