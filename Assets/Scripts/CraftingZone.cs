using UnityEngine;

public class CraftingZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && InventoryManager.Instance != null)
        {
            Debug.Log("CraftingZone: PLAYER a intrat in zona de craft");
            InventoryManager.Instance.SetNearCraftZone(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && InventoryManager.Instance != null)
        {
            Debug.Log("CraftingZone: PLAYER a iesit din zona de craft");
            InventoryManager.Instance.SetNearCraftZone(false);
        }
    }
}
