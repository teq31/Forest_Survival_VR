using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private SurvivalManager survivalManager;

    [Header("UI References")]
    public GameObject inventoryPanel;
    public Button openInventoryButton;
    public Button addToInventoryButton;
    public Button closeInventoryButton;

    public Button craftButton;
    public GameObject craftPanel;
    public Button craftCampfireButton;
    public Button craftTorchButton;

    [Header("Consumables UI")]
    public Button drinkButton;
    public Button eatButton; // NOU: Butonul pentru mancare

    [Header("Crafting Prefabs & Spawn Points")]
    public GameObject campfirePrefab;
    public Transform campfireSpawnPoint;

    public GameObject torchPrefab;
    public Transform torchHandPoint;

    public TMP_Text stickCountText;
    public TMP_Text stoneCountText;
    public TMP_Text foodCountText;
    public TMP_Text waterCountText;

    private int stickCount = 0;
    private int stoneCount = 0;
    private int foodCount = 0;
    private int waterCount = 0;
    private int craftedItemsCount = 0;

    private CollectibleItem currentItem;

    private bool isNearWater = false;
    private bool isNearCraftZone = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        survivalManager = FindObjectOfType<SurvivalManager>();
        if (survivalManager == null) Debug.LogError("SurvivalManager nu a fost gasit in scena!");

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (addToInventoryButton != null) addToInventoryButton.gameObject.SetActive(false);
        if (craftPanel != null) craftPanel.SetActive(false);
        if (craftButton != null) craftButton.gameObject.SetActive(false);

        if (openInventoryButton != null) openInventoryButton.onClick.AddListener(OpenInventory);
        if (closeInventoryButton != null) closeInventoryButton.onClick.AddListener(CloseInventory);
        if (addToInventoryButton != null) addToInventoryButton.onClick.AddListener(AddCurrentItemToInventory);
        
        if (craftButton != null) craftButton.onClick.AddListener(ToggleCraftPanel);
        if (craftCampfireButton != null) craftCampfireButton.onClick.AddListener(CraftCampfire);
        if (craftTorchButton != null) craftTorchButton.onClick.AddListener(CraftTorch);

        if (drinkButton != null) drinkButton.onClick.AddListener(ConsumeWaterItem);
        // NOU: Conectam butonul de mancare
        if (eatButton != null) eatButton.onClick.AddListener(ConsumeFoodItem);

        UpdateInventoryUI();
    }

    public void ConsumeWaterItem()
    {
        if (waterCount > 0)
        {
            waterCount--;

            if (survivalManager != null)
            {
                survivalManager.ConsumeWater();
            }

            UpdateInventoryUI();
            Debug.Log("Ai baut apa! Water count: " + waterCount);
        }
        else
        {
            Debug.Log("Nu ai apa in inventar!");
        }
    }

    // NOU: Logica pentru mancat
    public void ConsumeFoodItem()
    {
        if (foodCount > 0)
        {
            // 1. Scadem mancarea
            foodCount--;

            // 2. Apelam SurvivalManager pentru a creste bara de Hunger
            if (survivalManager != null)
            {
                survivalManager.ConsumeFood();
            }

            // 3. Actualizam UI
            UpdateInventoryUI();
            Debug.Log("Ai mancat! Food count: " + foodCount);
        }
        else
        {
            Debug.Log("Nu ai mancare in inventar!");
        }
    }

    public void RegisterGrabbedItem(CollectibleItem item)
    {
        currentItem = item;
        UpdateAddButtonVisibility();
    }

    public void ClearCurrentItem(CollectibleItem item)
    {
        if (currentItem == item)
        {
            currentItem = null;
            UpdateAddButtonVisibility();
        }
    }

    public void SetNearWater(bool value)
    {
        isNearWater = value;
        UpdateAddButtonVisibility();
    }

    public void SetNearCraftZone(bool value)
    {
        isNearCraftZone = value;
        UpdateCraftButtonVisibility();
    }

    private void UpdateCraftButtonVisibility()
    {
        if (craftButton == null) return;
        craftButton.gameObject.SetActive(isNearCraftZone);
    }

    private void UpdateAddButtonVisibility()
    {
        if (addToInventoryButton == null) return;
        bool shouldShow = (currentItem != null) || isNearWater;
        addToInventoryButton.gameObject.SetActive(shouldShow);
    }

    private void AddCurrentItemToInventory()
    {
        if (currentItem != null)
        {
            switch (currentItem.itemType)
            {
                case ItemType.Stick: stickCount++; break;
                case ItemType.Stone: stoneCount++; break;
                case ItemType.Food: foodCount++; break;
                case ItemType.Water: waterCount++; break;
            }
            UpdateInventoryUI();
            Destroy(currentItem.gameObject);
            currentItem = null;
        }
        else if (isNearWater)
        {
            waterCount++;
            UpdateInventoryUI();
        }
        UpdateAddButtonVisibility();
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        bool newState = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(newState);
    }

    public void OpenInventory()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    private void UpdateInventoryUI()
    {
        if (stickCountText != null) stickCountText.text = "Sticks: " + stickCount;
        if (stoneCountText != null) stoneCountText.text = "Stones: " + stoneCount;
        if (foodCountText != null) foodCountText.text = "Food: " + foodCount;
        if (waterCountText != null) waterCountText.text = "Water: " + waterCount;
    }

    private void ToggleCraftPanel()
    {
        if (craftPanel == null) return;
        craftPanel.SetActive(!craftPanel.activeSelf);
    }
 
    private const int CAMPFIRE_STICKS_REQUIRED = 2;
    private const int CAMPFIRE_STONES_REQUIRED = 2;
    private const int TORCH_STICKS_REQUIRED = 1;
    private const int TORCH_STONES_REQUIRED = 1;

    private void CraftCampfire()
    {
        if (stickCount < CAMPFIRE_STICKS_REQUIRED || stoneCount < CAMPFIRE_STONES_REQUIRED)
        {
            Debug.Log("Nu ai destule resurse pentru CAMPFIRE!");
            return;
        }
        if (campfirePrefab == null || campfireSpawnPoint == null)
        {
            Debug.LogWarning("Campfire prefab lipseste!");
            return;
        }
        stickCount -= CAMPFIRE_STICKS_REQUIRED;
        stoneCount -= CAMPFIRE_STONES_REQUIRED;
        craftedItemsCount++;
        UpdateInventoryUI();
        Instantiate(campfirePrefab, campfireSpawnPoint.position, campfireSpawnPoint.rotation);
        Debug.Log("Campfire craftuit cu succes!");
    }

    private void CraftTorch()
    {
        if (stickCount < TORCH_STICKS_REQUIRED || stoneCount < TORCH_STONES_REQUIRED)
        {
            Debug.Log("Nu ai destule resurse pentru TORCH!");
            return;
        }
        if (torchPrefab == null || torchHandPoint == null)
        {
            Debug.LogWarning("Torch prefab lipseste!");
            return;
        }
        stickCount -= TORCH_STICKS_REQUIRED;
        stoneCount -= TORCH_STONES_REQUIRED;
        craftedItemsCount++;
        UpdateInventoryUI();
        GameObject torchInstance = Instantiate(torchPrefab, torchHandPoint.position, torchHandPoint.rotation);
        torchInstance.transform.SetParent(torchHandPoint, true);
        Debug.Log("Torch craftuită și pusă în mână!");
    }

    public int GetStickCount() { return stickCount; }
    public int GetStoneCount() { return stoneCount; }
    public int GetFoodCount() { return foodCount; }
    public int GetWaterCount() { return waterCount; }
    public int GetCraftedItemsCount() { return craftedItemsCount; }
}