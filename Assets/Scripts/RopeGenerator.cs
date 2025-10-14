using System.Collections.Generic;
using UnityEngine;

public class RopeGenerator : MonoBehaviour
{
    [Header("Rope Settings")]
    public GameObject ropeSegmentPrefab;
    public int segmentCount = 25;
    public float segmentSpacing = 0.2f;
    public Transform attachPoint; // usually player's back or hand
    public Transform segmentParent;

    [Header("Enhanced Rope Physics")]
    public float jointSpring = 500f;
    public float jointDamper = 25f;
    public float segmentMass = 0.1f;
    public float segmentDrag = 0.5f;
    public float segmentAngularDrag = 0.8f;

    [Header("Dynamic Rope Behavior")]
    public float ropeStiffness = 0.8f;
    public float tensionMultiplier = 1.2f;

    private readonly List<GameObject> ropeSegments = new();
    private Player player;

    void Start()
    {
        player = attachPoint.GetComponent<Player>();
        GenerateRope();
    }

    void GenerateRope()
    {
        Rigidbody2D previousRb = attachPoint.GetComponent<Rigidbody2D>();
        Collider2D playerCollider = attachPoint.GetComponent<Collider2D>();

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 segmentPos = attachPoint.position - new Vector3(0, i * segmentSpacing, 0);
            GameObject segment = Instantiate(ropeSegmentPrefab, segmentPos, Quaternion.identity, segmentParent);

            segment.GetComponent<SpriteRenderer>().sortingOrder = i;

            // Configure Collider
            Collider2D segmentCollider = segment.GetComponent<Collider2D>();

            // Add to segments list first
            ropeSegments.Add(segment);

            // Setup segment physics and constraints
            SetupEnhancedSegment(segment, previousRb, i);

            segment.name = $"RopeSegment_{i}";
            previousRb = segment.GetComponent<Rigidbody2D>();
        }
    }

    void SetupEnhancedSegment(GameObject segment, Rigidbody2D previousRb, int segmentIndex)
    {
        // Setup Enhanced Rigidbody2D
        if (segment.TryGetComponent<Rigidbody2D>(out var rb))
        {
            // Gradually increase mass toward the end for more natural behavior
            rb.mass = segmentMass * (1f + segmentIndex * 0.05f);
            rb.linearDamping = segmentDrag;
            rb.angularDamping = segmentAngularDrag;
            rb.gravityScale = 0f; // Top-down game
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        // Setup Distance Joint for basic rope constraint
        if (segment.TryGetComponent<DistanceJoint2D>(out var joint))
        {
            joint.connectedBody = previousRb;
            joint.autoConfigureDistance = false;
            joint.distance = segmentSpacing * ropeStiffness;
            joint.maxDistanceOnly = false; // Allow both compression and extension
        }
        else
        {
            // Add DistanceJoint2D if it doesn't exist
            joint = segment.AddComponent<DistanceJoint2D>();
            joint.connectedBody = previousRb;
            joint.autoConfigureDistance = false;
            joint.distance = segmentSpacing * ropeStiffness;
            joint.maxDistanceOnly = false;
        }

        // Add SpringJoint2D for enhanced rope physics
        if (!segment.TryGetComponent<SpringJoint2D>(out var springJoint))
        {
            springJoint = segment.AddComponent<SpringJoint2D>();
        }

        if (springJoint != null)
        {
            springJoint.connectedBody = previousRb;
            springJoint.autoConfigureDistance = false;
            springJoint.distance = segmentSpacing;
            springJoint.frequency = jointSpring * 0.001f; // Convert to appropriate range (0-1000)
            springJoint.dampingRatio = jointDamper * 0.01f; // Convert to 0-1 range
        }

        // Setup RopeSegment script with enhanced parameters
        if (segment.TryGetComponent<RopeSegment>(out var ropeSegment))
        {
            ropeSegment.player = player;
        }

        // Apply tension based on distance from player for natural rope behavior
        float tensionFactor = (float)segmentIndex / segmentCount;
        if (segment.TryGetComponent<Rigidbody2D>(out var segmentRb))
        {
            // Segments further from player are slightly heavier
            segmentRb.mass *= (1f + tensionFactor * tensionMultiplier * 0.2f);
        }
    }

    public List<GameObject> GetRopeSegments()
    {
        return ropeSegments;
    }

    public void DetachAllEnemies()
    {
        foreach (GameObject segment in ropeSegments)
        {
            if (segment != null)
            {
                RopeSegment ropeSegment = segment.GetComponent<RopeSegment>();
                if (ropeSegment != null)
                {
                    ropeSegment.DetachFromEnemy();
                }
            }
        }
    }

    // Method to dynamically adjust rope stiffness based on player speed
    public void AdjustRopeStiffness(float playerSpeed)
    {
        float dynamicStiffness = Mathf.Lerp(0.6f, 1.2f, playerSpeed / 10f);
        ropeStiffness = dynamicStiffness;

        // Update joint distances for all segments
        for (int i = 0; i < ropeSegments.Count; i++)
        {
            if (ropeSegments[i] != null)
            {
                if (ropeSegments[i].TryGetComponent<DistanceJoint2D>(out var joint))
                {
                    joint.distance = segmentSpacing * ropeStiffness;
                }

                if (ropeSegments[i].TryGetComponent<SpringJoint2D>(out var springJoint))
                {
                    springJoint.distance = segmentSpacing * ropeStiffness;
                }
            }
        }
    }

    // Method to get rope tension for gameplay feedback
    public float GetAverageRopeTension()
    {
        float totalTension = 0f;
        int validSegments = 0;

        foreach (GameObject segment in ropeSegments)
        {
            if (segment != null && segment.TryGetComponent<Rigidbody2D>(out var rb))
            {
                totalTension += rb.linearVelocity.magnitude;
                validSegments++;
            }
        }

        return validSegments > 0 ? totalTension / validSegments : 0f;
    }

    // Method to apply force to entire rope (for special effects)
    public void ApplyForceToRope(Vector2 force, ForceMode2D mode = ForceMode2D.Force)
    {
        foreach (GameObject segment in ropeSegments)
        {
            if (segment != null && segment.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.AddForce(force, mode);
            }
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (attachPoint != null)
        {
            // Draw attach point
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(attachPoint.position, 0.2f);

            // Draw rope segments and connections
            if (ropeSegments != null && ropeSegments.Count > 0)
            {
                Gizmos.color = Color.blue;
                Vector3 previousPos = attachPoint.position;

                foreach (GameObject segment in ropeSegments)
                {
                    if (segment != null)
                    {
                        Gizmos.DrawLine(previousPos, segment.transform.position);
                        previousPos = segment.transform.position;
                    }
                }
            }

            // Draw rope constraints
            Gizmos.color = Color.yellow;
            for (int i = 0; i < ropeSegments.Count; i++)
            {
                if (ropeSegments[i] != null)
                {
                    Vector3 segmentPos = ropeSegments[i].transform.position;
                    Gizmos.DrawWireSphere(segmentPos, segmentSpacing * ropeStiffness);
                }
            }
        }
    }
}