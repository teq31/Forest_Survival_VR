using System;
using System.Collections.Generic;
using UnityEngine;

public class ForestScatterer : MonoBehaviour
{
    [Header("Terrain")]
    public Terrain terrain;

    [Header("Prefabs (tree types)")]
    public List<GameObject> treePrefabs = new();

    [Header("Spawn")]
    public int count = 400;
    public int seed = 12345;
    public Vector2 heightRange = new Vector2(0f, 1000f);
    [Range(0f, 60f)] public float maxSlopeDegrees = 25f;

    [Header("Spacing")]
    public float minDistance = 3.5f;
    public int maxAttempts = 200000;

    [Header("Random Scale")]
    public Vector2 uniformScaleRange = new Vector2(0.85f, 1.25f);

    [Header("Parent")]
    public Transform parent;
    public bool clearPrevious = true;

    [Header("Avoid edges")]
    [Range(0f, 0.49f)] public float borderPaddingNormalized = 0.02f;

    [Header("Ground Placement")]
    public bool snapToGround = true;
    public float baseYOffset = 0.02f;

    [Header("Optional: slight slope tilt")]
    public bool alignToSlope = false;
    [Range(0f, 20f)] public float maxTiltDegrees = 8f;

    [Header("Avoid Water (XZ inside collider => reject)")]
    public bool avoidWater = true;
    public LayerMask waterMask;     // bifezi layer Water
    public float waterMargin = 1.0f; // cât “îngroși” apa pe XZ ca să nu fie copaci pe margine

    [Header("Debug")]
    public bool debugLogs = false;
    public bool debugDrawRejected = false;

    private readonly List<Vector3> placedPositions = new();
    private readonly List<Collider> _waterColliders = new();

    private void Awake()
    {
        RebuildWaterCache();
    }

    private void OnValidate()
    {
        // ca să-ți refacă cache-ul când schimbi layer/mask în inspector
        if (Application.isPlaying) RebuildWaterCache();
    }

    [ContextMenu("Rebuild Water Cache")]
    public void RebuildWaterCache()
    {
        _waterColliders.Clear();

        // găsește toate colliderele din scenă și păstrează doar cele pe layer Water (conform waterMask)
        var all = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        int wm = waterMask.value;

        foreach (var c in all)
        {
            if (!c) continue;
            if (((1 << c.gameObject.layer) & wm) == 0) continue;
            _waterColliders.Add(c);
        }

        if (debugLogs)
            Debug.Log($"[ForestScatterer] Water cache: {_waterColliders.Count} colliders in mask.");
    }

    [ContextMenu("Scatter Forest")]
    public void Scatter()
    {
        if (!terrain) terrain = FindFirstObjectByType<Terrain>();
        if (!terrain)
        {
            Debug.LogError("ForestScatterer: No Terrain assigned/found.");
            return;
        }

        if (treePrefabs == null || treePrefabs.Count == 0)
        {
            Debug.LogError("ForestScatterer: Add at least one tree prefab to treePrefabs.");
            return;
        }

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

        // cache apă sigur (dacă ai adăugat/duplicat lacuri după play)
        if (avoidWater && _waterColliders.Count == 0)
            RebuildWaterCache();

        var rng = new System.Random(seed);
        placedPositions.Clear();

        var td = terrain.terrainData;
        var tPos = terrain.transform.position;
        float width = td.size.x;
        float length = td.size.z;

        float padX = width * borderPaddingNormalized;
        float padZ = length * borderPaddingNormalized;

        int placed = 0;
        int attempts = 0;
        float minDistSqr = minDistance * minDistance;

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;

            float localX = (float)(padX + rng.NextDouble() * (width - 2f * padX));
            float localZ = (float)(padZ + rng.NextDouble() * (length - 2f * padZ));
            float worldX = tPos.x + localX;
            float worldZ = tPos.z + localZ;

            if (!TryGetGroundFromTerrain(worldX, worldZ, out Vector3 groundPoint, out Vector3 groundNormal))
                continue;

            if (groundPoint.y < heightRange.x || groundPoint.y > heightRange.y)
                continue;

            float slope = Vector3.Angle(groundNormal, Vector3.up);
            if (slope > maxSlopeDegrees)
                continue;

            // ✅ Aici e fixul: verificare doar pe XZ în interiorul colliderului de apă
            if (avoidWater && IsPointInsideAnyWaterXZ(groundPoint, waterMargin))
            {
                if (debugDrawRejected)
                    Debug.DrawLine(groundPoint + Vector3.up * 0.1f, groundPoint + Vector3.up * 2f, Color.red, 1.0f);
                continue;
            }

            bool ok = true;
            for (int i = 0; i < placedPositions.Count; i++)
            {
                if ((placedPositions[i] - groundPoint).sqrMagnitude < minDistSqr)
                {
                    ok = false;
                    break;
                }
            }
            if (!ok) continue;

            var prefab = treePrefabs[rng.Next(0, treePrefabs.Count)];
            if (!prefab) continue;

#if UNITY_EDITOR
            GameObject instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
#else
            GameObject instance = Instantiate(prefab);
#endif
            instance.transform.SetParent(parent, true);

            float rotY = (float)(rng.NextDouble() * 360.0f);
            float s = Mathf.Lerp(uniformScaleRange.x, uniformScaleRange.y, (float)rng.NextDouble());
            instance.transform.localScale = Vector3.one * s;

            instance.transform.position = groundPoint;

            if (alignToSlope)
            {
                Quaternion tilt = Quaternion.FromToRotation(Vector3.up, groundNormal);
                tilt = ClampTilt(tilt, maxTiltDegrees);
                instance.transform.rotation = tilt * Quaternion.Euler(0f, rotY, 0f);
            }
            else
            {
                instance.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            }

            if (snapToGround)
                SnapInstanceToTerrain(instance);

            placedPositions.Add(instance.transform.position);
            placed++;

            if (debugLogs && placed % 50 == 0)
                Debug.Log($"ForestScatterer: placed {placed}/{count} (attempts={attempts})");
        }

