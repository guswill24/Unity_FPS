using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Botones del Menú")]
    public Button playButton;
    public Button quitButton;

    [Header("Paneles de la UI")]
    public GameObject panelSuperior;  // Panel HUD superior
    public GameObject panelMenu;      // Panel del menú principal

    void Start()
    {
        // Verifica y asigna eventos a los botones
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        else
            Debug.LogWarning("Falta asignar el botón Play en el inspector.");

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        else
            Debug.LogWarning("Falta asignar el botón Quit en el inspector.");

        // Configura visibilidad inicial
        if (panelSuperior != null)
            panelSuperior.SetActive(false);

        if (panelMenu != null)
            panelMenu.SetActive(true);
    }

    private void OnPlayClicked()
    {

        // Oculta el panel del menú y muestra el superior
        if (panelMenu != null)
            panelMenu.SetActive(false);
        else
            Debug.LogWarning("PanelMenu no asignado en el inspector.");

        if (panelSuperior != null)
        {
            panelSuperior.SetActive(true);
        }
        else
        {
            Debug.LogWarning("PanelSuperior no asignado en el inspector.");
        }

        // Llama al LevelManager si existe
        if (LevelManager.instance != null)
            LevelManager.instance.StartGame();
    }

    private void OnQuitClicked()
    {
        Debug.Log("Saliendo del juego...");
        if (LevelManager.instance != null)
            LevelManager.instance.QuitGame();
        else
            Application.Quit();
    }
}

