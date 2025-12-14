using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    // Asigură-te că DayNightCycle este atașat aici!
    [Header("Day/Night Integration")]
    [Tooltip("Referința la scriptul de ciclu zi/noapte pentru a citi starea curentă.")]
    public DayNightCycle dayNightCycle; 
    
    [Header("Visual Effects")]
    [Tooltip("Obiectul Particle System al ploii (copil al Main Camera, dezactivat inițial).")]
    public GameObject rainParticleSystem; 
    
    [Header("Audio Sources")]
    [Tooltip("Audio Source-ul pentru sunetele ambientale (2D).")]
    public AudioSource ambianceSource;
    [Tooltip("Audio Source-ul pentru sunetul de ploaie (2D).")]
    public AudioSource rainSoundSource; 

    [Header("Audio Clips")]
    public AudioClip dayAmbiance; 
    public AudioClip nightAmbiance; 
    public AudioClip rainLoop; 

    private bool isRaining = false;
    private ParticleSystem rainParticles; // Referință la componenta ParticleSystem

    
    void Start()
    {
        // 1. Obține componenta ParticleSystem și oprește-o la start
        if (rainParticleSystem != null) 
        {
            // Presupune că ParticleSystem este atașat la GameObject-ul rainParticleSystem
            rainParticles = rainParticleSystem.GetComponent<ParticleSystem>();
            
            rainParticleSystem.SetActive(false); // Dezactivează GameObject-ul

            if (rainParticles != null)
            {
                // Oprește imediat emisia și curăță particulele deja emise (pentru a preveni glich-urile)
                rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // 2. Setează și pornește sunetul de ambient (Zi inițial)
        if (ambianceSource != null && dayAmbiance != null)
        {
            ambianceSource.clip = dayAmbiance;
            ambianceSource.loop = true;
            ambianceSource.Play();
        }

        // 3. Setează sunetul de ploaie (mute inițial)
        if (rainSoundSource != null && rainLoop != null)
        {
            rainSoundSource.clip = rainLoop;
            rainSoundSource.loop = true;
            rainSoundSource.volume = 0f; 
            rainSoundSource.Play();
        }
        
        // 4. Asigură că ceața globală este dezactivată la începutul zilei
        RenderSettings.fog = false;
    }

    void Update()
    {
        if (dayNightCycle == null) return;

        // Citește starea curentă direct din DayNightCycle
        DayNightCycle.TimeState state = dayNightCycle.CurrentTimeState; 
        
        HandleAmbianceSwitch(state);
        HandleWeather(state); 
    }
    
    void HandleAmbianceSwitch(DayNightCycle.TimeState state)
    {
        // Treci la sunetul de Noapte în fazele Dusk și Night
        if (state == DayNightCycle.TimeState.Night || state == DayNightCycle.TimeState.Dusk) 
        {
            if (ambianceSource.clip != nightAmbiance && nightAmbiance != null)
            {
                ambianceSource.clip = nightAmbiance;
                ambianceSource.Play();
            }
        }
        // Treci la sunetul de Zi în fazele Day și Dawn
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
        // Ploaia și Ceața se activează în fazele de tranziție (Dusk) și Noapte
        if (state == DayNightCycle.TimeState.Dusk || state == DayNightCycle.TimeState.Night) 
        {
            if (!isRaining)
            {
                // Activează ploaia, sunetul și ceața
                SetRainActive(true);
                RenderSettings.fog = true; 
            }
        }
        else // Oprește ploaia în Dawn și Day
        {
            if (isRaining)
            {
                // Dezactivează ploaia, sunetul și ceața
                SetRainActive(false);
                RenderSettings.fog = false; 
            }
        }
    }

    void SetRainActive(bool active)
    {
        isRaining = active;
        
        if (rainParticleSystem != null)
            rainParticleSystem.SetActive(active); // Activează/dezactivează vizualul

        // Controlează emisia particulelor
        if (rainParticles != null)
        {
            if (active)
            {
                rainParticles.Play(); // Pornește emisia
            }
            else
            {
                // Oprește emisia și curăță particulele rămase
                rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 
            }
        }
        
        // Volumul 0.6 pentru activ (se aude), 0 pentru dezactivat (silent)
        rainSoundSource.volume = active ? 0.6f : 0f;
    }
}