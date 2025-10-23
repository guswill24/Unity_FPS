using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    public int levelScore;
    public float levelTimer = 10f;
    private string levelName1 = "SciFi_Industrial";
    private string levelName2 = "Isla";

    [Header("Estado del juego")]
    public bool isGameActive = false;
    public GameObject mainMenuPanel;

    [Header("Mensajes")]
    public TextMeshProUGUI messageText;
    public float messageDuration = 2f;

    private static bool hasSessionStarted = false;
    private bool levelEndTriggered = false;

    void Awake() => instance = this;

    void Start()
    {
        levelScore = 0;

        if (messageText != null)
        {
            messageText.text = string.Empty;
            messageText.gameObject.SetActive(false);
        }

        if (!hasSessionStarted)
        {
            ShowMainMenu();
        }
        else
        {
            isGameActive = true;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if(mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!isGameActive) return;

        string current = SceneManager.GetActiveScene().name;

        if (current == levelName1)
            HandleLevel1();
        else if (current == levelName2)
            HandleLevel2();
    }

    public void StartGame()
    {
        hasSessionStarted = true;
        isGameActive = true;

        // Oculta el menú antes de recargar
        if (mainMenuPanel != null) 
            mainMenuPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Recargar la escena actual para reiniciar todo (enemigos incluidos)
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowMainMenu()
    {
        isGameActive = false;
        if(mainMenuPanel != null) mainMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleLevel1()
    {
        if(levelEndTriggered) return;

        if(levelScore < 4)
        {
            if(levelTimer > 0f)
                levelTimer -= Time.deltaTime;
            else
            {
                levelEndTriggered = true;
                ShowMessage("Game Over");
                StartCoroutine(ReloadAfterDelay(levelName1, messageDuration));
            }
        }
        else
        {
            levelEndTriggered = true;
            StartCoroutine(LoadAfterDelay(levelName2, 0.25f));
        }
    }

    private void HandleLevel2()
    {
        if(levelEndTriggered) return;

        var crawler = FindObjectOfType<CrawlerEnemy>();
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        var ph = playerObj?.GetComponent<PlayerHealth>();

        if(crawler == null)
        {
            levelEndTriggered = true;
            StartCoroutine(ShowLevelCompletedThenMenu());
        }
        else if(ph != null && ph.health <= 0f)
        {
            levelEndTriggered = true;
            ShowMessage("Game Over");
            StartCoroutine(ReloadAfterDelay(levelName1, messageDuration));
        }
    }

    private IEnumerator ShowLevelCompletedThenMenu()
    {
        ShowMessage("Nivel completado");
        yield return new WaitForSeconds(messageDuration);
        ShowMainMenu();
    }

    private void ShowMessage(string msg)
    {
        if(messageText != null)
        {
            messageText.text = msg;
            messageText.gameObject.SetActive(true);
            StartCoroutine(HideMessageAfterDelay());
        }
        Debug.Log(msg);
    }

    private IEnumerator ReloadAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        if(messageText != null)
            messageText.gameObject.SetActive(false);
    }
}
