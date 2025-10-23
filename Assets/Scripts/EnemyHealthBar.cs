using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthBar : MonoBehaviour
{
    public CrawlerEnemy enemy; // referencia al enemigo
    public float maxHealth = 100f;
    public float currentHealth;
    public Image healthBarFill; // Asigna el Fill del canvas
    public TextMeshProUGUI healthText; // Asigna el texto si usas números

    void Start()
    {
        if (enemy == null)
            enemy = GetComponentInParent<CrawlerEnemy>();

        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (enemy == null || healthText == null) return;

        int vidaEntera = Mathf.RoundToInt(enemy.health);
        vidaEntera = Mathf.Clamp(vidaEntera, 0, 100); // asegura que no pase de 100 ni baje de 0
        healthText.text = vidaEntera.ToString();
    }
}


