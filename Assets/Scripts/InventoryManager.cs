using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private SurvivalManager survivalManager;

    // --- NOU: Limita maximă de resurse ---
    private const int MAX_RESOURCE_LIMIT = 10;

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
    public Button eatButton;

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

    public void ConsumeFoodItem()
    {
        if (foodCount > 0)
        {
            foodCount--;

            if (survivalManager != null)
            {
                survivalManager.ConsumeFood();
            }

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

    // --- MODIFICARE PRINCIPALĂ AICI ---
    private void AddCurrentItemToInventory()
    {
        // Cazul 1: Colectam un obiect fizic (Stick, Stone, Food, Water Bottle)
        if (currentItem != null)
        {
            bool itemAdded = false; // Verificam daca am reusit sa adaugam

            switch (currentItem.itemType)
            {
                case ItemType.Stick:
                    if (stickCount < MAX_RESOURCE_LIMIT)
                    {
                        stickCount++;
                        itemAdded = true;
                    }
                    break;
                case ItemType.Stone:
                    if (stoneCount < MAX_RESOURCE_LIMIT)
                    {
                        stoneCount++;
                        itemAdded = true;
                    }
                    break;
                case ItemType.Food:
                    if (foodCount < MAX_RESOURCE_LIMIT)
                    {
                        foodCount++;
                        itemAdded = true;
                    }
                    break;
                case ItemType.Water:
                    if (waterCount < MAX_RESOURCE_LIMIT)
                    {
                        waterCount++;
                        itemAdded = true;
                    }
                    break;
            }

            if (itemAdded)
            {
                UpdateInventoryUI();
                // Distrugem obiectul doar daca l-am adaugat in inventar
                Destroy(currentItem.gameObject);
                currentItem = null;
                Debug.Log("Obiect adaugat in inventar.");
            }
            else
            {
                Debug.Log("Inventar PLIN pentru acest tip de resursă! (Max " + MAX_RESOURCE_LIMIT + ")");
                // Aici poti adauga un mesaj pe ecran pentru jucator (ex: MessageDirector)
            }
        }
        // Cazul 2: Luam apa din lac/rau (fara obiect fizic)
        else if (isNearWater)
        {
            if (waterCount < MAX_RESOURCE_LIMIT)
            {
                waterCount++;
                UpdateInventoryUI();
                Debug.Log("Apa colectata din sursa.");
            }
            else
            {
                Debug.Log("Nu mai poti cara apa! (Max " + MAX_RESOURCE_LIMIT + ")");
            }
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
        if (stickCountText != null) stickCountText.text = "Sticks: " + stickCount + "/" + MAX_RESOURCE_LIMIT;
        if (stoneCountText != null) stoneCountText.text = "Stones: " + stoneCount + "/" + MAX_RESOURCE_LIMIT;
        if (foodCountText != null) foodCountText.text = "Food: " + foodCount + "/" + MAX_RESOURCE_LIMIT;
        if (waterCountText != null) waterCountText.text = "Water: " + waterCount + "/" + MAX_RESOURCE_LIMIT;
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