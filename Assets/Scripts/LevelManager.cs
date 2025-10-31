using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    public int levelScore;
    public float levelTimer = 10f;
    [Header("Secuencia de escenas")]
    [Tooltip("Lista ordenada de escenas (índice 0: Nivel 1, índice 1: Nivel 2, resto: genéricos)")]
    public string[] levelNames = new string[0];

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

        int currentIndex = GetLevelIndex(SceneManager.GetActiveScene().name);
        if (currentIndex == 0)
            HandleLevel1();
        else if (currentIndex == 1)
            HandleLevel2();
        else if (currentIndex >= 2)
            HandleGenericLevel();
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
                StartCoroutine(ReloadAfterDelay(GetLevelName(0), messageDuration));
            }
        }
        else
        {
            levelEndTriggered = true;
            StartCoroutine(LoadNextOrMenuAfterDelay(0.25f));
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
            StartCoroutine(LoadNextOrMenuAfterDelay(0.25f));
        }
        else if(ph != null && ph.health <= 0f)
        {
            levelEndTriggered = true;
            ShowMessage("Game Over");
            StartCoroutine(ReloadAfterDelay(GetLevelName(0), messageDuration));
        }
    }

    // Manejador genérico para niveles extra: avanza al siguiente cuando no hay enemigos restantes
    private void HandleGenericLevel()
    {
        if(levelEndTriggered) return;

        // Criterio simple: si no queda ningún enemigo en escena, avanza
        var anyEnemy = FindObjectOfType<CrawlerEnemy>()
                    || FindObjectOfType<GolemEnemy>()
                    || FindObjectOfType<SkeletonEnemy>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        var ph = playerObj?.GetComponent<PlayerHealth>();

        if(!anyEnemy)
        {
            levelEndTriggered = true;
            StartCoroutine(LoadNextOrMenuAfterDelay(0.25f));
        }
        else if(ph != null && ph.health <= 0f)
        {
            levelEndTriggered = true;
            ShowMessage("Game Over");
            StartCoroutine(ReloadAfterDelay(GetLevelName(0), messageDuration));
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
        // Auto-localiza el Text si no está asignado
        if (messageText == null)
        {
            var texts = GameObject.FindObjectsOfType<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i];
                if (t != null && t.name.ToLowerInvariant().Contains("message"))
                {
                    messageText = t;
                    break;
                }
            }
        }

        if(messageText != null)
        {
            // Asegura que el Canvas/jerarquía estén activos
            var canvas = messageText.GetComponentInParent<Canvas>(true);
            if (canvas != null) canvas.gameObject.SetActive(true);

            messageText.text = msg;
            messageText.gameObject.SetActive(true);
            StartCoroutine(HideMessageAfterDelay());
        }
        else
        {
            Debug.LogWarning("LevelManager: 'messageText' no está asignado ni pudo autolocalizarse. No se mostrará el mensaje en UI.");
        }
        Debug.Log(msg);
    }

    private string[] GetLevelSequence()
    {
        // Devuelve el array configurado en el inspector, filtrando entradas vacías
        System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
        if (levelNames != null)
        {
            for (int i = 0; i < levelNames.Length; i++)
            {
                var s = levelNames[i];
                if (!string.IsNullOrEmpty(s)) list.Add(s);
            }
        }
        return list.ToArray();
    }

    private int GetLevelIndex(string sceneName)
    {
        var seq = GetLevelSequence();
        for (int i = 0; i < seq.Length; i++)
        {
            if (seq[i] == sceneName) return i;
        }
        return -1;
    }

    private string GetNextLevelName()
    {
        string current = SceneManager.GetActiveScene().name;
        var seq = GetLevelSequence();
        int idx = GetLevelIndex(current);
        if (idx >= 0 && idx + 1 < seq.Length) return seq[idx + 1];
        return null; // No hay siguiente
    }

    private string GetLevelName(int index)
    {
        var seq = GetLevelSequence();
        if (index >= 0 && index < seq.Length) return seq[index];
        return SceneManager.GetActiveScene().name;
    }

    private IEnumerator LoadNextOrMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        string next = GetNextLevelName();
        if (!string.IsNullOrEmpty(next))
            SceneManager.LoadScene(next);
        else
            ShowMainMenu();
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
