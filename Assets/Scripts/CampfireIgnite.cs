using UnityEngine;
using UnityEngine.XR;

public class CampfireIgnite : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private ParticleSystem smokeParticles;
    [SerializeField] private Light fireLight;

    [Header("Ignite Settings")]
    [SerializeField] private KeyCode igniteKey = KeyCode.F;
    [SerializeField] private float triggerThreshold = 0.8f;
    [SerializeField] private float lightIntensityOn = 4f;
    [SerializeField] private float lightRangeOn = 9f;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private bool useRootTagCheck = true;
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;
    private bool isLit;
    private bool triggerWasPressed;

    private InputDevice rightHand;

    void Start()
    {
        StopFx();
        TryGetRightHand();
    }

    void Update()
    {
        if (isLit || !playerInRange) return;

        // NON-VR ? tastatura
        if (!XRSettings.isDeviceActive)
        {
            if (Input.GetKeyDown(igniteKey))
            {
                Log("Ignite via keyboard (F)");
                Ignite();
            }
            return;
        }

        // VR ? trigger
        if (!rightHand.isValid)
            TryGetRightHand();

        if (rightHand.isValid &&
            rightHand.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            bool pressed = triggerValue > triggerThreshold;

            if (pressed && !triggerWasPressed)
            {
                Log("Ignite via VR trigger");
                Ignite();
            }

            triggerWasPressed = pressed;
        }
    }

    void TryGetRightHand()
    {
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        Log($"Right hand valid: {rightHand.isValid}");
    }

    private void Ignite()
    {
        isLit = true;

        if (fireParticles) fireParticles.Play(true);
        if (smokeParticles) smokeParticles.Play(true);

        if (fireLight)
        {
            fireLight.enabled = true;
            fireLight.intensity = lightIntensityOn;
            fireLight.range = lightRangeOn;
        }
    }

    private void StopFx()
    {
        if (fireParticles)
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (smokeParticles)
            smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (fireLight)
        {
            fireLight.enabled = false;
            fireLight.intensity = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other))
            playerInRange = false;
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (!other) return false;
        Transform root = other.transform.root;
        return root && root.CompareTag(playerTag);
    }

    private void Log(string msg)
    {
        if (!enableDebug) return;
        Debug.Log($"[CampfireIgnite] {msg}", this);
    }
}
