using System.Collections.Generic;
using UnityEngine;

public class StoneScatterer : MonoBehaviour
{
    [Header("Terrain")]
    public Terrain terrain;

    [Header("Stone prefabs")]
    public List<GameObject> stonePrefabs = new();

    [Header("Spawn")]
    public int count = 150;
    public int seed = 2222;
    [Range(0f, 45f)] public float maxSlope = 30f;
    public float minDistance = 2.0f;
    public int maxAttemptsMultiplier = 120;

    [Header("Random")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.4f);
    public bool randomRotation = true;

    [Header("Parent")]
    public Transform parent;
    public bool clearPrevious = true;

    [Header("Ground snap")]
    public LayerMask groundLayer;      // set to "Ground"
    public float raycastHeight = 5f;
    public float groundOffset = 0.02f;

    [Header("Avoid water")]
    public LayerMask waterMask;        // set to "Water"
    public Vector3 waterCheckHalfExtents = new Vector3(0.35f, 1.5f, 0.35f);
    public QueryTriggerInteraction waterTriggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Physics (XR friendly)")]
    [Tooltip("Daca e TRUE, o sa le faci kinematic si vor ramane in aer la drop in XR. Recomand FALSE pentru pickup items.")]
    public bool freezeRigidbodyOnSpawn = false;

    private readonly List<Vector3> usedPositions = new();

    [ContextMenu("Scatter Stones")]
    public void ScatterStones()
    {
        if (!terrain) terrain = FindFirstObjectByType<Terrain>();
        if (!terrain)
        {
            Debug.LogError("StoneScatterer: Terrain not assigned/found.");
            return;
        }

        if (stonePrefabs == null || stonePrefabs.Count == 0)
        {
            Debug.LogError("StoneScatterer: No stone prefabs assigned.");
            return;
        }

        Random.InitState(seed);
        usedPositions.Clear();

        // parent
        if (!parent)
        {
            var go = GameObject.Find("Stones");
            if (!go) go = new GameObject("Stones");
            parent = go.transform;
        }

        // clear
        if (clearPrevious)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                DestroyImmediate(parent.GetChild(i).gameObject);
        }

        var td = terrain.terrainData;
        var tPos = terrain.transform.position;

        int placed = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(count * maxAttemptsMultiplier, 5000);

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;

            // random in terrain bounds
            float x = Random.Range(0f, td.size.x);
            float z = Random.Range(0f, td.size.z);

            float worldX = tPos.x + x;
            float worldZ = tPos.z + z;

            // approx terrain height
            float approxY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + tPos.y;

            // slope check
            Vector3 normal = td.GetInterpolatedNormal(x / td.size.x, z / td.size.z);
            float slope = Vector3.Angle(normal, Vector3.up);
            if (slope > maxSlope) continue;

            // raycast to ground collider (exact)
            Vector3 rayOrigin = new Vector3(worldX, approxY + raycastHeight, worldZ);
            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer, QueryTriggerInteraction.Ignore))
                continue;

            Vector3 pos = hit.point + Vector3.up * groundOffset;

            // ✅ avoid water: if this position intersects any water collider => skip
            if (IsInsideWater(pos))
                continue;

            // min distance
            if (IsTooClose(pos, minDistance))
                continue;

            // spawn
            GameObject prefab = stonePrefabs[Random.Range(0, stonePrefabs.Count)];
            if (!prefab) continue;

            GameObject item = Instantiate(prefab, pos, Quaternion.identity, parent);

            if (randomRotation)
                item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float s = Random.Range(scaleRange.x, scaleRange.y);
            item.transform.localScale = Vector3.one * (0.1f * s);

            // tag + collectible type
            TrySetTag(item, "Stone");
            SetCollectibleTypeEverywhere(item, ItemType.Stone, "Stone");

            // physics behaviour for XR
            if (freezeRigidbodyOnSpawn)
                MakeKinematicEverywhere(item); // NOT recommended for XR pickup/drop

            usedPositions.Add(pos);
            placed++;
        }

        Debug.Log($"StoneScatterer: placed {placed}/{count} stones in {attempts} attempts.");
    }

    private bool IsInsideWater(Vector3 pos)
    {
        // overlap box around the chosen point (good for thin water planes/boxes)
        return Physics.CheckBox(
            pos,
            waterCheckHalfExtents,
            Quaternion.identity,
            waterMask,
            waterTriggerInteraction
        );
    }

    private bool IsTooClose(Vector3 pos, float minDist)
    {
        float minSqr = minDist * minDist;
        for (int i = 0; i < usedPositions.Count; i++)
        {
            if ((usedPositions[i] - pos).sqrMagnitude < minSqr)
                return true;
        }
        return false;
    }

    private void MakeKinematicEverywhere(GameObject root)
    {
        var rbs = root.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rbs)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void SetCollectibleTypeEverywhere(GameObject root, ItemType type, string tag)
    {
        var all = root.GetComponentsInChildren<CollectibleItem>(true);
        if (all != null && all.Length > 0)
        {
            foreach (var c in all)
            {
                c.itemType = type;
                TrySetTag(c.gameObject, tag);
            }
            return;
        }

        var collectible = root.GetComponent<CollectibleItem>();
        if (!collectible) collectible = root.AddComponent<CollectibleItem>();
        collectible.itemType = type;
    }

    private void TrySetTag(GameObject go, string tag)
    {
        try { go.tag = tag; }
        catch { Debug.LogWarning($"Tag '{tag}' nu exista. Creeaza-l in Tags & Layers."); }
    }
}
