using System.Collections.Generic;
using UnityEngine;

public class StickScatterer : MonoBehaviour
{
    public Terrain terrain;

    [Header("Stick prefabs")]
    public List<GameObject> stickPrefabs = new();   // prefabs de stick-uri

    [Header("Spawn")]
    public int count = 250;
    public int seed = 3333;
    [Range(0f, 45f)] public float maxSlope = 20f;
    public float minDistance = 1.2f;

    [Header("Random")]
    public Vector2 scaleRange = new Vector2(0.08f, 0.12f);
    public bool randomRotation = true;

    [Header("Parent")]
    public Transform parent;
    public bool clearPrevious = true;

    private readonly List<Vector3> usedPositions = new();

    [ContextMenu("Scatter Sticks")]
    public void ScatterSticks()
    {
        if (!terrain)
        {
            Debug.LogError("StickScatterer: Terrain not assigned.");
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

        while (placed < count && attempts < count * 100)
        {
            attempts++;

            float x = Random.Range(0f, td.size.x);
            float z = Random.Range(0f, td.size.z);

            float worldX = tPos.x + x;
            float worldZ = tPos.z + z;
            float y = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + tPos.y;

            Vector3 normal = td.GetInterpolatedNormal(x / td.size.x, z / td.size.z);
            float slope = Vector3.Angle(normal, Vector3.up);
            if (slope > maxSlope) continue;

            Vector3 pos = new Vector3(worldX, y, worldZ);

            bool tooClose = false;
            for (int i = 0; i < usedPositions.Count; i++)
            {
                if ((usedPositions[i] - pos).sqrMagnitude < minDistance * minDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            GameObject prefab = stickPrefabs[Random.Range(0, stickPrefabs.Count)];
            GameObject item = Instantiate(prefab, pos, Quaternion.identity, parent);

            if (randomRotation)
                item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float s = Random.Range(scaleRange.x, scaleRange.y);
            item.transform.localScale = Vector3.one * s;

            // Tag
            TrySetTag(item, "Stick");

            // CollectibleItem: Stick (root + copii)
            bool set = SetCollectibleTypeEverywhere(item, ItemType.Stick, "Stick");

            Debug.Log($"[StickScatterer] Spawned {item.name} | setStick={set}");

            usedPositions.Add(pos);
            placed++;
        }

        Debug.Log($"StickScatterer: spawned {placed}/{count} sticks.");
    }

    private bool SetCollectibleTypeEverywhere(GameObject root, ItemType type, string tag)
    {
        bool changed = false;

        var all = root.GetComponentsInChildren<CollectibleItem>(true);
        if (all != null && all.Length > 0)
        {
            foreach (var c in all)
            {
                c.itemType = type;
                TrySetTag(c.gameObject, tag);
                changed = true;
            }
            return changed;
        }

        var collectible = root.GetComponent<CollectibleItem>();
        if (!collectible) collectible = root.AddComponent<CollectibleItem>();
        collectible.itemType = type;
        changed = true;

        return changed;
    }

    private void TrySetTag(GameObject go, string tag)
    {
        try { go.tag = tag; }
        catch { Debug.LogWarning($"Tag '{tag}' nu exista. Creeaza-l in Tags & Layers."); }
    }
}
