using UnityEngine;

public class WaterProximityCheck : MonoBehaviour
{
    [Header("Detectie apa (XR Origin)")]
    public LayerMask waterMask;

    [Tooltip("Offset fata de XR Origin (ridica probe-ul la nivelul apei)")]
    public Vector3 pointOffset = new Vector3(0f, 0.9f, 0f);

    [Tooltip("Raza probei – mai mare decat grosimea apei")]
    public float probeRadius = 0.25f;

    public bool logDetails = true;

    public bool NearWater { get; private set; }

    void Update()
    {
        Vector3 probePos = transform.position + pointOffset;

        bool near = Physics.CheckSphere(
            probePos,
            probeRadius,
            waterMask,
            QueryTriggerInteraction.Collide
        );

        if (near != NearWater)
        {
            NearWater = near;

            if (InventoryManager.Instance != null)
                InventoryManager.Instance.SetNearWater(NearWater);

            if (logDetails)
                Debug.Log($"[WaterXR] NearWater={NearWater} probePos={probePos}");
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = NearWater ? Color.green : Color.red;
        Vector3 probePos = transform.position + pointOffset;
        Gizmos.DrawWireSphere(probePos, probeRadius);
    }
#endif
}
