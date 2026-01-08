using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [Header("Integrari")]
    public DayNightCycle dayNightCycle; 
    public SurvivalManager survivalManager; 
    
    [Header("Efecte si Audio")]
    public GameObject rainParticleSystem; 
    public AudioSource ambianceSource;
    public AudioSource rainSoundSource; 

    [Header("Clipuri")]
    public AudioClip dayAmbiance; 
    public AudioClip nightAmbiance; 
    public AudioClip rainLoop; 

    [Header("Setari Stres")]
    public float minCalmForRain = 75f;

    private bool isRaining = false;
    private ParticleSystem rainParticles;

    void Start()
    {
        if (rainParticleSystem != null) 
        {
            rainParticles = rainParticleSystem.GetComponent<ParticleSystem>();
            rainParticleSystem.SetActive(true); 

            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                emission.enabled = false;
                rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

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
    }

    void Update()
    {
        if (dayNightCycle == null || survivalManager == null) return;

        DayNightCycle.TimeState state = dayNightCycle.CurrentTimeState; 
        
        HandleAmbianceSwitch(state);
        HandleWeather(state); 
    }
    
    void HandleAmbianceSwitch(DayNightCycle.TimeState state)
    {
        AudioClip targetClip = (state == DayNightCycle.TimeState.Night || state == DayNightCycle.TimeState.Dusk) ? nightAmbiance : dayAmbiance;

        if (ambianceSource != null && ambianceSource.clip != targetClip)
        {
            ambianceSource.clip = targetClip;
            ambianceSource.Play();
        }
    }

    void HandleWeather(DayNightCycle.TimeState state)
    {
        bool isNight = (state == DayNightCycle.TimeState.Night);
        
        // Ploaia porneste doar daca jucatorul este CALM (peste 75)
        // Daca scade sub 75 (incepe sa se panicheze), ploaia se opreste
        bool isCalm = (survivalManager.currentPanic >= minCalmForRain);

        bool shouldRainNow = isNight && isCalm;

        if (shouldRainNow && !isRaining)
        {
            SetRainActive(true);
        }
        else if (!shouldRainNow && isRaining)
        {
            SetRainActive(false);
        }
    }

    void SetRainActive(bool active)
    {
        isRaining = active;
        RenderSettings.fog = active;

        if (rainParticles != null)
        {
            var emission = rainParticles.emission;
            emission.enabled = active;

            if (active)
            {
                if (!rainParticles.isPlaying) rainParticles.Play(true);
            }
            else
            {
                rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        
        if (rainSoundSource != null)
        {
            rainSoundSource.volume = active ? 0.6f : 0f;
        }
    }
}