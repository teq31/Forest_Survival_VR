using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [Header("Day/Night Integration")]
    public DayNightCycle dayNightCycle; 
    
    [Header("Visual Effects")]
    public GameObject rainParticleSystem; 
    
    [Header("Audio Sources")]
    public AudioSource ambianceSource;
    public AudioSource rainSoundSource; 

    [Header("Audio Clips")]
    public AudioClip dayAmbiance; 
    public AudioClip nightAmbiance; 
    public AudioClip rainLoop; 

    private bool isRaining = false;
    private ParticleSystem rainParticles;

    void Start()
    {
        if (rainParticleSystem != null) 
        {
            rainParticles = rainParticleSystem.GetComponent<ParticleSystem>();
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
        
        SetRainActive(false);
    }

    void Update()
    {
        if (dayNightCycle == null) return;

        DayNightCycle.TimeState state = dayNightCycle.CurrentTimeState; 
        
        HandleAmbianceSwitch(state);
        HandleWeather(state); 
    }
    
    void HandleAmbianceSwitch(DayNightCycle.TimeState state)
    {
        if (state == DayNightCycle.TimeState.Night || state == DayNightCycle.TimeState.Dusk) 
        {
            if (ambianceSource.clip != nightAmbiance && nightAmbiance != null)
            {
                ambianceSource.clip = nightAmbiance;
                ambianceSource.Play();
            }
        }
        else 
        {
            if (ambianceSource.clip != dayAmbiance && dayAmbiance != null)
            {
                ambianceSource.clip = dayAmbiance;
                ambianceSource.Play();
            }
        }
    }

    void HandleWeather(DayNightCycle.TimeState state)
    {
        bool shouldRain = (state == DayNightCycle.TimeState.Night);

        if (shouldRain && !isRaining)
        {
            SetRainActive(true);
        }
        else if (!shouldRain && isRaining)
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
            if (active)
            {
                var emission = rainParticles.emission;
                emission.enabled = true;
                rainParticles.Clear();
                rainParticles.Play();
            }
            else
            {
                var emission = rainParticles.emission;
                emission.enabled = false;
                rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    
        if (rainSoundSource != null)
            rainSoundSource.volume = active ? 0.6f : 0f;
    }
}