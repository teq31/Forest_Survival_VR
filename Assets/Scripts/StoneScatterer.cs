using System.Collections.Generic;
using UnityEngine;

public class StoneScatterer : MonoBehaviour
{
    public Terrain terrain;

    [Header("Stone prefabs")]
    public List<GameObject> stonePrefabs = new(); // 6–7 tipuri de pietre

    [Header("Spawn")]
    public int count = 150;
    public int seed = 2222;
    [Range(0f, 45f)] public float maxSlope = 30f;
    public float minDistance = 2.0f;

    [Header("Random")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.4f);
    public bool randomRotation = true;

    [Header("Parent")]
    public Transform parent;   // optional: un GO "Stones"
    public bool clearPrevious = true;

    private readonly List<Vector3> usedPositions = new();

    [ContextMenu("Scatter Stones")]
    public void ScatterStones()
    {
        if (!terrain)
        {
            Debug.LogError("StoneScatterer: Terrain not assigned.");
            return;
        }
        if (stonePrefabs == null || stonePrefabs.Count == 0)
        {
            Debug.LogError("StoneScatterer: No stone prefabs assigned.");
            return;
        }

        Random.InitState(seed);
        usedPositions.Clear();

        if (!parent)
        {
            var go = GameObject.Find("Stones");
            if (!go) go = new GameObject("Stones");
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

            GameObject prefab = stonePrefabs[Random.Range(0, stonePrefabs.Count)];
            GameObject item = Instantiate(prefab, pos, Quaternion.identity, parent);

            if (randomRotation)
                item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float s = Random.Range(scaleRange.x, scaleRange.y);
            item.transform.localScale = Vector3.one * 0.1f;

            // Tag pe root (daca exista tag-ul)
            TrySetTag(item, "Stone");

            // Seteaza CollectibleItem.itemType = Stone (root + copii)
            bool set = SetCollectibleTypeEverywhere(item, ItemType.Stone, "Stone");

            Debug.Log($"[StoneScatterer] Spawned {item.name} | setStone={set}");

            usedPositions.Add(pos);
            placed++;
        }

        Debug.Log($"StoneScatterer: spawned {placed}/{count} stones in {attempts} attempts.");
    }

    private bool SetCollectibleTypeEverywhere(GameObject root, ItemType type, string tag)
    {
        bool changed = false;

        // daca exista deja pe copii, seteaza pe toate
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

        // altfel adauga pe root
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
