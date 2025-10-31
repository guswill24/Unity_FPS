using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Referencia de enemigo")]
    public MonoBehaviour enemy; // Cualquier script del enemigo
    public bool autoFindEnemyInParent = true;

    [Header("Salud")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool autoSetMaxFromStart = true;

    [Header("UI")]
    public Image healthBarFill; // Fill de la barra
    public TextMeshProUGUI healthText; // Texto opcional
    public bool autoFindUI = true;
    public Slider healthSlider; // Soporte opcional para Slider

    void Start()
    {
        if (autoFindEnemyInParent && enemy == null)
            enemy = FindEnemyComponentInParents();

        if (autoFindUI)
            AutoFindUIRefs();

        // Inicializa maxHealth con la salud actual del enemigo si procede
        float h;
        if (autoSetMaxFromStart && TryGetHealth(out h) && h > 0f)
            maxHealth = h;

        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        float h;
        if (!TryGetHealth(out h)) return;

        currentHealth = Mathf.Max(0f, h);

        if (healthBarFill != null && maxHealth > 0.0001f)
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        if (healthSlider != null && maxHealth > 0.0001f)
            healthSlider.value = Mathf.Clamp01(currentHealth / maxHealth);

        if (healthText != null)
        {
            int vidaEntera = Mathf.RoundToInt(currentHealth);
            healthText.text = vidaEntera.ToString();
        }
    }

    private MonoBehaviour FindEnemyComponentInParents()
    {
        // Intenta con tipos conocidos
        var ce = GetComponentInParent<CrawlerEnemy>();
        if (ce != null) return ce;
        var ge = GetComponentInParent<GolemEnemy>();
        if (ge != null) return ge;
        var se = GetComponentInParent<SkeletonEnemy>();
        if (se != null) return se;
        var ez = GetComponentInParent<EnemyZombi>();
        if (ez != null) return ez;

        // Fallback: cualquier MonoBehaviour que tenga 'health'
        var behaviours = GetComponentsInParent<MonoBehaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            if (HasHealthFieldOrProperty(b)) return b;
        }
        Debug.LogWarning("EnemyHealthBar: No se encontró componente de enemigo en los padres.", this);
        return null;
    }

    private void AutoFindUIRefs()
    {
        if (healthBarFill == null)
        {
            // Busca un Image que parezca ser el Fill
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                string n = img.name.ToLowerInvariant();
                if (n.Contains("fill") || n.Contains("bar"))
                {
                    healthBarFill = img;
                    break;
                }
            }
            if (healthBarFill == null && images.Length > 0)
                healthBarFill = images[0];
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>(true);
        }

        if (healthText == null)
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts != null && texts.Length > 0)
                healthText = texts[0];
        }

        if (healthBarFill == null && healthSlider == null && healthText == null)
            Debug.LogWarning("EnemyHealthBar: No se asignaron referencias de UI (Image/Text).", this);
    }

    private bool TryGetHealth(out float healthValue)
    {
        healthValue = 0f;
        if (enemy == null)
        {
            Debug.LogWarning("EnemyHealthBar: 'enemy' no asignado.", this);
            return false;
        }

        // Tipos conocidos (rápido y seguro)
        if (enemy is CrawlerEnemy c) { healthValue = c.health; return true; }
        if (enemy is GolemEnemy g)   { healthValue = g.health; return true; }
        if (enemy is SkeletonEnemy s){ healthValue = s.health; return true; }
        if (enemy is EnemyZombi ez)  { healthValue = ez.health; return true; }

        // Reflexión genérica: campo/propiedad 'health' o 'Health'
        var t = enemy.GetType();
        var prop = t.GetProperty("health", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop != null && prop.CanRead)
        {
            var val = prop.GetValue(enemy, null);
            if (TryToFloat(val, out healthValue)) return true;
        }
        var field = t.GetField("health", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (field != null)
        {
            var val = field.GetValue(enemy);
            if (TryToFloat(val, out healthValue)) return true;
        }

        return false;
    }

    private bool HasHealthFieldOrProperty(MonoBehaviour b)
    {
        var t = b.GetType();
        var prop = t.GetProperty("health", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop != null && (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(int))) return true;
        var field = t.GetField("health", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (field != null && (field.FieldType == typeof(float) || field.FieldType == typeof(int))) return true;
        return false;
    }

    private bool TryToFloat(object val, out float f)
    {
        f = 0f;
        if (val is float fv) { f = fv; return true; }
        if (val is int iv)   { f = iv; return true; }
        return false;
    }
}


