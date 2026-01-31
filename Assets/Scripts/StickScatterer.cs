using System.Collections.Generic;
using UnityEngine;

public class StickScatterer : MonoBehaviour
{
    [Header("Terrain")]
    public Terrain terrain;

    [Header("Stick prefabs")]
    public List<GameObject> stickPrefabs = new();

    [Header("Spawn")]
    public int count = 250;
    public int seed = 3333;
    [Range(0f, 45f)] public float maxSlope = 20f;
    public float minDistance = 1.2f;
    public int maxAttemptsMultiplier = 120;

    [Header("Random")]
    public Vector2 scaleRange = new Vector2(0.08f, 0.12f);
    public bool randomRotation = true;

    [Header("Parent")]
    public Transform parent;
    public bool clearPrevious = true;

    [Header("Ground snap")]
    public LayerMask groundLayer;       // set la "Ground"
    public float raycastHeight = 5f;
    public float groundOffset = 0.02f;

    [Header("Avoid water")]
    public LayerMask waterMask;         // set la "Water"
    public Vector3 waterCheckHalfExtents = new Vector3(0.35f, 1.5f, 0.35f);
    public QueryTriggerInteraction waterTriggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Physics (XR friendly)")]
    [Tooltip("TRUE => le face kinematic si pot ramane in aer la drop in XR. Recomand FALSE pentru pickup items.")]
    public bool freezeRigidbodyOnSpawn = false;

    private readonly List<Vector3> usedPositions = new();

    [ContextMenu("Scatter Sticks")]
    public void ScatterSticks()
    {
        if (!terrain) terrain = FindFirstObjectByType<Terrain>();
        if (!terrain)
        {
            Debug.LogError("StickScatterer: Terrain not assigned/found.");
            return;
        }

        if (stickPrefabs == null || stickPrefabs.Count == 0)
        {
            Debug.LogError("StickScatterer: No stick prefabs assigned.");
            return;
        }

        Random.InitState(seed);
        usedPositions.Clear();

        if (!parent)
        {
            var go = GameObject.Find("Sticks");
            if (!go) go = new GameObject("Sticks");
            parent = go.transform;
        }

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

            float x = Random.Range(0f, td.size.x);
            float z = Random.Range(0f, td.size.z);

            float worldX = tPos.x + x;
            float worldZ = tPos.z + z;

            float approxY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + tPos.y;

            // slope check
            Vector3 normal = td.GetInterpolatedNormal(x / td.size.x, z / td.size.z);
            float slope = Vector3.Angle(normal, Vector3.up);
            if (slope > maxSlope) continue;

            // snap on ground collider
            Vector3 rayOrigin = new Vector3(worldX, approxY + raycastHeight, worldZ);
            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    raycastHeight * 2f, groundLayer, QueryTriggerInteraction.Ignore))
                continue;

            Vector3 pos = hit.point + Vector3.up * groundOffset;

            // ✅ avoid water
            if (IsInsideWater(pos))
                continue;

            // min distance
            if (IsTooClose(pos, minDistance))
                continue;

            // spawn
            GameObject prefab = stickPrefabs[Random.Range(0, stickPrefabs.Count)];
            if (!prefab) continue;

            GameObject item = Instantiate(prefab, pos, Quaternion.identity, parent);

            // random rotation
            if (randomRotation)
                item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // random scale
            float s = Random.Range(scaleRange.x, scaleRange.y);
            item.transform.localScale = Vector3.one * s;

            // tag + collectible
            TrySetTag(item, "Stick");
            SetCollectibleTypeEverywhere(item, ItemType.Stick, "Stick");

            // XR: optional freeze (NOT recommended)
            if (freezeRigidbodyOnSpawn)
                MakeKinematicEverywhere(item);

            usedPositions.Add(pos);
            placed++;
        }

        Debug.Log($"StickScatterer: placed {placed}/{count} sticks in {attempts} attempts.");
    }

    private bool IsInsideWater(Vector3 pos)
    {
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // ca sa vezi zona de check water in scena (optional)
        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, waterCheckHalfExtents * 2f);
    }
#endif
}
