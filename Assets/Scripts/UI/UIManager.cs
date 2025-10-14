using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    private GameManager gameManager;
    private PlayerHealth playerHealth;

    [Header("PauseUI")]
    [SerializeField] private GameObject pauseUI;

    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Score UI")]
    [SerializeField] private GameObject scoreUI;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI enemiesRemaining;

    [Header("Wave UI")]
    [SerializeField] private TextMeshProUGUI waveCounterText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button restartButton;

    [Header("Player Damage Feedback")]
    [SerializeField] private Image damageFlashImage;
    [SerializeField] private Color flashColor = new Color(1, 0, 0, 0.4f);
    [SerializeField] private float flashFadeSpeed = 3f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;   // assign in Inspector
    [SerializeField] private AudioClip clickSound;      // assign your button SFX here

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        gameManager = GameManager.instance;
    }

    void Update()
    {
        UpdateHealthDisplay();
        UpdateScoreUI();
        UpdateWaveCounterDisplay();
        ActivePlayerDamageFeedback();
    }

    public void DamageFlash()
    {
        if (damageFlashImage != null)
            damageFlashImage.color = flashColor;
    }

    private void ActivePlayerDamageFeedback()
    {
        if (damageFlashImage != null)
            damageFlashImage.color = Color.Lerp(damageFlashImage.color, Color.clear, flashFadeSpeed * Time.deltaTime);
    }

    private void UpdateWaveCounterDisplay()
    {
        waveCounterText.text = $"{gameManager.GetWaveCount()}";
    }

    private void UpdateHealthDisplay()
    {
        if (playerHealth == null || healthText == null) return;

        healthText.text = $"Health: {playerHealth.GetPlayerCurrentHealth()}/{playerHealth.GetPlayerMaxHealth()}";

        if (healthSlider != null)
        {
            healthSlider.maxValue = playerHealth.GetPlayerMaxHealth();
            healthSlider.value = playerHealth.GetPlayerCurrentHealth();
        }
    }

    private void UpdateScoreUI()
    {
        if (gameManager == null) return;

        if (scoreText != null)
            scoreText.text = $"{gameManager.GetScore()}";

        if (enemiesRemaining != null)
            enemiesRemaining.text = $"EnemiesRemaining: {gameManager.GetEnemiesRemaining()}";
    }

    public void ShowGameOver()
    {
        // optional: play a sound on game over UI appear
        PlayClick();

        if (gameOverPanel == null || gameManager == null) return;

        scoreUI.SetActive(false);
        gameOverPanel.SetActive(true);

        int playerScore = gameManager.GetScore();
        int enemiesKilled = gameManager.GetEnemiesKilled();

        if (finalScoreText != null)
        {
            finalScoreText.text = $"GAME OVER\n\nFinal Score: {playerScore}\nEnemies Defeated: {enemiesKilled}";
        }

        int lastHighScore = PlayerPrefs.GetInt("HighScore", 0);

        if (playerScore > lastHighScore)
        {
            PlayerPrefs.SetInt("HighScore", playerScore);
        }
    }

    public void ActivePauseUI()
    {
        PlayClick();
        Time.timeScale = 0;
        pauseUI.SetActive(true);
    }

    public void ResumeButton()
    {
        PlayClick();
        Time.timeScale = 1;
        pauseUI.SetActive(false);
    }

    public void BackToMenu()
    {
        StartCoroutine(BackToMenuRoutine());
    }
    private IEnumerator BackToMenuRoutine()
    {
        PlayClick();
        yield return new WaitForSecondsRealtime(0.1f); // ? Fix here
        SceneManager.LoadScene(0);
    }

    public void RestartButton()
    {
        StartCoroutine(RestartRoutine());
    }
    private IEnumerator RestartRoutine()
    {
        PlayClick();
        yield return new WaitForSecondsRealtime(0.1f); // ? Fix here
        Time.timeScale = 1;
        SceneManager.LoadScene(1);
    }
    private void PlayClick()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }
}
