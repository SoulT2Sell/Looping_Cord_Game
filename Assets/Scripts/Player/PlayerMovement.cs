using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Player player;

    [Header("Movement Settings")]
    public float speed;
    private Vector2 movementDir;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource walkingSource;
    [SerializeField] private AudioClip walkingClip; // Optional: assign walking sound clip
    [SerializeField] private float volumeScale = 1f;
    [SerializeField] private float minimumMovementThreshold = 0.1f; // Threshold to avoid tiny movements triggering audio

    public bool isMoving = false;
    private bool walkingAudioIsPlaying = false;

    void Start()
    {
        player = GetComponent<Player>();
        SetupAudioSource();
        InputEventHandler();
    }

    private void SetupAudioSource()
    {
        // Setup audio source if not configured
        if (walkingSource == null)
        {
            Debug.LogWarning("Walking AudioSource not assigned! Please assign it in the inspector.");
            return;
        }

        // Configure the audio source for looping footsteps
        walkingSource.loop = true;
        walkingSource.playOnAwake = false;
        walkingSource.volume = volumeScale;

        // Assign clip if provided
        if (walkingClip != null)
        {
            walkingSource.clip = walkingClip;
        }

        // Check if audio source has a clip
        if (walkingSource.clip == null)
        {
            Debug.LogWarning("Walking AudioSource has no AudioClip assigned!");
        }
    }

    private void InputEventHandler()
    {
        player.inputActions.Player.Move.performed += context =>
        {
            movementDir = context.ReadValue<Vector2>();
            isMoving = movementDir.magnitude > minimumMovementThreshold;
        };

        player.inputActions.Player.Move.canceled += context =>
        {
            movementDir = Vector2.zero;
            isMoving = false;
        };
    }

    private void FixedUpdate()
    {
        // Apply movement
        player.rb.linearVelocity = movementDir * speed;

        // Handle walking audio based on actual velocity (more reliable than input)
        HandleWalkingAudio();
    }

    private void HandleWalkingAudio()
    {
        if (walkingSource == null || walkingSource.clip == null)
            return;

        // Check if player is actually moving (using velocity magnitude)
        bool shouldPlayAudio = player.rb.linearVelocity.magnitude > minimumMovementThreshold;

        // Start audio if moving and not already playing
        if (shouldPlayAudio && !walkingAudioIsPlaying)
        {
            walkingSource.Play();
            walkingAudioIsPlaying = true;
            Debug.Log("Started walking audio");
        }
        // Stop audio if not moving and currently playing
        else if (!shouldPlayAudio && walkingAudioIsPlaying)
        {
            walkingSource.Stop();
            walkingAudioIsPlaying = false;
            Debug.Log("Stopped walking audio");
        }
    }

    // Optional: Public method to adjust walking volume
    public void SetWalkingVolume(float volume)
    {
        volumeScale = Mathf.Clamp01(volume);
        if (walkingSource != null)
        {
            walkingSource.volume = volumeScale;
        }
    }

    // Cleanup when object is destroyed
    private void OnDestroy()
    {
        if (walkingSource != null && walkingAudioIsPlaying)
        {
            walkingSource.Stop();
        }
    }

    // Optional: Pause/Resume methods for game pause functionality
    public void PauseWalkingAudio()
    {
        if (walkingSource != null && walkingAudioIsPlaying)
        {
            walkingSource.Pause();
        }
    }

    public void ResumeWalkingAudio()
    {
        if (walkingSource != null && walkingAudioIsPlaying)
        {
            walkingSource.UnPause();
        }
    }
}