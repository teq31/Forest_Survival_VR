using UnityEngine;

public class MessageDirector : MonoBehaviour
{
    [Header("References")]
    public StressSystem stressSystem;
    public MessageUI messageUI;
    public SurvivalManager survivalManager;
    public DayNightCycle dayNightCycle; // zi/noapte vine de aici

    [Header("Need thresholds (0..1)")]
    public float criticalNeed01 = 0.15f;  // <15% (critical)

    [Header("Stress thresholds")]
    public float stressHigh = 0.5f;
    public float stressMedium = 0.2f;

    [Header("Positive reinforcement (action detection)")]
    // Dacă mâncarea/apa îți ridică bara cu ~0.25, un prag de 0.12–0.18 e ideal
    public float minHungerJumpToPraise = 0.12f;
    public float minThirstJumpToPraise = 0.12f;

    // pentru “you calmed down”
    public float calmFromHigh = 0.70f;  // era sus
    public float calmToLow = 0.30f;     // a coborât jos

    [Header("Anti-spam")]
    public float cooldownSeconds = 0.8f;     // cât timp blocăm următorul mesaj
    public float stressHoldSeconds = 0.7f;   // cât trebuie să mențină stresul ca să apară mesajul

    [Header("Craft requirements (match your InventoryManager)")]
    public int campfireSticksRequired = 2;
    public int campfireStonesRequired = 2;
    public int torchSticksRequired = 1;
    public int torchStonesRequired = 1;

    [Header("Day/Night interpretation")]
    public bool treatDuskAsNight = true;
    public bool treatDawnAsNight = true;

    float _nextAllowedTime;
    string _lastKey;
    float _stressHighTimer;
    float _stressMedTimer;

    // Previous values (for detecting actions)
    float _prevHunger01 = 1f;
    float _prevThirst01 = 1f;
    float _prevStress01 = 0f;

    // Praise “one-shot” flags (prevents spam)
    bool _praisedFood;
    bool _praisedWater;
    bool _praisedCalm;

    void Update()
    {
        if (stressSystem == null) { Debug.LogError("MessageDirector: StressSystem is NULL"); return; }
        if (messageUI == null) { Debug.LogError("MessageDirector: MessageUI is NULL"); return; }

        if (survivalManager == null)
        {
            survivalManager = FindObjectOfType<SurvivalManager>();

            if (survivalManager == null)
            {
                Debug.LogError("MessageDirector: SurvivalManager is NULL (still not found)");
                return;
            }
        }


        if (dayNightCycle == null) { Debug.LogError("MessageDirector: DayNightCycle is NULL (assign it in Inspector)"); return; }
        if (InventoryManager.Instance == null) { Debug.LogError("MessageDirector: InventoryManager.Instance is NULL"); return; }

        float stress01 = stressSystem.Stress01;
        float hunger01 = Mathf.Clamp01(survivalManager.currentHunger / 100f);
        float thirst01 = Mathf.Clamp01(survivalManager.currentThirst / 100f);

        // Timers for stress messages
        _stressHighTimer = (stress01 > stressHigh) ? (_stressHighTimer + Time.deltaTime) : 0f;
        _stressMedTimer = (stress01 > stressMedium) ? (_stressMedTimer + Time.deltaTime) : 0f;

        // Reset praise flags when you go back into problem states
        if (hunger01 < criticalNeed01) _praisedFood = false;
        if (thirst01 < criticalNeed01) _praisedWater = false;
        if (stress01 > calmFromHigh) _praisedCalm = false;

        // ---------------------------
        // 1) POSITIVE REINFORCEMENT
        // ---------------------------
        if (Time.time >= _nextAllowedTime)
        {
            float hungerDelta = hunger01 - _prevHunger01;
            float thirstDelta = thirst01 - _prevThirst01;

            // 💧 Praise drinking (detect a real jump)
            if (!_praisedWater && thirstDelta >= minThirstJumpToPraise)
            {
                messageUI.Show("Nice work. Drinking water helps you stay focused.");
                _praisedWater = true;
                _nextAllowedTime = Time.time + cooldownSeconds;
            }
            // 🍎 Praise eating (detect a real jump)
            else if (!_praisedFood && hungerDelta >= minHungerJumpToPraise)
            {
                messageUI.Show("Well done. Eating helped restore your energy.");
                _praisedFood = true;
                _nextAllowedTime = Time.time + cooldownSeconds;
            }
            // 🧘 Praise calming down (stress fell from high -> low)
            else if (!_praisedCalm && _prevStress01 > calmFromHigh && stress01 < calmToLow)
            {
                messageUI.Show("You’ve steadied yourself. Keep this pace.");
                _praisedCalm = true;
                _nextAllowedTime = Time.time + cooldownSeconds;
            }
        }

        // Debug (o dată pe secundă)
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[Update] Hunger01={hunger01:F2} (Δ={(hunger01 - _prevHunger01):F2}) Thirst01={thirst01:F2} (Δ={(thirst01 - _prevThirst01):F2}) Stress01={stress01:F2}");
            Debug.Log($"[Update] TimeState={dayNightCycle.CurrentTimeState}");
        }

