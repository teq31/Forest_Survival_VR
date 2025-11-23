using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class CollectibleItem : MonoBehaviour
{
    public ItemType itemType;  

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    private void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RegisterGrabbedItem(this);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ClearCurrentItem(this);
    }
}
