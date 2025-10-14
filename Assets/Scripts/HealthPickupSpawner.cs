using UnityEngine;
using System.Collections.Generic;

public class HealthPickupSpawner : MonoBehaviour
{
    [Header("Health Pickup Settings")]
    [Tooltip("The Health Pickup prefab")]
    public GameObject healthPickupPrefab;

    [Tooltip("How many health pickups to spawn each wave")]
    public int pickupCount = 3;

    // Hard-coded world positions for pickups (tweak as needed):
    private static readonly Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(  0f,  0f, 0f),
        new Vector3(  4f,  2f, 0f),
        new Vector3( -4f, -2f, 0f),
        new Vector3(  3f, -3f, 0f),
        new Vector3( -3f,  3f, 0f),
        new Vector3(  6f,  0f, 0f),
        new Vector3( -6f,  1f, 0f),
        new Vector3(  1f, -6f, 0f)
    };

    /// <summary>
    /// Call this exactly as your GameManager already does:
    ///     healthSpawner.SpawnHealthPickups();
    /// </summary>
    public void SpawnHealthPickups()
    {
        if (healthPickupPrefab == null)
        {
            Debug.LogWarning("HealthPickupSpawner: no prefab assigned.");
            return;
        }

        // build a list of indices [0..spawnPositions.Length-1]
        var indices = new List<int>(spawnPositions.Length);
        for (int i = 0; i < spawnPositions.Length; i++)
            indices.Add(i);

        // Fisher–Yates shuffle
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // spawn up to pickupCount, or as many positions as we have
        int toSpawn = Mathf.Min(pickupCount, indices.Count);
        for (int k = 0; k < toSpawn; k++)
        {
            Vector3 pos = spawnPositions[indices[k]];
            Instantiate(healthPickupPrefab, pos, Quaternion.identity);
        }
    }
}
