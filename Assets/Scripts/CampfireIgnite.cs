using UnityEngine;

public class CampfireIgnite : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private ParticleSystem smokeParticles;
    [SerializeField] private Light fireLight;

    [Header("Ignite Settings")]
    [SerializeField] private KeyCode igniteKey = KeyCode.F;
    [SerializeField] private float lightIntensityOn = 4f;
    [SerializeField] private float lightRangeOn = 9f;

    private bool playerInRange;
    private bool isLit;

    private void Awake()
    {
        StopFx();
    }

    private void Update()
    {
        if (isLit) return;

        if (playerInRange && Input.GetKeyDown(igniteKey))
            Ignite();
    }

    private void Ignite()
    {
        isLit = true;

        if (fireParticles != null) fireParticles.Play(true);
        if (smokeParticles != null) smokeParticles.Play(true);

        if (fireLight != null)
        {
            fireLight.enabled = true;
            fireLight.intensity = lightIntensityOn;
            fireLight.range = lightRangeOn;
        }
    }

    private void StopFx()
    {
        if (fireParticles != null)
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (smokeParticles != null)
            smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (fireLight != null)
        {
            fireLight.enabled = false;
            fireLight.intensity = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }
}