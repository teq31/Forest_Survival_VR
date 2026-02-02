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

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private bool logUpdateWhenInRange = true;
    [SerializeField] private bool useRootTagCheck = true; // XR-friendly
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;
    private bool isLit;

    private void Awake()
    {
        Log($"Awake on '{name}'. Scene object active={gameObject.activeInHierarchy}");
        ValidateSetup();
        StopFx();
    }

    private void OnEnable()
    {
        Log($"OnEnable on '{name}'.");
    }

    private void Start()
    {
        Log($"Start on '{name}'. Press '{igniteKey}' while in trigger range.");
    }

    private void Update()
    {
        if (isLit) return;

        // Key detection debug
        if (Input.GetKeyDown(igniteKey))
            Log($"KeyDown detected: {igniteKey} (focus should be on Game view). playerInRange={playerInRange}");

        if (playerInRange)
        {
            if (logUpdateWhenInRange)
                Log($"In range. Waiting for '{igniteKey}'. isLit={isLit}");

            if (Input.GetKeyDown(igniteKey))
                Ignite();
        }
    }

    private void Ignite()
    {
        Log("Ignite() called.");
        isLit = true;

        if (fireParticles != null)
        {
            fireParticles.Play(true);
            Log($"fireParticles.Play() ok. isPlaying={fireParticles.isPlaying}");
        }
        else Log("fireParticles is NULL");

        if (smokeParticles != null)
        {
            smokeParticles.Play(true);
            Log($"smokeParticles.Play() ok. isPlaying={smokeParticles.isPlaying}");
        }
        else Log("smokeParticles is NULL");

        if (fireLight != null)
        {
            fireLight.enabled = true;
            fireLight.intensity = lightIntensityOn;
            fireLight.range = lightRangeOn;
            Log($"fireLight enabled. intensity={fireLight.intensity}, range={fireLight.range}");
        }
        else Log("fireLight is NULL");
    }

    private void StopFx()
    {
        Log("StopFx() called.");

        if (fireParticles != null)
        {
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Log("fireParticles stopped.");
        }
        else Log("fireParticles is NULL");

        if (smokeParticles != null)
        {
            smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Log("smokeParticles stopped.");
        }
        else Log("smokeParticles is NULL");

        if (fireLight != null)
        {
            fireLight.enabled = false;
            fireLight.intensity = 0f;
            Log("fireLight disabled.");
        }
        else Log("fireLight is NULL");
    }

    private void OnTriggerEnter(Collider other)
    {
        LogTrigger("ENTER", other);

        bool isPlayer = IsPlayerCollider(other);
        Log($"ENTER check -> isPlayer={isPlayer}");

        if (isPlayer)
        {
            playerInRange = true;
            Log("playerInRange set TRUE.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        LogTrigger("EXIT", other);

        bool isPlayer = IsPlayerCollider(other);
        Log($"EXIT check -> isPlayer={isPlayer}");

        if (isPlayer)
        {
            playerInRange = false;
            Log("playerInRange set FALSE.");
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;

        if (!useRootTagCheck)
            return other.CompareTag(playerTag);

        // XR-friendly: collider is often on a child, but root is tagged
        Transform root = other.transform.root;
        if (root == null) return other.CompareTag(playerTag);

        return root.CompareTag(playerTag) || other.CompareTag(playerTag);
    }

    private void ValidateSetup()
    {
        // Collider / Rigidbody sanity checks
        Collider col = GetComponent<Collider>();
        if (col == null) Log("WARNING: No Collider on campfire object. Triggers will not work.");
        else Log($"Collider found: {col.GetType().Name}, isTrigger={col.isTrigger}");

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) Log("WARNING: No Rigidbody on campfire object. Trigger may not fire depending on other object.");
        else Log($"Rigidbody found: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}");

        // Reference checks
        Log($"Refs: fireParticles={(fireParticles ? fireParticles.name : "NULL")}, " +
            $"smokeParticles={(smokeParticles ? smokeParticles.name : "NULL")}, " +
            $"fireLight={(fireLight ? fireLight.name : "NULL")}");

        // Layer info
        Log($"Campfire layer={LayerMask.LayerToName(gameObject.layer)}({gameObject.layer})");
    }

    private void LogTrigger(string type, Collider other)
    {
        if (!enableDebug) return;

        string otherName = other ? other.name : "NULL";
        string otherTag = other ? other.tag : "NULL";
        int otherLayer = other ? other.gameObject.layer : -1;
        string otherLayerName = other ? LayerMask.LayerToName(otherLayer) : "NULL";
        string otherRoot = (other && other.transform.root) ? other.transform.root.name : "NULL";

        Debug.Log($"[CampfireIgnite] Trigger {type} on '{name}' by '{otherName}' tag='{otherTag}' " +
                  $"layer='{otherLayerName}'({otherLayer}) root='{otherRoot}'",
                  this);
    }

    private void Log(string msg)
    {
        if (!enableDebug) return;
        Debug.Log($"[CampfireIgnite] {msg}", this);
    }
}
