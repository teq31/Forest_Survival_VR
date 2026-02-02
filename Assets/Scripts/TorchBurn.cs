using System.Collections;
using UnityEngine;

public class TorchBurn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private ParticleSystem smokeParticles;
    [SerializeField] private Light torchLight;

    [Header("Timing")]
    [SerializeField] private float burnSeconds = 60f;
    [SerializeField] private float lingerAfterOutSeconds = 3f;

    [Header("Light Settings")]
    [SerializeField] private float lightIntensityOn = 3.5f;
    [SerializeField] private float lightRangeOn = 8f;

    private Coroutine routine;

    private void OnEnable()
    {
        // start burning immediately when spawned
        StartBurning();
    }

    public void StartBurning()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(BurnRoutine());
    }

    private IEnumerator BurnRoutine()
    {
        // turn ON
        SetLit(true);

        // burn time
        yield return new WaitForSeconds(burnSeconds);

        // turn OFF
        SetLit(false);

        // linger
        yield return new WaitForSeconds(lingerAfterOutSeconds);

        // destroy torch object (it will disappear from hand)
        Destroy(gameObject);
    }

    private void SetLit(bool on)
    {
        if (fireParticles != null)
        {
            if (on) fireParticles.Play(true);
            else fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (smokeParticles != null)
        {
            if (on) smokeParticles.Play(true);
            else smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (torchLight != null)
        {
            torchLight.enabled = on;
            if (on)
            {
                torchLight.intensity = lightIntensityOn;
                torchLight.range = lightRangeOn;
            }
        }
    }
}