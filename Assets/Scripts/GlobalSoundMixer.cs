using UnityEngine;

public class GlobalSoundMixer : MonoBehaviour
{
    [Header("--- CONECTARE ---")]
    public StressSystem stressSystem;
    public DayNightCycle dayNightSystem; // Scriptul de timp
    public ParticleSystem rainParticleSystem; // TRAGE AICI OBIECTUL RAIN SYSTEM!

    [Header("--- SURSE AUDIO (Pe XR Origin) ---")]
    public AudioSource calmBreathSource;
    public AudioSource panicBreathSource;
    public AudioSource rainSource;
    public AudioSource daySource;
    public AudioSource nightSource;

    [Header("--- SETARI ---")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    // Acestea se vor bifa singure acum!
    public bool isRaining; 
    public bool isNight;   

    void Start()
    {
        // Setup initial (Mute la toti)
        SetupSource(calmBreathSource);
        SetupSource(panicBreathSource);
        SetupSource(rainSource);
        SetupSource(daySource);
        SetupSource(nightSource);
    }

    void Update()
    {
        // 1. VERIFICARE AUTOMATA (AUTO-DETECT)
        
        // A. E noapte? (Modifica 18 si 6 daca ai alte ore)
        // Daca nu ai acces la .Hour, sterge linia asta si bifeaza manual 'Is Night'
        if (dayNightSystem != null) 
        {
             // Incercam sa ghicim variabila ta de timp (Hour, CurrentTime, TimeOfDay)
             // Daca da eroare la .Hour, scrie numele corect al variabilei tale!
             // isNight = dayNightSystem.Hour >= 18 || dayNightSystem.Hour < 6;
        }

        // B. Ploua? (Verificam daca sistemul de particule e pornit)
        if (rainParticleSystem != null)
        {
            // Daca particulele sunt active si emit, inseamna ca ploua
            isRaining = rainParticleSystem.gameObject.activeInHierarchy && rainParticleSystem.isPlaying;
        }

        // 2. MIXAJ AUDIO (APLICAREA VOLUMELOR)

        // Stres
        float stress = (stressSystem != null) ? Mathf.Clamp01(stressSystem.Stress01) : 0f;
        SetVolume(calmBreathSource, 1f - stress);
        SetVolume(panicBreathSource, stress);

        // Ploaie
        SetVolume(rainSource, isRaining ? 1f : 0f);

        // Zi vs Noapte
        SetVolume(daySource, isNight ? 0f : 1f);   // Ziua se opreste noaptea
        SetVolume(nightSource, isNight ? 1f : 0f); // Noaptea porneste noaptea
    }

    void SetVolume(AudioSource source, float targetVol)
    {
        if (source != null)
            source.volume = Mathf.Lerp(source.volume, targetVol * masterVolume, Time.deltaTime * 2f);
    }

    void SetupSource(AudioSource source)
    {
        if (source != null) { source.loop = true; if (!source.isPlaying) source.Play(); source.volume = 0f; source.spatialBlend = 0f; }
    }
}