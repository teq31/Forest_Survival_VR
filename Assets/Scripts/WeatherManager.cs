using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [Header("Integrari")]
    public DayNightCycle dayNightCycle;
    public StressSystem stressSystem;

    [Header("Obiectul de Ploaie (VFX)")]
    // Aici tragi tot obiectul care contine particulele
    public GameObject rainGameObject; 

    [Header("Audio")]
    public AudioSource ambianceSource;
    public AudioSource rainSoundSource;

    [Header("Clipuri")]
    public AudioClip dayAmbiance;
    public AudioClip nightAmbiance;
    public AudioClip rainLoop;

    [Header("Reguli")]
    [Range(0f, 1f)]
    public float maxStressForRain = 0.30f; 
    public bool allowRainInDusk = true;
    public float stressEmaSpeed = 4f;

    // Stare interna
    private float _stressEma01 = 0f;
    private bool _lastRainState = false; // Tine minte ce am facut in frame-ul trecut

    void Start()
    {
        // 1. Validari
        if (dayNightCycle == null) dayNightCycle = FindFirstObjectByType<DayNightCycle>();
        if (stressSystem == null) stressSystem = FindFirstObjectByType<StressSystem>();

        if (rainGameObject == null)
        {
            Debug.LogError("CRITIC: Nu ai pus 'Rain GameObject' in Inspector la WeatherManager!");
            return;
        }

        // 2. Initializare Audio
        if (ambianceSource != null && dayAmbiance != null)
        {
            ambianceSource.clip = dayAmbiance;
            ambianceSource.loop = true;
            ambianceSource.Play();
        }

        if (rainSoundSource != null && rainLoop != null)
        {
            rainSoundSource.clip = rainLoop;
            rainSoundSource.loop = true;
            rainSoundSource.volume = 0f;
            rainSoundSource.Play();
        }

        RenderSettings.fog = false;

        // 3. FORTAM STAREA INITIALA: OPRIT
        // Indiferent ce s-a intamplat inainte, la Start ploaia dispare.
        rainGameObject.SetActive(false);
        _lastRainState = false;
    }

    void Update()
    {
        if (dayNightCycle == null || rainGameObject == null) return;

        // --- 1. Calculam daca AR TREBUI sa ploua ---
        var timeState = dayNightCycle.CurrentTimeState;
        
        bool isNightOrDusk = (timeState == DayNightCycle.TimeState.Night) ||
                             (allowRainInDusk && timeState == DayNightCycle.TimeState.Dusk);

        float stress01 = (stressSystem != null) ? Mathf.Clamp01(stressSystem.Stress01) : 0f;
        
        // Smoothing pentru stres
        _stressEma01 = Mathf.Lerp(_stressEma01, stress01, 1f - Mathf.Exp(-stressEmaSpeed * Time.deltaTime));

        bool isCalm = (_stressEma01 <= maxStressForRain);
        
        bool shouldRain = isNightOrDusk && isCalm;

        // --- 2. Aplicam decizia (Doar Activam/Dezactivam obiectul) ---
        // Verificam daca starea s-a schimbat fata de frame-ul trecut sau fata de realitate
        if (shouldRain != _lastRainState || rainGameObject.activeSelf != shouldRain)
        {
            ApplyRainState(shouldRain);
        }

        // --- 3. Audio (Ambianta Zi/Noapte) ---
        HandleAmbiance(timeState);
    }

    void ApplyRainState(bool isRaining)
    {
        _lastRainState = isRaining;
        
        // AICI E TRUCUL: Stingem/Aprindem obiectul cu totul.
        // Nu mai exista "StopEmitting", "Clear", "Play". Doar ON/OFF.
        rainGameObject.SetActive(isRaining);
        
        RenderSettings.fog = isRaining;

        // Audio volum
        if (rainSoundSource != null)
        {
            rainSoundSource.volume = isRaining ? 0.6f : 0f;
        }
        
        // Debug util
        // Debug.Log($"[Weather] Ploaie setata la: {isRaining}");
    }

    void HandleAmbiance(DayNightCycle.TimeState state)
    {
        AudioClip targetClip =
            (state == DayNightCycle.TimeState.Night || state == DayNightCycle.TimeState.Dusk)
                ? nightAmbiance
                : dayAmbiance;

        if (ambianceSource != null && targetClip != null && ambianceSource.clip != targetClip)
        {
            ambianceSource.clip = targetClip;
            ambianceSource.Play();
        }
    }
}