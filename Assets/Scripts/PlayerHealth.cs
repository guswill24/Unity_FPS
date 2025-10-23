using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float health = 100f;
    public Slider healthBar; // Asigna una barra en UI

    void Start()
    {
        health = Mathf.Clamp(health, 0f, maxHealth);
        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = maxHealth;
            UpdateHealthUI();
        }
        else
        {
            Debug.LogWarning("PlayerHealth: 'healthBar' no asignado en el Inspector");
        }
    }

    public void TakeDamage(float amount)
    {
        health = Mathf.Clamp(health - amount, 0f, maxHealth);
        Debug.Log("Player Health: " + health);
        if (healthBar != null)
        {
            UpdateHealthUI();
        }
        else
        {
            Debug.LogWarning("PlayerHealth: 'healthBar' no asignado en el Inspector");
        }
        if (health <= 0f)
        {
            Debug.Log("PLAYER DEAD");
            // Aquí puedes recargar escena, mostrar pantalla de muerte, etc.
        }
    }

    private void UpdateHealthUI()
    {
        if (healthBar == null) return;
        healthBar.value = Mathf.Clamp(health, 0f, maxHealth);
    }
}
