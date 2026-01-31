using UnityEngine;
using System.Reflection;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class SimulatorSlopeSpeed : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public XRDeviceSimulator deviceSimulator;
    public LayerMask groundMask;

    [Header("Base sensitivity (flat)")]
    public float baseKeyboardXZ = 0.6f;

    [Header("Slope thresholds (gradient = |dh|/distance)")]
    [Tooltip("Sub acest gradient, considerăm terenul aproape plat (nu încetinim).")]
    public float slopeFlat = 0.03f;

    [Tooltip("Peste acest gradient, considerăm terenul foarte abrupt (max penalty/boost).")]
    public float slopeSteep = 0.22f;

    [Header("Speed limits (multipliers)")]
    [Tooltip("Pe urcare foarte abruptă: baseKeyboardXZ * minMultiplierUp")]
    public float minMultiplierUp = 0.35f;

    [Tooltip("Pe coborâre foarte abruptă: baseKeyboardXZ * maxMultiplierDown")]
    public float maxMultiplierDown = 1.6f;

    [Tooltip("Pe plat: baseKeyboardXZ * flatMultiplier (de obicei 1).")]
    public float flatMultiplier = 1f;

    [Header("Sampling")]
    public float sampleDistance = 2.0f;
    public float raycastUp = 2f;
    public float raycastDown = 25f;

    [Header("Smoothing")]
    [Tooltip("Cât de repede se schimbă sensibilitatea spre țintă (mai mare = mai rapid).")]
    public float lerpSpeed = 8f;

    [Header("Debug")]
    public bool debugLogs = false;
    public float debugInterval = 0.5f;

    Vector3 lastCamPos;
    float dbgTimer;

    FieldInfo keyboardXField;
    FieldInfo keyboardZField;

    float currentXZ;

    void Start()
    {
        if (deviceSimulator == null)
            deviceSimulator = GetComponent<XRDeviceSimulator>();

        if (cameraTransform == null)
            Debug.LogError("[SimSlopeSpeed] cameraTransform NULL (setează Main Camera).");

        if (deviceSimulator == null)
            Debug.LogError("[SimSlopeSpeed] XRDeviceSimulator NULL (pune scriptul pe GO-ul XR Device Simulator).");

        if (cameraTransform != null)
            lastCamPos = cameraTransform.position;

        CacheSensitivityFields();

        currentXZ = baseKeyboardXZ * flatMultiplier;
        ApplyXZ(currentXZ);

        if (debugLogs)
        {
            Debug.Log($"[SimSlopeSpeed] Simulator={deviceSimulator != null}, Cam={cameraTransform != null}");
            Debug.Log($"[SimSlopeSpeed] Found fields: X={(keyboardXField != null ? keyboardXField.Name : "NULL")} Z={(keyboardZField != null ? keyboardZField.Name : "NULL")}");
        }
    }

    void Update()
    {
        if (cameraTransform == null || deviceSimulator == null) return;

        dbgTimer += Time.deltaTime;

        // Mișcarea reală e pe CAMERA (simulator)
        Vector3 movement = cameraTransform.position - lastCamPos;
        lastCamPos = cameraTransform.position;

        // Dacă nu te miști, revino spre flat
        if (movement.sqrMagnitude < 0.000001f)
        {
            float targetFlat = baseKeyboardXZ * flatMultiplier;
            currentXZ = Mathf.Lerp(currentXZ, targetFlat, Time.deltaTime * lerpSpeed);
            ApplyXZ(currentXZ);

            if (debugLogs && dbgTimer >= debugInterval)
            {
                Debug.Log($"[SimSlopeSpeed] movement=0 -> targetFlat={targetFlat:F2}, current={currentXZ:F2}");
                dbgTimer = 0;
            }
            return;
        }

        // Direcția reală de mers (orizontal)
        Vector3 dir = movement;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.000001f)
            return;

        dir.Normalize();

        // Măsurăm solul sub cameră și în față
        bool ok0 = TryGetGroundHeight(cameraTransform.position, out float h0);
        bool ok1 = TryGetGroundHeight(cameraTransform.position + dir * sampleDistance, out float h1);

        if (!ok0 || !ok1)
        {
            float targetFlat = baseKeyboardXZ * flatMultiplier;
            currentXZ = Mathf.Lerp(currentXZ, targetFlat, Time.deltaTime * lerpSpeed);
            ApplyXZ(currentXZ);

            if (debugLogs && dbgTimer >= debugInterval)
            {
                Debug.Log($"[SimSlopeSpeed] raycast failed ok0={ok0} ok1={ok1} -> flat");
                dbgTimer = 0;
            }
            return;
        }

        float dh = h1 - h0;

        // Gradient: cât urcă/coboară pe metru
        float slope = Mathf.Abs(dh) / Mathf.Max(0.0001f, sampleDistance);

        // t=0 plat, t=1 foarte abrupt
        float t = Mathf.InverseLerp(slopeFlat, slopeSteep, slope);
        t = Mathf.Clamp01(t);

        // Curba: mic efect pe pante mici, efect puternic pe pante mari
        float curved = t * t; // poți schimba în Mathf.SmoothStep(0,1,t) dacă vrei mai “soft”

        float multiplier;
        string state;

        if (dh > 0f) // urcare
        {
            multiplier = Mathf.Lerp(flatMultiplier, minMultiplierUp, curved);
            state = "UPHILL";
        }
        else if (dh < 0f) // coborâre
        {
            multiplier = Mathf.Lerp(flatMultiplier, maxMultiplierDown, curved);
            state = "DOWNHILL";
        }
        else
        {
            multiplier = flatMultiplier;
            state = "FLAT";
        }

        float targetXZ = baseKeyboardXZ * multiplier;

        // smoothing ca să nu “pulseze” pe micro denivelări
        currentXZ = Mathf.Lerp(currentXZ, targetXZ, Time.deltaTime * lerpSpeed);
        ApplyXZ(currentXZ);

        if (debugLogs && dbgTimer >= debugInterval)
        {
            Debug.Log($"[SimSlopeSpeed] {state} dh={dh:F3} slope={slope:F3} t={t:F2} -> targetXZ={targetXZ:F2} currentXZ={currentXZ:F2}");
            dbgTimer = 0;
        }
    }

    void CacheSensitivityFields()
    {
        var t = deviceSimulator.GetType();
        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var f in fields)
        {
            if (f.FieldType != typeof(float)) continue;
            var n = f.Name.ToLowerInvariant();

            // Heuristici pentru multe versiuni
            if (keyboardXField == null && n.Contains("keyboard") && n.Contains("x") && n.Contains("translate"))
                keyboardXField = f;

            if (keyboardZField == null && n.Contains("keyboard") && n.Contains("z") && n.Contains("translate"))
                keyboardZField = f;

            // Alternative întâlnite în unele build-uri
            if (keyboardXField == null && n.Contains("keyboard") && n.Contains("x") && n.Contains("transl"))
                keyboardXField = f;

            if (keyboardZField == null && n.Contains("keyboard") && n.Contains("z") && n.Contains("transl"))
                keyboardZField = f;
        }

        if (debugLogs && (keyboardXField == null || keyboardZField == null))
        {
            Debug.LogWarning("[SimSlopeSpeed] N-am găsit fields direct. Listez toate float fields din XRDeviceSimulator:");
            foreach (var f in fields)
                if (f.FieldType == typeof(float))
                    Debug.Log("   float field: " + f.Name);
        }
    }

    void ApplyXZ(float xz)
    {
        if (keyboardXField != null) keyboardXField.SetValue(deviceSimulator, xz);
        if (keyboardZField != null) keyboardZField.SetValue(deviceSimulator, xz);
    }

    bool TryGetGroundHeight(Vector3 pos, out float groundY)
    {
        Vector3 origin = pos + Vector3.up * raycastUp;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastUp + raycastDown, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
            return true;
        }

        groundY = 0;
        return false;
    }
}