        // Dacă suntem în cooldown, nu dăm mesaje corective acum
        if (Time.time < _nextAllowedTime)
        {
            _prevHunger01 = hunger01;
            _prevThirst01 = thirst01;
            _prevStress01 = stress01;
            return;
        }

        // ---------------------------
        // 2) CORRECTIVE MESSAGES
        // ---------------------------
        var rec = DecideMessage(stress01);
        if (rec.key != null)
        {
            // optional anti-repeat:
            // if (rec.key == _lastKey) return;

            messageUI.Show(rec.text);
            _lastKey = rec.key;
            _nextAllowedTime = Time.time + cooldownSeconds;
        }

        // Save previous for next frame
        _prevHunger01 = hunger01;
        _prevThirst01 = thirst01;
        _prevStress01 = stress01;
    }

    (string key, string text) DecideMessage(float stress01)
    {
        float hunger01 = Mathf.Clamp01(survivalManager.currentHunger / 100f);
        float thirst01 = Mathf.Clamp01(survivalManager.currentThirst / 100f);
        bool isNight = IsNightLike();

        int sticks = InventoryManager.Instance.GetStickCount();
        int stones = InventoryManager.Instance.GetStoneCount();
        int food = InventoryManager.Instance.GetFoodCount();
        int water = InventoryManager.Instance.GetWaterCount();

        bool canCraftTorch = sticks >= torchSticksRequired && stones >= torchStonesRequired;
        bool canCraftCampfire = sticks >= campfireSticksRequired && stones >= campfireStonesRequired;

        // Priority 1: Thirst
        if (thirst01 < criticalNeed01)
        {
            if (water >= 1)
            {
                if (isNight)
                    return ("drink_water_night", "You’re dehydrated. Drink water now—then stay close and move carefully in the dark.");
                return ("drink_water_day", "You’re dehydrated. Drink water now.");
            }

            if (isNight)
            {
                if (canCraftTorch)
                    return ("no_water_night_torch", "No water left. Craft a torch and search for water—move slowly and stay alert.");
                return ("no_water_night_no_torch", "No water left—and it’s dark. Find sticks and stones to craft a torch before heading deeper.");
            }

            return ("no_water_day", "No water left. Look for a water source nearby and return to camp when you can.");
        }

        // Priority 2: Hunger
        if (hunger01 < criticalNeed01)
        {
            if (food >= 1)
            {
                if (isNight)
                    return ("eat_food_night", "You’re starving. Eat something now—then keep your actions slow and deliberate in the dark.");
                return ("eat_food_day", "You’re starving. Eat something now.");
            }

            if (isNight)
            {
                if (canCraftTorch)
                    return ("no_food_night_torch", "No food left. Craft a torch and forage carefully—focus on finding something small and safe.");
                return ("no_food_night_no_torch", "No food left—and it’s dark. Gather sticks and stones first so you can craft a torch.");
            }

            return ("no_food_day", "No food left. Forage nearby—take it one step at a time.");
        }

        // Priority 3: High stress
        if (_stressHighTimer >= stressHoldSeconds)
        {
            if (canCraftCampfire)
                return ("stress_high_build_fire", "You seem tense. Build a campfire—then take a moment to slow your breathing.");

            if (isNight)
            {
                if (canCraftTorch)
                    return ("stress_high_night_torch", "You seem tense. Craft a torch, then gather more sticks and stones to build a fire.");
                return ("stress_high_night_no_torch", "You seem tense—and it’s dark. Collect sticks and stones first so you can craft a torch.");
            }

            if (canCraftTorch)
                return ("stress_high_day_torch", "You seem tense. Craft a torch and focus on one small task at a time.");

            return ("stress_high_day", "You seem tense. Gather sticks and stones—building a fire can help you reset.");
        }

        // Priority 4: Medium stress
        if (_stressMedTimer >= stressHoldSeconds)
        {
            if (isNight)
                return ("stress_med_night", "Slow down. Keep your movements steady—choose one safe action and focus on it.");
            return ("stress_med_day", "Slow your movements. Look around, pick one small goal, and focus on that.");
        }

        return (null, null);
    }

    bool IsNightLike()
    {
        var state = dayNightCycle.CurrentTimeState;

        if (state == DayNightCycle.TimeState.Night) return true;
        if (treatDuskAsNight && state == DayNightCycle.TimeState.Dusk) return true;
        if (treatDawnAsNight && state == DayNightCycle.TimeState.Dawn) return true;

        return false;
    }
}