        Debug.Log($"ForestScatterer: placed {placed}/{count} trees in {attempts} attempts. Parent: {parent.name}");
    }

    // ---------------- WATER (XZ ONLY) ----------------
    bool IsPointInsideAnyWaterXZ(Vector3 p, float margin)
    {
        if (_waterColliders.Count == 0) return false;

        // punctul testat în world, dar îl proiectăm pe XZ pentru fiecare collider
        for (int i = 0; i < _waterColliders.Count; i++)
        {
            var c = _waterColliders[i];
            if (!c) continue;

            if (c is BoxCollider bc)
            {
                if (IsInsideBoxColliderXZ(p, bc, margin))
                    return true;
            }
            else
            {
                // fallback: bounds XZ (AABB) — bun pentru MeshCollider/Plane etc.
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
        // Convertim punctul în local space-ul BoxCollider-ului
        Vector3 local = bc.transform.InverseTransformPoint(worldPoint);

        // BoxCollider.center & size sunt în local space (perfect)
        Vector3 c = bc.center;
        Vector3 half = bc.size * 0.5f;

        // verificăm doar XZ (ignori Y complet)
        bool insideX = Mathf.Abs(local.x - c.x) <= (half.x + margin);
        bool insideZ = Mathf.Abs(local.z - c.z) <= (half.z + margin);

        return insideX && insideZ;
    }

    // ---------------- TERRAIN SAMPLING ----------------
    bool TryGetGroundFromTerrain(float worldX, float worldZ, out Vector3 groundPoint, out Vector3 groundNormal)
    {
        groundPoint = Vector3.zero;
        groundNormal = Vector3.up;

        var td = terrain.terrainData;
        var tPos = terrain.transform.position;

        float nx = (worldX - tPos.x) / td.size.x;
        float nz = (worldZ - tPos.z) / td.size.z;

        if (nx < 0f || nx > 1f || nz < 0f || nz > 1f)
            return false;

        float y = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + tPos.y;
        groundPoint = new Vector3(worldX, y, worldZ);
        groundNormal = td.GetInterpolatedNormal(nx, nz);
        return true;
    }

    // ---------------- SNAP BASE ----------------
    void SnapInstanceToTerrain(GameObject obj)
    {
        Physics.SyncTransforms();

        Vector3 p = obj.transform.position;
        float groundY = terrain.SampleHeight(new Vector3(p.x, 0f, p.z)) + terrain.transform.position.y;
        float desiredBottomY = groundY + baseYOffset;

        float bottomY = GetObjectBottomWorldY(obj);
        obj.transform.position += new Vector3(0f, desiredBottomY - bottomY, 0f);

        Physics.SyncTransforms();

        float bottomY2 = GetObjectBottomWorldY(obj);
        obj.transform.position += new Vector3(0f, desiredBottomY - bottomY2, 0f);
    }

    float GetObjectBottomWorldY(GameObject obj)
    {
        Collider col = obj.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.min.y;

        Renderer rend = obj.GetComponentInChildren<Renderer>();
        if (rend != null) return rend.bounds.min.y;

        return obj.transform.position.y;
    }

    Quaternion ClampTilt(Quaternion tilt, float maxDegrees)
    {
        tilt.ToAngleAxis(out float angle, out Vector3 axis);
        angle = Mathf.DeltaAngle(0f, angle);
        angle = Mathf.Clamp(angle, -maxDegrees, maxDegrees);
        return Quaternion.AngleAxis(angle, axis);
    }
}
