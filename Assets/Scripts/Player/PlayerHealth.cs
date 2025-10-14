using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerHealth : MonoBehaviour
{
    private Player player;
    private CinemachineImpulseSource cinemachineImpulseSource;

    [Header("Health Settings")]
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private GameObject dieEffect1;
    [SerializeField] private GameObject dieEffect2;
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invincibilityDuration = 1f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioSource damageAudioSource; // Optional: for damage sounds
    [SerializeField] private AudioClip damageSFX; // Optional: for damage sounds
    [SerializeField] private float deathAudioVolume = 1f;
    [SerializeField] private float damageAudioVolume = 0.7f;

    private int currentHealth;
    private bool isPlayerDead = false;
    private bool isInvincible = false;

    [SerializeField] private AudioClip healthPickup;
    [SerializeField] private AudioSource healthPickupSource;
    
    void Start()
    {
        player = GetComponent<Player>();
        cinemachineImpulseSource = GetComponent<CinemachineImpulseSource>();
        currentHealth = maxHealth;
        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        // Setup death audio source
        if (deathAudioSource != null)
        {
            deathAudioSource.loop = false;
            deathAudioSource.playOnAwake = false;
            deathAudioSource.volume = deathAudioVolume;

            if (deathSFX != null)
                deathAudioSource.clip = deathSFX;
        }
        else
        {
            Debug.LogWarning("Death AudioSource not assigned! Please assign it in the inspector.");
        }

        // Setup damage audio source (optional)
        if (damageAudioSource != null)
        {
            damageAudioSource.loop = false;
            damageAudioSource.playOnAwake = false;
            damageAudioSource.volume = damageAudioVolume;

            if (damageSFX != null)
                damageAudioSource.clip = damageSFX;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInvincible && collision.gameObject.CompareTag("Enemy"))
            TakeDamage(collision.gameObject.GetComponent<EnemyBehavior>().damage);
    }

    public void TakeDamage(int amount)
    {
        currentHealth = currentHealth - amount;
        UIManager.instance.DamageFlash();

        if (currentHealth <= 0)
        {
            // Play death sound BEFORE destroying the object
            PlayDeathSFX();
            GetComponentInChildren<SpriteRenderer>().enabled = false;
            GetComponent<CapsuleCollider2D>().enabled = false;
            Instantiate(dieEffect1, transform.position, Quaternion.identity);
            cinemachineImpulseSource.GenerateImpulse();
            Instantiate(dieEffect2, transform.position, Quaternion.identity);

            // Delay the Die() call to let the death sound play
            StartCoroutine(DelayedDeath());
        }
        else
        {
            // Play damage sound
            PlayDamageSFX();

            Instantiate(damageEffect, transform.position, Quaternion.identity);
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    private void PlayDeathSFX()
    {
        if (deathAudioSource != null && deathSFX != null)
        {
            deathAudioSource.Play();
            Debug.Log("Playing death SFX");
        }
        else
        {
            Debug.LogWarning("Cannot play death SFX - AudioSource or AudioClip missing!");
        }
    }

    private void PlayDamageSFX()
    {
        if (damageAudioSource != null && damageSFX != null)
        {
            damageAudioSource.Play();
            Debug.Log("Playing damage SFX");
        }
    }

    private IEnumerator DelayedDeath()
    {
        // Wait for death sound to start playing
        if (deathAudioSource != null && deathSFX != null)
        {
            // Wait for the length of the death sound, or at least 1 second
            float waitTime = Mathf.Max(deathSFX.length, 1f);
            yield return new WaitForSeconds(waitTime);
        }
        else
        {
            // If no death sound, just wait a moment for visual effects
            yield return new WaitForSeconds(0.5f);
        }

        Die();
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    void Die()
    {
        isPlayerDead = true;
        Debug.Log("Player died!");
        GetComponent<Collider2D>().enabled = false;
        enabled = false;
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HealthPickup") && currentHealth < maxHealth)
        {
            healthPickupSource.Play();
            currentHealth += 1;
            player.playerAnimationController.isHealing = true;
            Destroy(other.gameObject);
        }
    }

    public bool GetIsPlayerDead() => isPlayerDead;
    public int GetPlayerCurrentHealth() => currentHealth;
    public int GetPlayerMaxHealth() => maxHealth;

    // Optional: Method to set death volume at runtime
    public void SetDeathVolume(float volume)
    {
        deathAudioVolume = Mathf.Clamp01(volume);
        if (deathAudioSource != null)
            deathAudioSource.volume = deathAudioVolume;
    }
}