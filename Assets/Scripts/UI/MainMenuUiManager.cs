using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MainMenuUiManager : MonoBehaviour
{
    public static MainMenuUiManager Instance;

    public enum UI { mainmenuUI, InfoUi }
    [SerializeField] private List<GameObject> uis;
    [SerializeField] private TMP_Text highScoreTxt;

    [Header("UI Sound")]
    [SerializeField] private AudioClip clickSound;   // sound to play on UI clicks
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip music;
    [SerializeField] private AudioSource musicSource;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Setup audio source for UI clicks
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        musicSource.Play();
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        highScoreTxt.text = $"High Score : {highScore}";
    }

    public void StartButton()
    {
        StartCoroutine(StartRoutine());
    }

    private IEnumerator StartRoutine()
    {
        PlayClick();
        yield return new WaitForSecondsRealtime(0.1f); 
        Time.timeScale = 1;
        SceneManager.LoadScene(1);
    }

    public void InfoButton()
    {
        PlayClick();
        EnableUI(UI.InfoUi);
    }

    public void BackToMenuButton()
    {
        PlayClick();
        EnableUI(UI.mainmenuUI);
    }

    public void ExitButton()
    {
        PlayClick();
        Application.Quit();
    }

    private void EnableUI(UI selectedUi)
    {
        foreach (GameObject ui in uis)
        {
            ui.SetActive(false);
        }
        uis[((int)selectedUi)].SetActive(true);
    }


    private void PlayClick()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound);
    }
}