
using System.Collections.Generic;
using UnityEngine;

public class FoodScatterer : MonoBehaviour
{
    public Terrain terrain;

    [Header("Mushroom prefabs")]
    public List<GameObject> foodPrefabs = new();

    [Header("Spawn")]
    public int count = 200;
    public int seed = 1234;
    [Range(0f, 45f)] public float maxSlope = 25f;
    public float minDistance = 1.5f;

    [Header("Random")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    public bool randomRotation = true;

    [Header("Parent")]
    public Transform parent;
    public bool clearPrevious = true;

    [Header("Ground check (snap to Ground layer)")]
    public LayerMask groundLayer;       // Inspector: Ground
    public float raycastHeight = 5f;
    public float groundOffset = 0.02f;

    [Header("Avoid Water (XZ inside collider => reject)")]
    public bool avoidWater = true;
    public LayerMask waterMask;         // Inspector: Water
    public float waterMargin = 0.5f;    // margin pe XZ ca sa nu fie pe margine

    [Header("XR / Physics")]
    [Tooltip("Daca e TRUE, NU modificam rigidbody-ul (recomandat pentru XR Grab ca sa cada corect).")]
    public bool xrGrabFriendly = true;

    [Header("Debug")]
    public bool debugLogs = false;
    public bool debugDrawRejected = false;

    private readonly List<Vector3> usedPositions = new();
    private readonly List<Collider> _waterColliders = new();

    private void Awake()
    {
        if (avoidWater) RebuildWaterCache();
    }

    [ContextMenu("Rebuild Water Cache")]
    public void RebuildWaterCache()
    {
        _waterColliders.Clear();

        var all = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        int wm = waterMask.value;

        foreach (var c in all)
        {
            if (!c) continue;
            if (((1 << c.gameObject.layer) & wm) == 0) continue;
            _waterColliders.Add(c);
        }

        if (debugLogs)
            Debug.Log($"[FoodScatterer] Water cache: {_waterColliders.Count} colliders in mask.");
    }

    [ContextMenu("Scatter Food")]
    public void ScatterFood()
    {
        if (!terrain) terrain = FindFirstObjectByType<Terrain>();
        if (!terrain)
        {
            Debug.LogError("FoodScatterer: Terrain not assigned/found.");
            return;
        }
        if (foodPrefabs == null || foodPrefabs.Count == 0)
        {
            Debug.LogError("FoodScatterer: No food prefabs assigned.");
            return;
        }

        if (!parent)
        {
            var go = GameObject.Find("Food");
            if (!go) go = new GameObject("Food");
            parent = go.transform;
        }

        if (clearPrevious)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                DestroyImmediate(parent.GetChild(i).gameObject);
#else
                Destroy(parent.GetChild(i).gameObject);
#endif
            }
        }

        if (avoidWater && _waterColliders.Count == 0)
            RebuildWaterCache();

        Random.InitState(seed);
        usedPositions.Clear();

        var td = terrain.terrainData;
        var tPos = terrain.transform.position;

        int placed = 0;
        int attempts = 0;
        int maxAttempts = count * 120;
        float minDistSqr = minDistance * minDistance;

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;

            float x = Random.Range(0f, td.size.x);
            float z = Random.Range(0f, td.size.z);

            float worldX = tPos.x + x;
            float worldZ = tPos.z + z;

            // panta din normal Terrain
            Vector3 normal = td.GetInterpolatedNormal(x / td.size.x, z / td.size.z);
            float slope = Vector3.Angle(normal, Vector3.up);
            if (slope > maxSlope) continue;

            // aproximare Y
            float approxY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + tPos.y;

            // Raycast pe Ground
            Vector3 rayOrigin = new Vector3(worldX, approxY + raycastHeight, worldZ);
            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer, QueryTriggerInteraction.Ignore))
                continue;

            Vector3 pos = hit.point + Vector3.up * groundOffset;

            // ✅ NU in apa (XZ only)
            if (avoidWater && IsPointInsideAnyWaterXZ(pos, waterMargin))
            {
                if (debugDrawRejected)
                    Debug.DrawLine(pos + Vector3.up * 0.1f, pos + Vector3.up * 2f, Color.cyan, 1.0f);
                continue;
            }

            // distanta minima
            bool tooClose = false;
            for (int i = 0; i < usedPositions.Count; i++)
            {
                if ((usedPositions[i] - pos).sqrMagnitude < minDistSqr)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // instantiate
            GameObject prefab = foodPrefabs[Random.Range(0, foodPrefabs.Count)];
            if (!prefab) continue;

            GameObject item = Instantiate(prefab, pos, Quaternion.identity, parent);

            if (randomRotation)
                item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float s = Random.Range(scaleRange.x, scaleRange.y);
            item.transform.localScale = Vector3.one * s;

            // tag + collectible
            TrySetTag(item, "Food");
            bool setOk = SetCollectibleFoodEverywhere(item);

            // ✅ XR fix: NU face isKinematic true permanent
            if (!xrGrabFriendly)
                MakeStaticIfNeeded(item);

            if (debugLogs)
                Debug.Log($"[FoodScatterer] Spawned {item.name} | setFood={setOk}");

            usedPositions.Add(pos);
            placed++;
        }

        Debug.Log($"FoodScatterer: spawned {placed}/{count} items in {attempts} attempts.");
    }

    // ---------------- WATER (XZ ONLY) ----------------
    bool IsPointInsideAnyWaterXZ(Vector3 p, float margin)
    {
        if (_waterColliders.Count == 0) return false;

        for (int i = 0; i < _waterColliders.Count; i++)
        {
            var c = _waterColliders[i];
            if (!c) continue;

            if (c is BoxCollider bc)
            {
                if (IsInsideBoxColliderXZ(p, bc, margin)) return true;
            }
            else
            {
                var b = c.bounds;
                if (p.x >= b.min.x - margin && p.x <= b.max.x + margin &&
                    p.z >= b.min.z - margin && p.z <= b.max.z + margin)
                    return true;
            }
        }
        return false;
    }

    bool IsInsideBoxColliderXZ(Vector3 worldPoint, BoxCollider bc, float margin)
    {
        Vector3 local = bc.transform.InverseTransformPoint(worldPoint);
        Vector3 c = bc.center;
        Vector3 half = bc.size * 0.5f;

        bool insideX = Mathf.Abs(local.x - c.x) <= (half.x + margin);
        bool insideZ = Mathf.Abs(local.z - c.z) <= (half.z + margin);
        return insideX && insideZ;
    }

    // ---------------- COLLECTIBLE + TAG ----------------
    private bool SetCollectibleFoodEverywhere(GameObject root)
    {
        bool changed = false;

        var all = root.GetComponentsInChildren<CollectibleItem>(true);
        if (all != null && all.Length > 0)
        {
            foreach (var c in all)
            {
                c.itemType = ItemType.Food;
                TrySetTag(c.gameObject, "Food");
                changed = true;
            }
            return changed;
        }

        var collectible = root.GetComponent<CollectibleItem>();
        if (!collectible) collectible = root.AddComponent<CollectibleItem>();
        collectible.itemType = ItemType.Food;
        return true;
    }

    private void TrySetTag(GameObject go, string tag)
    {
        try { go.tag = tag; }
        catch { Debug.LogWarning($"Tag '{tag}' nu exista. Creeaza-l in Tags & Layers."); }
    }

    // ---------------- OPTIONAL STATIC MODE (NON-XR) ----------------
    private void MakeStaticIfNeeded(GameObject root)
    {
        // daca chiar vrei obiecte “decor”, nu interactibile
        var rbs = root.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rbs)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }
}
