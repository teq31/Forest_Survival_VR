using UnityEngine;
using UnityEngine.UI;

public class SurvivalManager : MonoBehaviour
{
    [Header("UI Sliders (3 only)")]
    public Slider healthBar; // ❤️ Health
    public Slider hungerBar; // 🍎 Hunger
    public Slider thirstBar; // 💧 Thirst

    [Header("Systems")]
    public StressSystem stressSystem;     // VR + non-VR (fallback already)
    public DayNightCycle dayNightCycle;   // zi/noapte vine de aici

    [Header("Current Values (100 = full, 0 = fail)")]
    public float currentHealth = 100f;
    public float currentHunger = 100f;
    public float currentThirst = 100f;

    [Header("Need degradation (per second)")]
    public float hungerRate = 0.2f;
    public float thirstRate = 0.3f;

    [Header("Restoration (needs)")]
    public float foodRestoreValue = 25f;
    public float waterRestoreValue = 30f;

    [Header("Health healing from actions")]
    public float healOnEat = 6f;          // cât HP primești când mănânci
    public float healOnDrink = 4f;        // cât HP primești când bei
    public float healBonusWhenCritical = 6f; // bonus dacă erai sub pragul critic

    [Header("Health damage from hunger/thirst")]
    [Range(0f, 1f)] public float criticalNeed01 = 0.15f;  // sub 15% începe damage
    public float hungerDamagePerSecond = 8f;              // damage/sec când hunger=0
    public float thirstDamagePerSecond = 12f;             // damage/sec când thirst=0

    [Header("Health damage from stress (NIGHT ONLY)")]
    [Range(0f, 1f)] public float stressDamageStart = 0.55f; // de aici începe damage
    public float stressDamagePerSecondAtMax = 6f;           // damage/sec la Stress01=1
    public float stressEmaSpeed = 6f;                       // smoothing pentru Stress01

    [Header("Day/Night interpretation")]
    public bool treatDuskAsNight = true;
    public bool treatDawnAsNight = true;

    [Header("Debug")]
    public bool debugLogs = false;

    private float _stressEma01 = 0f;
    private bool _isGameOver = false;

    void Start()
    {
        SetMaxValues();
        UpdateUI();
    }

    void SetMaxValues()
    {
        if (healthBar != null) healthBar.maxValue = 100f;
        if (hungerBar != null) hungerBar.maxValue = 100f;
        if (thirstBar != null) thirstBar.maxValue = 100f;
    }

    void Update()
    {
        if (_isGameOver) return;

        // 1) Needs degrade
        currentHunger -= hungerRate * Time.deltaTime;
        currentThirst -= thirstRate * Time.deltaTime;

        currentHunger = Mathf.Clamp(currentHunger, 0f, 100f);
        currentThirst = Mathf.Clamp(currentThirst, 0f, 100f);

        // 2) Stress (0..1) with smoothing
        float stress01 = (stressSystem != null) ? Mathf.Clamp01(stressSystem.Stress01) : 0f;
        _stressEma01 = Mathf.Lerp(_stressEma01, stress01, 1f - Mathf.Exp(-stressEmaSpeed * Time.deltaTime));

        // 3) Damage from hunger/thirst (only when below critical)
        float hunger01 = currentHunger / 100f;
        float thirst01 = currentThirst / 100f;

        float hungerSeverity = NeedSeverity01(hunger01, criticalNeed01); // 0..1
        float thirstSeverity = NeedSeverity01(thirst01, criticalNeed01); // 0..1

        float damageFromHunger = hungerSeverity * hungerDamagePerSecond;
        float damageFromThirst = thirstSeverity * thirstDamagePerSecond;

        // 4) Damage from stress (NIGHT ONLY)
        float damageFromStress = 0f;
        if (IsNightLike())
        {
            float stressSeverity = Mathf.InverseLerp(stressDamageStart, 1f, _stressEma01); // 0..1
            damageFromStress = stressSeverity * stressDamagePerSecondAtMax;
        }

        float totalDamagePerSec = damageFromHunger + damageFromThirst + damageFromStress;

        currentHealth -= totalDamagePerSec * Time.deltaTime;
        currentHealth = Mathf.Clamp(currentHealth, 0f, 100f);

        // 5) UI + GameOver
        UpdateUI();
        if (currentHealth <= 0f) TriggerGameOver("health");

        if (debugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"NightLike={IsNightLike()} StressRaw={(stressSystem ? stressSystem.Stress01 : -1f):F2} StressEma={_stressEma01:F2}");
            Debug.Log($"Damage: hunger={damageFromHunger:F2} thirst={damageFromThirst:F2} stress={damageFromStress:F2} total={totalDamagePerSec:F2} Health={currentHealth:F1}");
        }
    }

    // 0 when need >= criticalNeed01, 1 when need == 0
    float NeedSeverity01(float need01, float critical01)
    {
        if (need01 >= critical01) return 0f;
        return Mathf.Clamp01(1f - (need01 / Mathf.Max(0.0001f, critical01)));
    }

    bool IsNightLike()
    {
        if (dayNightCycle == null) return false;

        var state = dayNightCycle.CurrentTimeState;
        if (state == DayNightCycle.TimeState.Night) return true;
        if (treatDuskAsNight && state == DayNightCycle.TimeState.Dusk) return true;
        if (treatDawnAsNight && state == DayNightCycle.TimeState.Dawn) return true;
        return false;
    }

    void UpdateUI()
    {
        if (healthBar != null) healthBar.value = currentHealth;
        if (hungerBar != null) hungerBar.value = currentHunger;
        if (thirstBar != null) thirstBar.value = currentThirst;
    }

    // === Interactions ===
    public void ConsumeFood()
    {
        bool wasCritical = (currentHunger / 100f) < criticalNeed01;

        currentHunger += foodRestoreValue;
        currentHunger = Mathf.Clamp(currentHunger, 0f, 100f);

        float heal = healOnEat + (wasCritical ? healBonusWhenCritical : 0f);
        currentHealth = Mathf.Clamp(currentHealth + heal, 0f, 100f);

        Debug.Log($"Food consumed. Hunger restored. +HP {heal:F0}");
        UpdateUI();
    }

    public void ConsumeWater()
    {
        bool wasCritical = (currentThirst / 100f) < criticalNeed01;

        currentThirst += waterRestoreValue;
        currentThirst = Mathf.Clamp(currentThirst, 0f, 100f);

        float heal = healOnDrink + (wasCritical ? healBonusWhenCritical : 0f);
        currentHealth = Mathf.Clamp(currentHealth + heal, 0f, 100f);

        Debug.Log($"Water consumed. Thirst restored. +HP {heal:F0}");
        UpdateUI();
    }

    void TriggerGameOver(string reason)
    {
        if (_isGameOver) return;
        _isGameOver = true;

        Debug.Log($"Game Over! You collapsed from {reason} exhaustion.");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is NULL! Asigura-te ca GameManager exista in scena.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager.Instance is NULL!");
            return;
        }

        GameManager.Instance.ShowGameOver(
            reason,
            InventoryManager.Instance.GetStickCount(),
            InventoryManager.Instance.GetStoneCount(),
            InventoryManager.Instance.GetFoodCount(),
            InventoryManager.Instance.GetWaterCount(),
            InventoryManager.Instance.GetCraftedItemsCount()
        );
    }
}
