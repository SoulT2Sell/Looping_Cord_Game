using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpeedBoost : MonoBehaviour
{

    [SerializeField] private GameObject pickUpEffect;

    [Header("Speed Boost Settings")]
    [SerializeField] private float boostAmount = 2f;  // how much faster
    [SerializeField] private float boostDuration = 3f;  // seconds of boost

    [Header("Spawning Settings")]
    [SerializeField] private GameObject speedBoostPrefab; // reference to this prefab for spawning new ones
    [SerializeField] private int maxSpeedBoosts = 3;
    [SerializeField] private float respawnDelay = 5f;

    [Header("Spawn Area Around Player")]
    [SerializeField] private float spawnRangeX = 15f; // left/right from player
    [SerializeField] private float spawnRangeY = 10f; // up/down from player
    [SerializeField] private float minDistanceFromPlayer = 3f; // don't spawn too close to player

    [Header("Optional: Manual Spawn Limits")]
    [SerializeField] private bool useManualLimits = false;
    [SerializeField] private Vector2 minWorldBounds = new(-20, -15);
    [SerializeField] private Vector2 maxWorldBounds = new(20, 15);

    [Header("Collision Check")]
    [SerializeField] private LayerMask obstacleLayer = -1; // what layers to avoid spawning on
    [SerializeField] private float checkRadius = 0.5f;

    // Static variables shared by all SpeedBoost instances
    private static bool isBoostActive = false;
    private static int currentSpeedBoosts = 0;
    private static Transform playerTransform;

    // Instance variables
    private float boostTimer;
    private PlayerMovement playerMovement;
    private float originalSpeed;


    [SerializeField] AudioSource speedPickupSource;
    [SerializeField] AudioClip speedPickup;
    void Start()
    {
        // Find player on first spawn
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        // Count this speed boost
        currentSpeedBoosts++;

        // If this is the first speed boost, spawn the initial set
        if (currentSpeedBoosts == 1)
        {
            SpawnInitialBoosts();
        }
    }

    void Update()
    {
        // Only the pickup that *started* the boost runs the timer
        if (isBoostActive && playerMovement != null)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
                EndBoost();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"SpeedBoost triggered by: {other.name}");

        if (!other.CompareTag("Player"))
        {
            Debug.Log("Not a player, ignoring");
            return;
        }

        if (isBoostActive)
        {
            Debug.Log("Boost already active, ignoring");
            return; // if *any* boost is active, do nothing
        }

        // Grab their movement script
        if (!other.TryGetComponent<PlayerMovement>(out playerMovement))
        {
            Debug.Log("No PlayerMovement component found");
            return;
        }

        Debug.Log("Starting speed boost!");

        // Hide the pickup so you can't reuse it
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        Instantiate(pickUpEffect, transform.position, Quaternion.identity);
        speedPickupSource.Play();
        StartBoost();
    }

    void StartBoost()
    {
        isBoostActive = true;                        // lock out other pickups
        originalSpeed = playerMovement.speed;        // remember old speed
        playerMovement.speed = originalSpeed * boostAmount;
        boostTimer = boostDuration;                  // start countdown
    }

    void EndBoost()
    {
        playerMovement.speed = originalSpeed;        // restore speed
        isBoostActive = false;                       // allow pickups again

        // Spawn a new speed boost after delay before destroying this one
        if (speedBoostPrefab != null)
        {
            Invoke(nameof(SpawnNewBoost), respawnDelay);
        }

        currentSpeedBoosts--;                        // decrease count
        Destroy(gameObject);                         // clean up this pickup
    }

    void SpawnInitialBoosts()
    {
        // Spawn additional speed boosts to reach the max count
        for (int i = 1; i < maxSpeedBoosts; i++) // start from 1 since this object counts as the first
        {
            SpawnNewBoost();
        }
    }

    void SpawnNewBoost()
    {
        if (currentSpeedBoosts >= maxSpeedBoosts || speedBoostPrefab == null || playerTransform == null)
            return;

        Vector2 spawnPosition = GetRandomSpawnPosition();
        int attempts = 0;
        int maxAttempts = 20;

        // Try to find a valid position
        while (!IsValidSpawnPosition(spawnPosition) && attempts < maxAttempts)
        {
            spawnPosition = GetRandomSpawnPosition();
            attempts++;
        }

        if (attempts < maxAttempts)
        {
            Instantiate(speedBoostPrefab, spawnPosition, Quaternion.identity);
            Debug.Log($"SpeedBoost spawned at {spawnPosition}");
        }
        else
        {
            Debug.LogWarning("Couldn't find valid spawn position after " + maxAttempts + " attempts");
        }
    }

    Vector2 GetRandomSpawnPosition()
    {
        if (playerTransform == null) return Vector2.zero;

        Vector2 playerPos = playerTransform.position;

        // Generate random position around player
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        float randomY = Random.Range(-spawnRangeY, spawnRangeY);

        Vector2 potentialPosition = playerPos + new Vector2(randomX, randomY);

        // Clamp to manual world bounds if enabled
        if (useManualLimits)
        {
            potentialPosition.x = Mathf.Clamp(potentialPosition.x, minWorldBounds.x, maxWorldBounds.x);
            potentialPosition.y = Mathf.Clamp(potentialPosition.y, minWorldBounds.y, maxWorldBounds.y);
        }

        return potentialPosition;
    }

    bool IsValidSpawnPosition(Vector2 position)
    {
        if (playerTransform == null) return false;

        // Check if too close to player
        float distanceToPlayer = Vector2.Distance(position, playerTransform.position);
        if (distanceToPlayer < minDistanceFromPlayer)
            return false;

        // Clamp to manual world bounds if enabled
        if (useManualLimits)
        {
            if (position.x < minWorldBounds.x || position.x > maxWorldBounds.x ||
                position.y < minWorldBounds.y || position.y > maxWorldBounds.y)
                return false;
        }

        // Check for obstacles (trees, walls, etc.)
        Collider2D hit = Physics2D.OverlapCircle(position, checkRadius, obstacleLayer);
        if (hit != null)
        {
            // Allow spawning on tilemap ground, but not on trees/walls
            if (hit.CompareTag("Wall") || hit.CompareTag("Tree") || hit.CompareTag("Obstacle"))
                return false;
        }

        // Check if there's already a speed boost nearby
        Collider2D[] nearbyBoosts = Physics2D.OverlapCircleAll(position, 2f);
        foreach (var boost in nearbyBoosts)
        {
            // Check if this collider has the SpeedBoost script
            if (boost.GetComponent<SpeedBoost>() != null)
                return false;
        }

        return true;
    }

    void OnDestroy()
    {
        // Make sure we decrease the count when destroyed
        currentSpeedBoosts = Mathf.Max(0, currentSpeedBoosts - 1);
    }

    // Visual debugging in scene view
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;

        // Draw spawn area around player
        Gizmos.color = Color.green;
        Vector3 size = new(spawnRangeX * 2, spawnRangeY * 2, 0);
        Gizmos.DrawWireCube(playerPos, size);

        // Draw minimum distance circle
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerPos, minDistanceFromPlayer);

        // Draw manual world bounds if enabled
        if (useManualLimits)
        {
            Gizmos.color = Color.blue;
            Vector3 boundsCenter = (Vector3)(minWorldBounds + maxWorldBounds) / 2f;
            Vector3 boundsSize = (Vector3)(maxWorldBounds - minWorldBounds);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }

        // Draw current spawn positions
        Gizmos.color = Color.yellow;
        SpeedBoost[] currentBoosts = FindObjectsByType<SpeedBoost>(FindObjectsSortMode.None);
        foreach (var boost in currentBoosts)
        {
            if (boost != null)
                Gizmos.DrawWireSphere(boost.transform.position, 0.5f);
        }
    }
}