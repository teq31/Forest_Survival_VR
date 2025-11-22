using UnityEngine;
using TMPro;

public class DayNightCycle : MonoBehaviour
{
    public enum TimeState { Day, Dusk, Night, Dawn }

    
    [Header("Skybox Materials")]
    public Material daySkybox; 
    public Material nightSkybox; 
    public Material duskSkybox;  
    public Material dawnSkybox;  
    
    [Header("Game Settings")]
    [Tooltip("Total duration of one full cycle (Day -> Night -> Day) in minutes.")]
    public float cycleDurationMinutes = 5f; 
    
    [Tooltip("Percentage of the cycle allocated to the full Day/Night phases. Ex: 0.3 means 30% Day + 30% Night. The remaining 40% is Dusk/Dawn.")]
    [Range(0.01f, 0.45f)]
    public float fullPhaseRatio = 0.3f; 
    
    [Header("Scene References")]
    public Light directionalLight; 
    public TMPro.TextMeshProUGUI timerText; 

    
    private TimeState currentState = TimeState.Day;
    private float timeElapsedInPhase = 0f;
    private float fullPhaseDuration; 
    private float transitionPhaseDuration; 

    private float dayIntensity = 1f;
    private float nightIntensity = 0.1f;

    void Start()
    {
        float totalDurationSeconds = cycleDurationMinutes * 60f;
        
        fullPhaseDuration = totalDurationSeconds * fullPhaseRatio;
        
        transitionPhaseDuration = (totalDurationSeconds * (1f - (fullPhaseRatio * 2f))) / 2f;
        
        SwitchPhase(TimeState.Day);
    }

    void Update()
    {
        timeElapsedInPhase += Time.deltaTime;
        
        float currentPhaseDuration = GetCurrentPhaseDuration(currentState);

        RotateLight(currentPhaseDuration);
        UpdateTimer(currentPhaseDuration);

        if (timeElapsedInPhase >= currentPhaseDuration)
        {
            SwitchPhase(GetNextState(currentState));
        }
    }

    void SwitchPhase(TimeState newState)
    {
        currentState = newState;
        timeElapsedInPhase = 0f;
        
        switch (currentState)
        {
            case TimeState.Day:
                RenderSettings.skybox = daySkybox;
                directionalLight.intensity = dayIntensity;
                directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f); 
                break;
                
            case TimeState.Dusk:
                RenderSettings.skybox = duskSkybox; 
                directionalLight.intensity = (dayIntensity + nightIntensity) / 2f; 
                break;
                
            case TimeState.Night:
                RenderSettings.skybox = nightSkybox;
                directionalLight.intensity = nightIntensity;
                directionalLight.transform.rotation = Quaternion.Euler(150f, 150f, 0f); 
                break;
                
            case TimeState.Dawn:
                RenderSettings.skybox = dawnSkybox; 
                directionalLight.intensity = (dayIntensity + nightIntensity) / 2f; 
                break;
        }
    }

    void RotateLight(float phaseDuration)
    {
        float rotationAmount = (Time.deltaTime / phaseDuration) * 90f; 
        directionalLight.transform.Rotate(Vector3.up, rotationAmount, Space.World);
    }

    float GetCurrentPhaseDuration(TimeState state)
    {
        if (state == TimeState.Day || state == TimeState.Night)
        {
            return fullPhaseDuration;
        }
        else // Dusk or Dawn
        {
            return transitionPhaseDuration;
        }
    }

    TimeState GetNextState(TimeState state)
    {
        switch (state)
        {
            case TimeState.Day: return TimeState.Dusk;
            case TimeState.Dusk: return TimeState.Night;
            case TimeState.Night: return TimeState.Dawn;
            case TimeState.Dawn: return TimeState.Day;
            default: return TimeState.Day;
        }
    }

    void UpdateTimer(float phaseDuration)
    {
        float timeLeft = phaseDuration - timeElapsedInPhase;
        int minutes = Mathf.FloorToInt(timeLeft / 60F);
        int seconds = Mathf.FloorToInt(timeLeft % 60);

        string phaseName = currentState.ToString();
        
        string durationLabel = (currentState == TimeState.Dusk || currentState == TimeState.Dawn) 
            ? $"Transition Duration: {transitionPhaseDuration:F1} seconds"
            : $"Full Phase Duration: {fullPhaseDuration:F1} seconds";

        timerText.text = $"Phase: {phaseName}\n{durationLabel}\nRemaining: {minutes:00}:{seconds:00}";
    }
}