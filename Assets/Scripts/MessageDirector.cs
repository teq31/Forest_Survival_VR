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

    // Random + anti-repeat per variant
    System.Random _rng = new System.Random();
    string _lastVariantId;

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
                messageUI.Show(PickVariant("praise_water", new[]
                {
                    "Nice work. Drinking water helps you stay focused.",
                    "Good call. Hydration keeps your mind sharp.",
                    "Well done—staying hydrated improves your control."
                }));

                _praisedWater = true;
                _nextAllowedTime = Time.time + cooldownSeconds;
            }
            // 🍎 Praise eating (detect a real jump)
            else if (!_praisedFood && hungerDelta >= minHungerJumpToPraise)
            {
                messageUI.Show(PickVariant("praise_food", new[]
                {
                    "Well done. Eating helped restore your energy.",
                    "Nice. That should give you a bit more strength.",
                    "Good job—fueling up helps you keep going."
                }));

                _praisedFood = true;
                _nextAllowedTime = Time.time + cooldownSeconds;
            }
            // 🧘 Praise calming down (stress fell from high -> low)
            else if (!_praisedCalm && _prevStress01 > calmFromHigh && stress01 < calmToLow)
            {
                messageUI.Show(PickVariant("praise_calm", new[]
                {
                    "You’ve steadied yourself. Keep this pace.",
                    "Good. You’re back in control—keep it steady.",
                    "Nice recovery. Stay calm and deliberate."
                }));

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

        bool thirstCritical = thirst01 < criticalNeed01;
        bool hungerCritical = hunger01 < criticalNeed01;

        bool stressHighHeld = _stressHighTimer >= stressHoldSeconds;
        bool stressMedHeld = _stressMedTimer >= stressHoldSeconds;

        // Short add-ons if stress is also high
        string stressAddon = stressHighHeld
            ? PickVariant("addon_stress", new[]
            {
                " Keep your breathing slow.",
                " Don’t rush—steady hands.",
                " One step at a time."
            })
            : "";

        // ---------------------------
        // Priority 0: BOTH thirst + hunger (COMBINED)
        // ---------------------------
        if (thirstCritical && hungerCritical)
        {
            bool haveAny = (water >= 1 || food >= 1);

            string baseText = haveAny
                ? PickVariant("need_both_have", new[]
                {
                    "You’re starving and dehydrated. Drink water first, then eat—slow and steady.",
                    "Critical needs: water and food. Hydrate first, then refuel.",
                    "You need both water and food. Drink first, then eat."
                })
                : PickVariant("need_both_no", new[]
                {
                    "You’re starving and dehydrated—and supplies are low. Search nearby and don’t take risks.",
                    "Both hunger and thirst are critical. Stay close, search nearby, and move carefully.",
                    "Critical needs. Prioritize safety: search nearby, slow steps, return to camp."
                });

            if (isNight)
            {
                baseText += PickVariant("both_night_hint", new[]
                {
                    " Stay close in the dark.",
                    " Keep your torch in mind tonight.",
                    " Don’t go deep into the dark."
                });
            }

            // dacă e stres mare, adaugă la final
            baseText += stressAddon;

            return ("need_both_critical", baseText);
        }

        // ---------------------------
        // Priority 1: Thirst
        // ---------------------------
        if (thirstCritical)
        {
            string key;
            string text;

            if (water >= 1)
            {
                key = isNight ? "drink_water_night" : "drink_water_day";
                text = isNight
                    ? PickVariant(key, new[]
                    {
                        "You’re dehydrated. Drink water now—then stay close and move carefully in the dark.",
                        "Drink water now. Keep your steps slow tonight.",
                        "Hydration first. Drink, then move carefully in the dark."
                    })
                    : PickVariant(key, new[]
                    {
                        "You’re dehydrated. Drink water now.",
                        "You need water. Drink before you go further.",
                        "Hydration first—drink water now."
                    });
            }
            else if (isNight)
            {
                if (canCraftTorch)
                {
                    key = "no_water_night_torch";
                    text = PickVariant(key, new[]
                    {
                        "No water left. Craft a torch and search for water—move slowly and stay alert.",
                        "Out of water. Make a torch and search nearby carefully.",
                        "No water. Torch up, then search close to camp."
                    });
                }
                else
                {
                    key = "no_water_night_no_torch";
                    text = PickVariant(key, new[]
                    {
                        "No water left—and it’s dark. Find sticks and stones to craft a torch before heading deeper.",
                        "It’s dark and you’re out of water. Craft a torch first.",
                        "No water. In the dark, get torch materials before you search."
                    });
                }
            }
            else
            {
                key = "no_water_day";
                text = PickVariant(key, new[]
                {
                    "No water left. Look for a water source nearby and return to camp when you can.",
                    "You’re out of water. Search for a nearby water source and stay safe.",
                    "No water. Prioritize a nearby source—don’t push too far."
                });
            }

            text += stressAddon;
            return (key, text);
        }

        // ---------------------------
        // Priority 2: Hunger
        // ---------------------------
        if (hungerCritical)
        {
            string key;
            string text;

            if (food >= 1)
            {
                key = isNight ? "eat_food_night" : "eat_food_day";
                text = isNight
                    ? PickVariant(key, new[]
                    {
                        "You’re starving. Eat something now—then keep your actions slow and deliberate in the dark.",
                        "Eat now. Keep it calm and careful tonight.",
                        "Food first. Eat, then move deliberately in the dark."
                    })
                    : PickVariant(key, new[]
                    {
                        "You’re starving. Eat something now.",
                        "Low energy—eat now and recover.",
                        "Eat something. You’ll move better after."
                    });
            }
            else if (isNight)
            {
                if (canCraftTorch)
                {
                    key = "no_food_night_torch";
                    text = PickVariant(key, new[]
                    {
                        "No food left. Craft a torch and forage carefully—focus on finding something small and safe.",
                        "Out of food. Torch up and forage close to camp.",
                        "No food. Make a torch and search carefully nearby."
                    });
                }
                else
                {
                    key = "no_food_night_no_torch";
                    text = PickVariant(key, new[]
                    {
                        "No food left—and it’s dark. Gather sticks and stones first so you can craft a torch.",
                        "It’s dark and you’re out of food. Craft a torch before foraging.",
                        "No food. In the dark, torch first—then forage."
                    });
                }
            }
            else
            {
                key = "no_food_day";
                text = PickVariant(key, new[]
                {
                    "No food left. Forage nearby—take it one step at a time.",
                    "You’re out of food. Search nearby for something safe to eat.",
                    "No food. Stay close and forage carefully."
                });
            }

            text += stressAddon;
            return (key, text);
        }

        // ---------------------------
        // Priority 3: High stress
        // ---------------------------
        if (stressHighHeld)
        {
            if (canCraftCampfire)
                return ("stress_high_build_fire", PickVariant("stress_high_build_fire", new[]
                {
                    "You seem tense. Build a campfire—then take a moment to slow your breathing.",
                    "Stress is high. Make a fire and reset your pace.",
                    "You’re overwhelmed. Campfire first—then one small task."
                }));

            if (isNight)
            {
                if (canCraftTorch)
                    return ("stress_high_night_torch", PickVariant("stress_high_night_torch", new[]
                    {
                        "You seem tense. Craft a torch, then gather more sticks and stones to build a fire.",
                        "High stress. Make a torch and take it step by step.",
                        "Craft a torch first—then focus on one safe action."
                    }));

                return ("stress_high_night_no_torch", PickVariant("stress_high_night_no_torch", new[]
                {
                    "You seem tense—and it’s dark. Collect sticks and stones first so you can craft a torch.",
                    "High stress at night. Torch materials first—no rushing.",
                    "In the dark, get torch materials first. Slow down."
                }));
            }

            if (canCraftTorch)
                return ("stress_high_day_torch", PickVariant("stress_high_day_torch", new[]
                {
                    "You seem tense. Craft a torch and focus on one small task at a time.",
                    "High stress. Torch up and pick one simple goal.",
                    "You’re tense. Make a torch and simplify your next move."
                }));

            return ("stress_high_day", PickVariant("stress_high_day", new[]
            {
                "You seem tense. Gather sticks and stones—building a fire can help you reset.",
                "Slow down. Collect materials and steady your pace.",
                "Stress is high. Do one small thing: gather sticks and stones."
            }));
        }

        // ---------------------------
        // Priority 4: Medium stress
        // ---------------------------
        if (stressMedHeld)
        {
            if (isNight)
                return ("stress_med_night", PickVariant("stress_med_night", new[]
                {
                    "Slow down. Keep your movements steady—choose one safe action and focus on it.",
                    "Take it easy tonight. One safe step at a time.",
                    "Steady pace. Stay alert and keep it simple."
                }));

            return ("stress_med_day", PickVariant("stress_med_day", new[]
            {
                "Slow your movements. Look around, pick one small goal, and focus on that.",
                "Breathe. Choose one small task and do it calmly.",
                "Slow down. Keep it simple and steady."
            }));
        }

        return (null, null);
    }

    string PickVariant(string key, string[] variants)
    {
        if (variants == null || variants.Length == 0) return null;

        // încearcă să nu repete aceeași variantă de două ori la rând
        for (int i = 0; i < 5; i++)
        {
            int idx = _rng.Next(variants.Length);
            string id = $"{key}#{idx}";
            if (id != _lastVariantId)
            {
                _lastVariantId = id;
                return variants[idx];
            }
        }

        // fallback
        int fallbackIdx = _rng.Next(variants.Length);
        _lastVariantId = $"{key}#{fallbackIdx}";
        return variants[fallbackIdx];
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
