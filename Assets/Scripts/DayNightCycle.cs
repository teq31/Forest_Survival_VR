using UnityEngine;
using TMPro;

public class DayNightCycle : MonoBehaviour
{
    public enum TimeState { Day, Dusk, Night, Dawn }
    
    // Proprietate publică pentru a permite accesul altor scripturi (ex: WeatherManager) la starea curentă
    public TimeState CurrentTimeState => currentState;
    
    [Tooltip("Progresul total al ciclului (0.0 = Miezul Nopții, 0.5 = Miezul Zilei)")]
    public float timeOfDay { get; private set; } = 0f; 

    
    [Header("Skybox Materials")]
    // Notă: Aceste materiale trebuie să suporte shader-ul "Blended Skybox" sau "Procedural Sky"
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
    private float totalDurationSeconds;

    private float dayIntensity = 1f;
    private float nightIntensity = 0.1f;

    void Start()
    {
        totalDurationSeconds = cycleDurationMinutes * 60f;
        
        fullPhaseDuration = totalDurationSeconds * fullPhaseRatio;
        
        transitionPhaseDuration = (totalDurationSeconds * (1f - (fullPhaseRatio * 2f))) / 2f;
        
        SwitchPhase(TimeState.Day);
    }

    void Update()
    {
        timeElapsedInPhase += Time.deltaTime;
        
        float currentPhaseDuration = GetCurrentPhaseDuration(currentState);

        // Calculează progresul total (0.0 la 1.0)
        timeOfDay = Mathf.Repeat(Time.time / totalDurationSeconds, 1f);
        
        HandleSkyboxBlending(); 
        RotateLightGlobal(); 
        UpdateTimer(currentPhaseDuration);

        // Verifică tranziția de fază
        if (timeElapsedInPhase >= currentPhaseDuration)
        {
            SwitchPhase(GetNextState(currentState));
        }
    }

    void SwitchPhase(TimeState newState)
    {
        currentState = newState;
        timeElapsedInPhase = 0f;
        
        // **IMPORTANT**: Nu mai setăm Skybox sau Intensitatea aici. 
        // Acestea sunt gestionate constant și lin de HandleSkyboxBlending() și RotateLightGlobal().
    }
    
    // Gestionează schimbarea Skybox-ului lin și setează materialul de bază pentru fazele pline.
    void HandleSkyboxBlending()
    {
        if (currentState == TimeState.Day)
        {
            // Faza Plină de Zi (fără blending)
            RenderSettings.skybox = daySkybox;
            if (RenderSettings.skybox != null) RenderSettings.skybox.SetFloat("_Blend", 0f); 
            DynamicGI.UpdateEnvironment(); 
        }
        else if (currentState == TimeState.Night)
        {
            // Faza Plină de Noapte (fără blending)
            RenderSettings.skybox = nightSkybox;
            if (RenderSettings.skybox != null) RenderSettings.skybox.SetFloat("_Blend", 0f);
            DynamicGI.UpdateEnvironment(); 
        }
        else if (currentState == TimeState.Dusk || currentState == TimeState.Dawn)
        {
            // Faza de tranziție (blending)
            float blendRatio = timeElapsedInPhase / transitionPhaseDuration;

            // Setează materialul de tranziție
            if (currentState == TimeState.Dusk)
            {
                RenderSettings.skybox = duskSkybox; 
            }
            else // Dawn
            {
                RenderSettings.skybox = dawnSkybox;
            }
            
            // Aplică blending-ul (asigură-te că materialul are proprietatea _Blend)
            if (RenderSettings.skybox != null) RenderSettings.skybox.SetFloat("_Blend", blendRatio); 
            
            // Asigură-te că Unity actualizează imediat setările
            DynamicGI.UpdateEnvironment(); 
        }
    }


    // Rotație bazată pe progresul total al timpului (0 la 1) și controlul intensității
    void RotateLightGlobal()
    {
        // Calculează unghiul total de rotație (360 de grade pe parcursul ciclului)
        float angle = timeOfDay * 360f; 
        
        // Rotește lumina pe axa X pentru a simula mișcarea pe cer (răsărit/apus)
        directionalLight.transform.localRotation = Quaternion.Euler(
            angle, 
            directionalLight.transform.localRotation.eulerAngles.y,
            directionalLight.transform.localRotation.eulerAngles.z
        );
        
        // Ajustează intensitatea luminii lin în timpul tranzițiilor (Fără setări fixe din SwitchPhase)
        float sunAngle = directionalLight.transform.localRotation.eulerAngles.x;
        
        // Dacă soarele este între 90 și 270 de grade (sub orizont)
        if (sunAngle > 90f && sunAngle < 270f)
        {
             // Calculează cât de "întunecat" trebuie să fie (0 la 1)
             float darkRatio = Mathf.InverseLerp(90f, 270f, sunAngle);
             // Interpolează intensitatea de la Zi la Noapte
             directionalLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, darkRatio);
        }
        else
        {
            // Zi Plină (Soarele sus pe cer)
            directionalLight.intensity = dayIntensity;
        }
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
            ? $"Transition: {transitionPhaseDuration:F1} seconds"
            : $"Complete phase: {fullPhaseDuration:F1} seconds";

        timerText.text = $"Phase: {phaseName}\n{durationLabel}\nRemaining: {minutes:00}:{seconds:00}\nProgress: {timeOfDay:F2}";
    }
}