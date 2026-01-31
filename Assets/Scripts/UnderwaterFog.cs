using UnityEngine;

public class UnderwaterFog : MonoBehaviour
{
    [Header("Who do we test? (Main Camera / XR Camera)")]
    public Transform target; // daca e null => transform-ul acestui GO

    [Header("Water detection")]
    public LayerMask waterMask;      // seteaza la layer-ul "Water"
    public float rayStartHeight = 50f;  // cat de sus porneste ray-ul (deasupra camerei)
    public float rayLength = 120f;      // cat de jos cauta
    public float surfaceOffset = 0f;    // daca vrei pragul putin mai sus/jos

    [Header("Fog underwater")]
    public Color underwaterFogColor = new Color(0.05f, 0.3f, 0.4f, 1f);
    public float underwaterFogDensity = 0.08f;

    [Header("Stability (avoid flicker near surface)")]
    public float hysteresis = 0.05f;

    [Header("Debug")]
    public bool debugLog = false;

    private bool isUnderwater;

    // saved defaults
    private bool fogEnabledBefore;
    private Color fogColorBefore;
    private float fogDensityBefore;
    private FogMode fogModeBefore;

    void Awake()
    {
        if (!target) target = transform;
    }

    void Start()
    {
        fogEnabledBefore = RenderSettings.fog;
        fogColorBefore = RenderSettings.fogColor;
        fogDensityBefore = RenderSettings.fogDensity;
        fogModeBefore = RenderSettings.fogMode;
    }

    void Update()
    {
        Vector3 p = target.position;

        // Ray pornit de sus (fix ca sa nu rateze daca esti sub apa)
        Vector3 rayOrigin = new Vector3(p.x, p.y + rayStartHeight, p.z);

        bool overWater = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            rayLength,
            waterMask,
            QueryTriggerInteraction.Collide // IMPORTANT: merge si pe trigger water
        );

        bool nowUnderwater = false;

        if (overWater)
        {
            float waterY = hit.point.y + surfaceOffset;

            // hysteresis: cand esti underwater, nu iesi instant la micro miscari
            float threshold = waterY + (isUnderwater ? hysteresis : -hysteresis);

            nowUnderwater = p.y < threshold;

            if (debugLog)
                Debug.Log($"[UnderwaterFog] posY={p.y:0.00} waterY={waterY:0.00} overWater=True underwater={nowUnderwater} hit={hit.collider.name}");
        }
        else
        {
            if (debugLog)
                Debug.Log($"[UnderwaterFog] overWater=False pos={p}");
        }

        if (nowUnderwater != isUnderwater)
        {
            isUnderwater = nowUnderwater;
            ApplyFog(isUnderwater);
        }
    }

    void ApplyFog(bool underwater)
    {
        if (underwater)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = underwaterFogColor;
            RenderSettings.fogDensity = underwaterFogDensity;
        }
        else
        {
            RenderSettings.fog = fogEnabledBefore;
            RenderSettings.fogMode = fogModeBefore;
            RenderSettings.fogColor = fogColorBefore;
            RenderSettings.fogDensity = fogDensityBefore;
        }
    }
}
