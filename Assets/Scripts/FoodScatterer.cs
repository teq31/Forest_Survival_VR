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

    private readonly List<Vector3> usedPositions = new();

    [ContextMenu("Scatter Food")]
    public void ScatterFood()
    {
        if (!terrain)
        {
            Debug.LogError("FoodScatterer: Terrain not assigned.");
            return;
        }
        if (foodPrefabs == null || foodPrefabs.Count == 0)
        {
            Debug.LogError("FoodScatterer: No food prefabs assigned.");
            return;
        }

        Random.InitState(seed);
        usedPositions.Clear();

        if (!parent)
        {
            var go = GameObject.Find("Food");
            if (!go) go = new GameObject("Food");
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

        while (placed < count && attempts < count * 80)
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

            GameObject prefab = foodPrefabs[Random.Range(0, foodPrefabs.Count)];
            GameObject item = Instantiate(prefab, pos, Quaternion.identity, parent);

            if (randomRotation)
                item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float s = Random.Range(scaleRange.x, scaleRange.y);
            item.transform.localScale = Vector3.one * s;

            // ✅ seteaza tag pe root
            TrySetTag(item, "Food");

            // ✅ IMPORTANT: seteaza CollectibleItem pe root SAU pe copii (unde exista)
            bool setSomething = SetCollectibleFoodEverywhere(item);

            Debug.Log($"[FoodScatterer] Spawned {item.name} | setFood={setSomething}");

            usedPositions.Add(pos);
            placed++;
        }

        Debug.Log($"FoodScatterer: spawned {placed}/{count} items in {attempts} attempts.");
    }

    private bool SetCollectibleFoodEverywhere(GameObject root)
    {
        bool changed = false;

        // 1) daca exista deja pe copii, seteaza pe toate
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

        // 2) altfel, adauga pe root
        var collectible = root.GetComponent<CollectibleItem>();
        if (!collectible) collectible = root.AddComponent<CollectibleItem>();
        collectible.itemType = ItemType.Food;
        changed = true;

        return changed;
    }

    private void TrySetTag(GameObject go, string tag)
    {
        try { go.tag = tag; }
        catch { Debug.LogWarning($"Tag '{tag}' nu exista. Creeaza-l in Tags & Layers."); }
    }
}
