using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private UIManager uIManager;

    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Wave Settings")]
    [SerializeField] private int waveEnemyCount = 20;
    [SerializeField] private int waveEnemyAdd = 10;
    [SerializeField] private float waveColdown = 3;
    private float waveColdownTimer = 0f;
    private int waveCounter = 0;
    private bool waveFinished = false;
    private bool waveCounterAdded = false;

    private int enemySpawnedInOneWave = 0;
    private int enemyKilledInOneWave = 0;

    [Header("Enemy Spawning")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private int maxEnemies = 8;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float spawnInterval = 5f;

    [Header("Game Stats")]
    private int score = 0;
    private int enemiesKilled = 0;

    [Header("Player Health Integration")]
    private PlayerHealth playerHealth;

    // Made readonly and simplified initializer
    private readonly List<GameObject> activeEnemies = new();
    private readonly List<GameObject> activeHealthPickups = new();

    private float spawnTimer = 0f;
    private bool gameRunning = true;

    [Header("Speed Boost Spawning")]
    [SerializeField] private GameObject speedBoostPrefab;
    [SerializeField] private int boostCount = 3;

    [Header("Health Pickup Spawning")]
    [SerializeField] private GameObject healthPickupPrefab;
    [SerializeField] private int healthPickupCount = 3;

    // Health pickup spawn positions - within tilemap bounds
    private static readonly Vector3[] healthSpawnPositions = new Vector3[]
    {
        new Vector3(  8f,   6f, 0f),  // Top right area
        new Vector3( -8f,   6f, 0f),  // Top left area
        new Vector3(  8f,  -6f, 0f),  // Bottom right area
        new Vector3( -8f,  -6f, 0f),  // Bottom left area
        new Vector3(  0f,  10f, 0f),  // Top center
        new Vector3(  0f, -10f, 0f),  // Bottom center
        new Vector3( 12f,   0f, 0f),  // Right center
        new Vector3(-12f,   0f, 0f),  // Left center
        new Vector3(  6f,   8f, 0f),  // Upper right diagonal
        new Vector3( -6f,   8f, 0f),  // Upper left diagonal
        new Vector3(  6f,  -8f, 0f),  // Lower right diagonal
        new Vector3( -6f,  -8f, 0f),  // Lower left diagonal
        new Vector3( 10f,   3f, 0f),  // Right side varied
        new Vector3(-10f,  -3f, 0f),  // Left side varied
        new Vector3(  3f,  12f, 0f),  // Top side varied
        new Vector3( -3f, -12f, 0f)   // Bottom side varied
    };

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        uIManager = UIManager.instance;

        // Debug check
        Debug.Log("GameManager initialized. Health Pickup Prefab: " + (healthPickupPrefab != null ? "Found" : "Missing"));
    }

    void Update()
    {
        if (!gameRunning) return;

        activeEnemies.RemoveAll(e => e == null);

        if (waveColdownTimer <= waveColdown)
        {
            waveColdownTimer += Time.deltaTime;
            return;
        }
        else
        {
            waveFinished = false;
        }

        if (waveFinished == false && waveCounterAdded == false)
        {
            waveCounter++;
            waveEnemyCount += waveEnemyAdd;
            spawnInterval -= 0.5f;
            if (spawnInterval <= 2)
                spawnInterval = 2;
            enemySpawnedInOneWave = 0;
            enemyKilledInOneWave = 0;
            waveCounterAdded = true;

            SpawnSpeedBoosts(); // speedboost spawn
            SpawnHealthPickups(); // health pickup spawn
        }

        spawnTimer += Time.deltaTime;

        WaveEnemySpawn();

        if (playerHealth != null && playerHealth.GetIsPlayerDead() || playerHealth == null)
            Invoke(nameof(HandleEndGame), 2f);

        HandleGamePause();
    }

    private void HandleGamePause()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            uIManager.ActivePauseUI();
        }
    }

    private void HandleEndGame()
    {
        gameRunning = false;
        Time.timeScale = 0f;
        uIManager.ShowGameOver();
    }

    private void WaveEnemySpawn()
    {
        if (enemySpawnedInOneWave < waveEnemyCount)
        {
            if (spawnTimer >= spawnInterval && activeEnemies.Count < maxEnemies)
            {
                SpawnEnemy();
                spawnTimer = 0f;
            }
        }
        else
        {
            if (activeEnemies.Count <= 0)
            {
                waveColdownTimer = 0f;
                waveFinished = true;
                waveCounterAdded = false;
            }
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || player == null) return;

        Vector2 spawnPosition = spawnPoints[Random.Range(0, spawnPoints.Count)].position;

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        if (!newEnemy.CompareTag("Enemy"))
            newEnemy.tag = "Enemy";

        enemySpawnedInOneWave++;
        activeEnemies.Add(newEnemy);
    }

    public void RegisterEnemyDeath(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            enemyKilledInOneWave++;
            enemiesKilled++;
            score += 100;
        }
    }

    public void OnEnemyWrapped()
    {
        score += 50;
    }

    public int GetEnemiesKilled() => enemiesKilled;

    public int GetWaveCount() => waveCounter;

    public int GetScore() => score;

    public int GetEnemiesRemaining() => waveEnemyCount - enemyKilledInOneWave;

    public List<GameObject> GetActiveEnemies() => activeEnemies;

    private void SpawnSpeedBoosts()
    {
        if (speedBoostPrefab == null || player == null) return;

        for (int i = 0; i < boostCount; i++)
        {
            // Spawn further away from player in random directions
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float distance = Random.Range(spawnRadius * 2f, spawnRadius * 4f);
            Vector2 pos = (Vector2)player.position + randomDirection * distance;

            Instantiate(speedBoostPrefab, pos, Quaternion.identity);
        }
    }

    private void SpawnHealthPickups()
    {
        if (healthPickupPrefab == null)
        {
            Debug.LogWarning("GameManager: Health pickup prefab not assigned.");
            return;
        }

        // Clean up existing health pickups before spawning new ones
        foreach (GameObject pickup in activeHealthPickups)
        {
            if (pickup != null)
                Destroy(pickup);
        }
        activeHealthPickups.Clear();

        Debug.Log($"Spawning {healthPickupCount} health pickups for wave {waveCounter}");

        // Build a list of indices [0..healthSpawnPositions.Length-1]
        var indices = new List<int>(healthSpawnPositions.Length);
        for (int i = 0; i < healthSpawnPositions.Length; i++)
            indices.Add(i);

        // Fisher–Yates shuffle
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Spawn up to healthPickupCount, or as many positions as we have
        int toSpawn = Mathf.Min(healthPickupCount, indices.Count);
        for (int k = 0; k < toSpawn; k++)
        {
            Vector3 pos = healthSpawnPositions[indices[k]];
            GameObject healthPickup = Instantiate(healthPickupPrefab, pos, Quaternion.identity);
            activeHealthPickups.Add(healthPickup);
            Debug.Log($"Health pickup spawned at position: {pos}");
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            CleanupEffects();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            CleanupEffects();
        }
    }

    private void OnDestroy()
    {
        CleanupEffects();
    }

    private void CleanupEffects()
    {
        // Clean up any remaining health pickup effects
        GameObject[] healEffects = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name.Contains("HealPickUpEffect"))
            .ToArray();

        foreach (GameObject effect in healEffects)
        {
            if (effect != null)
                DestroyImmediate(effect);
        }
    }
}