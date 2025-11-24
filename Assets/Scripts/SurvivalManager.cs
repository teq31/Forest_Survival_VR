using UnityEngine;
using UnityEngine.UI;

public class SurvivalManager : MonoBehaviour
{
    // Conecteaza aceste Slider-e din Inspector
    [Header("Indicatori UI (Slidere)")]
    // Health Bar a fost eliminat
    public Slider hungerBar; // Foame (Saturatie)
    public Slider thirstBar; // Sete (Hidratare)
    public Slider panicBar;  // Panica (Calm)

    [Header("Valori Curente (100 = Plin/Bine, 0 = Esec)")]
    // Health a fost eliminat
    public float currentHunger = 100f; // Incepe plin (saturatie)
    public float currentThirst = 100f; // Incepe plin (hidratare)
    public float currentPanic = 100f;  // Incepe plin (calm) 

    [Header("Ritmul de Degradare (pe secunda)")]
    public float hungerRate = 0.2f;  // Cat scade Saturatia pe secunda
    public float thirstRate = 0.3f;  // Cat scade Hidratarea pe secunda (mai rapida)
    public float panicRate = 0.4f;   // Cat scade Calm-ul pe secunda (noaptea/fara foc)

    [Header("Valori de Restaurare")]
    public float foodRestoreValue = 25f;  // Cat adauga mancarea
    public float waterRestoreValue = 30f; // Cat adauga apa
    public float fireCalmValue = 1f;      // Cat adauga focul la Calm pe secunda 

    [Header("Conditii Joc")]
    public bool isNight = true; // Setat pe true pentru a testa degradarea initiala

    void Start()
    {
        SetMaxValues();
        UpdateUI();
    }

    void SetMaxValues()
    {
        // Setam maximul doar pentru barele ramase
        if (hungerBar != null) hungerBar.maxValue = 100f;
        if (thirstBar != null) thirstBar.maxValue = 100f;
        if (panicBar != null) panicBar.maxValue = 100f;
    }

    void Update()
    {
        // 1. Degradarea continua
        currentHunger -= hungerRate * Time.deltaTime;
        currentThirst -= thirstRate * Time.deltaTime;

        // 2. Degradarea Calmului (Panica): Scade doar daca este noapte
        if (isNight)
        {
            currentPanic -= panicRate * Time.deltaTime;
        }

        // 3. Limitare valori intre 0 si 100
        currentHunger = Mathf.Clamp(currentHunger, 0f, 100f);
        currentThirst = Mathf.Clamp(currentThirst, 0f, 100f);
        currentPanic = Mathf.Clamp(currentPanic, 0f, 100f);

        // 4. Update vizual
        UpdateUI();

        // 5. Verificare esec
        CheckFailureConditions();
    }

    void UpdateUI()
    {
        if (hungerBar != null) hungerBar.value = currentHunger;
        if (thirstBar != null) thirstBar.value = currentThirst;
        if (panicBar != null) panicBar.value = currentPanic;
    }

    // === METODE DE INTERACTIUNE ===

    public void ConsumeFood()
    {
        currentHunger += foodRestoreValue;
        currentHunger = Mathf.Clamp(currentHunger, 0f, 100f);
        Debug.Log("Food consumed. Hunger restored.");
        UpdateUI();
    }

    public void ConsumeWater()
    {
        currentThirst += waterRestoreValue;
        currentThirst = Mathf.Clamp(currentThirst, 0f, 100f);
        Debug.Log("Water consumed. Thirst restored.");
        UpdateUI();
    }

    public void ReducePanicNearFire()
    {
        currentPanic += fireCalmValue * Time.deltaTime * 2f;
        currentPanic = Mathf.Clamp(currentPanic, 0f, 100f);
    }

    void CheckFailureConditions()
    {
        if (currentHunger <= 0f) TriggerGameOver("hunger");
        else if (currentThirst <= 0f) TriggerGameOver("thirst");
        else if (currentPanic <= 0f) TriggerGameOver("panic");
    }

    void TriggerGameOver(string reason)
    {
        Debug.Log($"Game Over! You collapsed from {reason} exhaustion.");
        
        if (GameOverManager.Instance != null && InventoryManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver(
                reason,
                InventoryManager.Instance.GetStickCount(),
                InventoryManager.Instance.GetStoneCount(),
                InventoryManager.Instance.GetFoodCount(),
                InventoryManager.Instance.GetWaterCount(),
                InventoryManager.Instance.GetCraftedItemsCount()
            );
        }
    }
}