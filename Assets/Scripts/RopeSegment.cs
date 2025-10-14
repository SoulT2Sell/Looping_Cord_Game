using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class RopeSegment : MonoBehaviour
{
    [Header("Rope Physics")]
    public float maxVelocity = 10f;
    public float dragMultiplier = 0.98f;
    private Rigidbody2D rb;
    public Player player;
    private List<DistanceJoint2D> enemyJoints = new List<DistanceJoint2D>();

    [Header("Visual Settings")]
    [SerializeField] private List<Sprite> segmentsImgList = new List<Sprite>(); 
    public Color normalColor;
    public Color wrappingColor = Color.orange;
    private SpriteRenderer spriteRenderer;
    private bool isAttachedToEnemy = false;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = segmentsImgList[Random.Range(0, segmentsImgList.Count)];
        normalColor = spriteRenderer.color;
    
        //if (spriteRenderer != null)
        //   spriteRenderer.color = normalColor;
    }
    private void Update()
    {
        LimitVelocity();
        ApplyDrag();
    }
    private void LimitVelocity()
    {
        // Limit rope segment velocity based on player movement
        if (player != null && rb.linearVelocity.magnitude > player.rb.linearVelocity.magnitude + 2f)
        {
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, player.rb.linearVelocity.magnitude + 2f);
        }
        // Overall velocity limit
        if (rb.linearVelocity.magnitude > maxVelocity)
        {
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxVelocity);
        }
    }
    private void ApplyDrag()
    {
        // Apply slight drag when player isn't moving to make rope settle
        if (player != null && !player.playerMovement.isMoving)
        {
            rb.linearVelocity *= dragMultiplier;
        }
    }
    public void EnemyAttach(Rigidbody2D enemy)
    {
        if (enemy == null || isAttachedToEnemy) return;
        DistanceJoint2D enemyAttachJoint = gameObject.AddComponent<DistanceJoint2D>();
        enemyAttachJoint.connectedBody = enemy;
        enemyAttachJoint.autoConfigureDistance = false;
        // Calculate distance and add some slack for natural rope behavior
        Vector2 jointDistance = enemy.gameObject.transform.position - transform.position;
        enemyAttachJoint.distance = jointDistance.magnitude * 0.8f; // 80% of current distance
        enemyAttachJoint.maxDistanceOnly = true; // Only limit maximum distance
        // Store the joint for later cleanup
        enemyJoints.Add(enemyAttachJoint);
        isAttachedToEnemy = true;
        // Change visual feedback
        if (spriteRenderer != null)
            spriteRenderer.color = wrappingColor;
        Debug.Log($"Rope segment {gameObject.name} attached to enemy {enemy.gameObject.name}");
    }
    public void DetachFromEnemy()
    {
        // Remove all enemy joints
        foreach (DistanceJoint2D joint in enemyJoints)
        {
            if (joint != null)
            {
                Destroy(joint);
            }
        }
        enemyJoints.Clear();
        isAttachedToEnemy = false;
        // Reset visual feedback
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
        Debug.Log($"Rope segment {gameObject.name} detached from enemy");
    }
    public bool IsAttachedToEnemy()
    {
        return isAttachedToEnemy;
    }
    // Clean up destroyed joints
    private void LateUpdate()
    {
        enemyJoints.RemoveAll(joint => joint == null);
        if (enemyJoints.Count == 0 && isAttachedToEnemy)
        {
            isAttachedToEnemy = false;
            if (spriteRenderer != null)
                spriteRenderer.color = normalColor;
        }
    }
}