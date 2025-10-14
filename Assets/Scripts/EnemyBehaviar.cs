using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    private Animator animator;

    //[Header("EnemyAttack")]
    //[SerializeField] private GameObject enemyAttack;
    //[SerializeField] private float activeAttackDistance;
    //[SerializeField] private float attackColdown;

    [Header("Damage")]
    public int damage;

    [Header("Enemy Settings")]
    public float moveSpeed = 2f;
    public float chaseRange = 8f;
    public float wrapTimeRequired = 2f;
    public int minRopeSegmentsToWrap = 4;

    [Header("Enhanced Chase Behavior")]
    public float chaseSpeed = 3f;
    public float obstacleAvoidanceRadius = 1f;
    public float separationRadius = 1.5f;
    public float predictionTime = 0.5f;
    public float pathSmoothness = 2f;

    [Header("Natural Movement")]
    public float wanderRadius = 3f;
    public float wanderDistance = 4f;
    public float wanderJitter = 1f;
    public float maxForce = 5f;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color chasingColor = Color.yellow;
    public Color wrappingColor = Color.red;
    public Color wrappedColor = Color.black;

    private readonly List<GameObject> contactRopeSegments = new();
    private readonly List<GameObject> attachedSegments = new();
    private bool isWrapped = false;
    private bool isBeingWrapped = false;
    private float wrapTimer = 0f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform player;
    private GameManager gameManager;
    [SerializeField] private GameObject dieEffect;

    // Enhanced AI States
    private enum EnemyState { Wandering, Chasing, BeingWrapped, Wrapped, Fleeing }
    private EnemyState currentState = EnemyState.Wandering;

    // Natural movement variables
    private Vector2 wanderTarget;
    private Vector2 velocity;
    private Vector2 desiredVelocity;
    private Vector2 lastPlayerPosition;
    private float playerLostTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>(); 

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            lastPlayerPosition = player.position;
        }

        gameManager = FindFirstObjectByType<GameManager>();

        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;

        // Initialize wandering
        wanderTarget = Random.insideUnitCircle * wanderRadius;
        velocity = rb.linearVelocity;
    }

    void Update()
    {
        if (isWrapped) return;

        //if(Vector2.Distance(transform.position, player.position) <= activeAttackDistance)
        //{
        //    StartCoroutine(HandleAttack());
        //}

        UpdateAIState();
        HandleMovement();
        HandleWrapping();
        HandleAnimation();
    }

    //private IEnumerator HandleAttack()
    //{
    //    animator.SetTrigger("isAttaking");
    //    yield return new WaitForSecondsRealtime(attackColdown);
    //}

    private void HandleAnimation()
    {
        if(rb.linearVelocity.magnitude > 0)
        {
            animator.SetBool("isWalking", true);
            animator.SetFloat("xVelocity", rb.linearVelocity.x);
            animator.SetFloat("yVelocity", rb.linearVelocity.y);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }

    void UpdateAIState()
    {
        if (isBeingWrapped)
        {
            currentState = EnemyState.BeingWrapped;
            return;
        }

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // Check if player is moving fast (potentially trying to wrap)
            float playerSpeed = player.GetComponent<Rigidbody2D>()?.linearVelocity.magnitude ?? 0f;
            bool playerMovingFast = playerSpeed > 4f;

            if (distanceToPlayer <= chaseRange)
            {
                // If player is moving very fast and close, consider fleeing
                if (playerMovingFast && distanceToPlayer < 3f && contactRopeSegments.Count > 0)
                {
                    currentState = EnemyState.Fleeing;
                }
                else
                {
                    currentState = EnemyState.Chasing;
                }
                lastPlayerPosition = player.position;
                playerLostTimer = 0f;
            }
            else
            {
                // Lost sight of player, search for a bit before going back to wandering
                playerLostTimer += Time.deltaTime;
                if (playerLostTimer < 3f)
                {
                    currentState = EnemyState.Chasing; // Keep searching
                }
                else
                {
                    currentState = EnemyState.Wandering;
                }
            }
        }
        else
        {
            currentState = EnemyState.Wandering;
        }
    }

    void HandleMovement()
    {
        Vector2 steering = Vector2.zero;

        switch (currentState)
        {
            case EnemyState.Wandering:
                steering = Wander() + Separate() + AvoidObstacles();
                if (spriteRenderer != null)
                    spriteRenderer.color = normalColor;
                break;

            case EnemyState.Chasing:
                steering = Seek(GetPredictedPlayerPosition()) + Separate() + AvoidObstacles();
                if (spriteRenderer != null && !isBeingWrapped)
                    spriteRenderer.color = chasingColor;
                break;

            case EnemyState.Fleeing:
                steering = Flee(player.position) + Separate() + AvoidObstacles();
                if (spriteRenderer != null)
                    spriteRenderer.color = Color.magenta;
                break;

            case EnemyState.BeingWrapped:
                steering = HandleBeingWrappedSteering();
                break;
        }

        // Apply steering
        steering = Vector2.ClampMagnitude(steering, maxForce);
        velocity += steering * Time.deltaTime;

        // Apply speed limits based on state
        float maxSpeed = currentState == EnemyState.Chasing ? chaseSpeed :
                        currentState == EnemyState.Fleeing ? chaseSpeed * 1.2f : moveSpeed;
        velocity = Vector2.ClampMagnitude(velocity, maxSpeed);

        // Smooth the movement
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, velocity, pathSmoothness * Time.deltaTime);
    }

    Vector2 GetPredictedPlayerPosition()
    {
        if (player == null) return lastPlayerPosition;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            return (Vector2)player.position + playerRb.linearVelocity * predictionTime;
        }
        return player.position;
    }

    Vector2 Wander()
    {
        // Jitter the wander target
        wanderTarget += new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * wanderJitter * Time.deltaTime;
        wanderTarget = wanderTarget.normalized * wanderRadius;

        // Project target in front of the enemy
        Vector2 targetLocal = wanderTarget;
        Vector2 targetWorld = (Vector2)transform.position + velocity.normalized * wanderDistance + targetLocal;

        return Seek(targetWorld);
    }

    Vector2 Seek(Vector2 target)
    {
        desiredVelocity = (target - (Vector2)transform.position).normalized *
                         (currentState == EnemyState.Chasing ? chaseSpeed : moveSpeed);
        return desiredVelocity - velocity;
    }

    Vector2 Flee(Vector2 target)
    {
        desiredVelocity = ((Vector2)transform.position - target).normalized * chaseSpeed * 1.2f;
        return desiredVelocity - velocity;
    }

    Vector2 Separate()
    {
        Vector2 steering = Vector2.zero;
        int count = 0;

        // Find nearby enemies
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, separationRadius);

        foreach (Collider2D col in nearbyColliders)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                Vector2 diff = (Vector2)transform.position - (Vector2)col.transform.position;
                float distance = diff.magnitude;

                if (distance > 0)
                {
                    diff.Normalize();
                    diff /= distance; // Weight by distance
                    steering += diff;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            steering /= count;
            steering = steering.normalized * moveSpeed - velocity;
        }

        return steering;
    }

    Vector2 AvoidObstacles()
    {
        Vector2 steering = Vector2.zero;

        // Cast rays in multiple directions to detect obstacles
        Vector2[] directions = {
            velocity.normalized,
            Quaternion.Euler(0, 0, 45) * velocity.normalized,
            Quaternion.Euler(0, 0, -45) * velocity.normalized,
            Quaternion.Euler(0, 0, 90) * velocity.normalized,
            Quaternion.Euler(0, 0, -90) * velocity.normalized
        };

        foreach (Vector2 dir in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, obstacleAvoidanceRadius,
                LayerMask.GetMask("Default"));

            if (hit.collider != null && !hit.collider.CompareTag("Enemy") && !hit.collider.CompareTag("Player"))
            {
                Vector2 avoidDirection = Vector2.Perpendicular(dir);
                steering += avoidDirection * (obstacleAvoidanceRadius - hit.distance);
            }
        }

        return steering;
    }

    Vector2 HandleBeingWrappedSteering()
    {
        Vector2 steering = Vector2.zero;

        // Try to escape from rope center
        if (contactRopeSegments.Count > 0)
        {
            Vector2 centerOfRope = Vector2.zero;
            int validSegments = 0;

            foreach (GameObject segment in contactRopeSegments)
            {
                if (segment != null)
                {
                    centerOfRope += (Vector2)segment.transform.position;
                    validSegments++;
                }
            }

            if (validSegments > 0)
            {
                centerOfRope /= validSegments;
                Vector2 escapeDirection = ((Vector2)transform.position - centerOfRope).normalized;
                steering = escapeDirection * chaseSpeed * 0.7f - velocity;
            }
        }

        // Add some random movement to make escape attempts look more natural
        steering += new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 2f;

        return steering;
    }

    void HandleWrapping()
    {
        contactRopeSegments.RemoveAll(segment => segment == null);

        bool hasEnoughSegments = contactRopeSegments.Count >= minRopeSegmentsToWrap;
        bool segmentsAreDistributed = CheckIfSegmentsFormLoop();

        if (hasEnoughSegments && segmentsAreDistributed)
        {
            if (!isBeingWrapped)
            {
                StartWrapping();
            }
            else
            {
                ContinueWrapping();
            }
        }
        else
        {
            if (isBeingWrapped)
            {
                CancelWrapping();
            }
        }
    }

    bool CheckIfSegmentsFormLoop()
    {
        if (contactRopeSegments.Count < 3) return false;

        List<float> angles = new();
        Vector2 enemyPos = transform.position;

        foreach (GameObject segment in contactRopeSegments)
        {
            if (segment != null && segment.GetComponent<RopeSegment>() != null)
            {
                Vector2 direction = (Vector2)segment.transform.position - enemyPos;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                angles.Add(angle);
            }
        }

        if (angles.Count < minRopeSegmentsToWrap)
            return false;

        angles.Sort();

        float totalSpread = 0f;
        for (int i = 1; i < angles.Count; i++)
        {
            float diff = angles[i] - angles[i - 1];
            if (diff > 180f) diff = 360f - diff;
            totalSpread += diff;
        }

        float wrapDiff = (360f + angles[0]) - angles[^1];
        if (wrapDiff > 180f) wrapDiff = 360f - wrapDiff;
        totalSpread += wrapDiff;

        return totalSpread > 200f;
    }

    void StartWrapping()
    {
        isBeingWrapped = true;
        wrapTimer = 0f;

        if (spriteRenderer != null)
            spriteRenderer.color = wrappingColor;
    }

    void ContinueWrapping()
    {
        wrapTimer += Time.deltaTime;

        if (spriteRenderer != null)
        {
            float progress = wrapTimer / wrapTimeRequired;
            spriteRenderer.color = Color.Lerp(wrappingColor, wrappedColor, progress);
        }

        if (wrapTimer >= wrapTimeRequired)
        {
            CompleteWrapping();
        }
    }

    void CompleteWrapping()
    {
        isWrapped = true;
        isBeingWrapped = false;
        currentState = EnemyState.Wrapped;

        foreach (GameObject segment in contactRopeSegments)
        {
            if (segment != null && !attachedSegments.Contains(segment))
            {
                if (segment.TryGetComponent<RopeSegment>(out var ropeSegment))
                {
                    ropeSegment.EnemyAttach(rb);
                    attachedSegments.Add(segment);
                }
            }
        }

        if (spriteRenderer != null)
            spriteRenderer.color = wrappedColor;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (gameManager != null)
            gameManager.OnEnemyWrapped();

        Invoke(nameof(Die), 1f);
    }

    void CancelWrapping()
    {
        isBeingWrapped = false;
        wrapTimer = 0f;
        UpdateAIState();
    }

    void Die()
    {
        if (gameManager != null)
            gameManager.RegisterEnemyDeath(gameObject);

        foreach (GameObject segment in attachedSegments)
        {
            if (segment != null && segment.TryGetComponent<RopeSegment>(out var ropeSegment))
            {
                ropeSegment.DetachFromEnemy();
            }
        }

        GameObject newDieparticle = Instantiate(dieEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<RopeSegment>(out var ropeSegment) && !isWrapped)
        {
            if (!contactRopeSegments.Contains(ropeSegment.gameObject))
            {
                contactRopeSegments.Add(ropeSegment.gameObject);
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<RopeSegment>(out var ropeSegment))
        {
            contactRopeSegments.Remove(ropeSegment.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Chase range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Separation radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        // Obstacle avoidance
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, obstacleAvoidanceRadius);

        // Contact rope segments
        if (contactRopeSegments != null)
        {
            Gizmos.color = Color.red;
            foreach (GameObject segment in contactRopeSegments)
            {
                if (segment != null)
                {
                    Gizmos.DrawLine(transform.position, segment.transform.position);
                }
            }
        }

        // Current target
        if (player != null && currentState == EnemyState.Chasing)
        {
            Gizmos.color = Color.blue;
            Vector2 predictedPos = GetPredictedPlayerPosition();
            Gizmos.DrawLine(transform.position, predictedPos);
            Gizmos.DrawWireSphere(predictedPos, 0.3f);
        }

        // Velocity
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, velocity);
    }

    //public void activeDamage()
    //{
    //    enemyAttack.SetActive(true);
    //}
    //public void disableDamage()
    //{
    //    enemyAttack.SetActive(false);
    //}
}