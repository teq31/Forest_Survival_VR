using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
    [Tooltip("Cât timp păstrăm nearWater = true după ultimul contact")]
    public float graceTime = 0.25f;

    private float lastSeenTime = -999f;

    private void OnTriggerStay(Collider other)
    {
        // IMPORTANT: la XR, uneori collider-ul e copil -> root
        if (!other.transform.root.CompareTag("Player")) return;

        lastSeenTime = Time.time;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.SetNearWater(true);
    }

    private void Update()
    {
        // dacă n-am mai fost în trigger de graceTime, atunci false
        if (Time.time - lastSeenTime > graceTime)
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.SetNearWater(false);
        }
    }
}
