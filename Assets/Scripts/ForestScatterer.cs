using System.Collections.Generic;
using UnityEngine;

public class ForestScatterer : MonoBehaviour
{
    [Header("Terrain")]
    public Terrain terrain;                 // trage Terrain-ul aici
    public bool useTerrainColliderRaycast = false;

    [Header("Prefabs (6-7 tree types)")]
    public List<GameObject> treePrefabs = new(); // trage aici NF_Prop_Oak_02, NF_Prop_Pine_01 etc.

    [Header("Spawn")]
    public int count = 400;
    public int seed = 12345;
    public Vector2 heightRange = new Vector2(0f, 1000f); // in world Y
    [Range(0f, 45f)] public float maxSlopeDegrees = 25f;

    [Header("Spacing")]
    public float minDistance = 3.5f;        // distanta minima intre copaci
    public int maxAttempts = 200000;        // incearca pana gaseste pozitii valide

    [Header("Random Scale")]
    public Vector2 uniformScaleRange = new Vector2(0.85f, 1.25f);

    [Header("Parent")]
    public Transform parent;                // optional: un GameObject "Forest"
    public bool clearPrevious = true;

    [Header("Optional: Avoid edges")]
    [Range(0f, 0.49f)] public float borderPaddingNormalized = 0.02f; // 2% margine

    private readonly List<Vector3> placedPositions = new();

    [ContextMenu("Scatter Forest")]
    public void Scatter()
    {
        if (!terrain) terrain = FindFirstObjectByType<Terrain>();
        if (!terrain)
        {
            Debug.LogError("No Terrain assigned/found.");
            return;
        }
        if (treePrefabs == null || treePrefabs.Count == 0)
        {
            Debug.LogError("Add at least one tree prefab to treePrefabs.");
            return;
        }

        var rng = new System.Random(seed);
        placedPositions.Clear();

        // Parent container
        if (!parent)
        {
            var go = GameObject.Find("Forest");
            if (!go) go = new GameObject("Forest");
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

        var td = terrain.terrainData;
        var tPos = terrain.transform.position;
        float width = td.size.x;
        float length = td.size.z;

        // border padding in world units
        float padX = width * borderPaddingNormalized;
        float padZ = length * borderPaddingNormalized;

        int placed = 0;
        int attempts = 0;

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;

            // random point in terrain bounds (world)
            float x = (float)(padX + rng.NextDouble() * (width - 2f * padX));
            float z = (float)(padZ + rng.NextDouble() * (length - 2f * padZ));
            float worldX = tPos.x + x;
            float worldZ = tPos.z + z;

            // height + normal from terrain
            float y = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + tPos.y;
            if (y < heightRange.x || y > heightRange.y) continue;

            Vector3 normal = td.GetInterpolatedNormal(x / width, z / length);
            float slope = Vector3.Angle(normal, Vector3.up);
            if (slope > maxSlopeDegrees) continue;

            var pos = new Vector3(worldX, y, worldZ);

            // optional: raycast to collider if you prefer (for terrains + other colliders)
            if (useTerrainColliderRaycast)
            {
                var ray = new Ray(new Vector3(worldX, y + 200f, worldZ), Vector3.down);
                if (Physics.Raycast(ray, out var hit, 1000f))
                {
                    pos = hit.point;
                    normal = hit.normal;
                    slope = Vector3.Angle(normal, Vector3.up);
                    if (slope > maxSlopeDegrees) continue;
                }
                else continue;
            }

            // spacing check
            bool ok = true;
            for (int i = 0; i < placedPositions.Count; i++)
            {
                if ((placedPositions[i] - pos).sqrMagnitude < minDistance * minDistance)
                {
                    ok = false;
                    break;
                }
            }
            if (!ok) continue;

            // choose prefab
            var prefab = treePrefabs[rng.Next(0, treePrefabs.Count)];
            if (!prefab) continue;

#if UNITY_EDITOR
            GameObject instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
#else
            GameObject instance = Instantiate(prefab);
#endif
            instance.transform.SetParent(parent, true);
            instance.transform.position = pos;

            // rotate around Y randomly
            float rotY = (float)(rng.NextDouble() * 360.0);
            instance.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            // scale random
            float s = Mathf.Lerp(uniformScaleRange.x, uniformScaleRange.y, (float)rng.NextDouble());
            instance.transform.localScale = Vector3.one * s;

            placedPositions.Add(pos);
            placed++;
        }

        Debug.Log($"ForestScatterer: placed {placed}/{count} trees in {attempts} attempts. Parent: {parent.name}");
    }
}
