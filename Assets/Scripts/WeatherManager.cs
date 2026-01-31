using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [Header("Integrari")]
    public DayNightCycle dayNightCycle;
    public SurvivalManager survivalManager; // optional, îl păstrăm
    public StressSystem stressSystem;        // calm/agitație (VR + non-VR)

    [Header("Efecte si Audio")]
    public GameObject rainParticleSystem;
    public AudioSource ambianceSource;
    public AudioSource rainSoundSource;

    [Header("Clipuri")]
    public AudioClip dayAmbiance;
    public AudioClip nightAmbiance;
    public AudioClip rainLoop;

    [Header("Rain rules")]
    [Tooltip("Rain will play in Night/Dusk only if Stress01 is BELOW this threshold.")]
    [Range(0f, 1f)]
    public float maxStressForRain = 0.30f; // calm if stress <= 0.30

    [Tooltip("If true, rain can run during Dusk as well.")]
    public bool allowRainInDusk = true;

    [Header("Smoothing")]
    [Tooltip("Smoothing for stress used by weather (prevents flicker).")]
    public float stressEmaSpeed = 4f;

    private bool isRaining = false;
    private ParticleSystem rainParticles;
    private float _stressEma01 = 0f;

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

        if (stressSystem == null) stressSystem = FindFirstObjectByType<StressSystem>();
    }

    void Update()
    {
        if (dayNightCycle == null) return;

        var state = dayNightCycle.CurrentTimeState;

        HandleAmbianceSwitch(state);
        HandleWeather(state);
    }

    void HandleAmbianceSwitch(DayNightCycle.TimeState state)
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

    void HandleWeather(DayNightCycle.TimeState state)
    {
        bool isNightOrDusk = (state == DayNightCycle.TimeState.Night) ||
                             (allowRainInDusk && state == DayNightCycle.TimeState.Dusk);

        float stress01 = (stressSystem != null) ? Mathf.Clamp01(stressSystem.Stress01) : 0f;
        _stressEma01 = Mathf.Lerp(_stressEma01, stress01, 1f - Mathf.Exp(-stressEmaSpeed * Time.deltaTime));

        bool isCalm = (_stressEma01 <= maxStressForRain);
        bool shouldRainNow = isNightOrDusk && isCalm;

        if (shouldRainNow && !isRaining) SetRainActive(true);
        else if (!shouldRainNow && isRaining) SetRainActive(false);
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
