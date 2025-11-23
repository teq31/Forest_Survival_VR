using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
       

        if (other.CompareTag("Player") && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetNearWater(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
       

        if (other.CompareTag("Player") && InventoryManager.Instance != null)
        {
          
            InventoryManager.Instance.SetNearWater(false);
        }
    }
}
