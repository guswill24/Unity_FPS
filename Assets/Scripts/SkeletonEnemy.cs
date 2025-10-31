using UnityEngine;
using UnityEngine.AI;
using System.Reflection;

public class SkeletonEnemy : MonoBehaviour
{
    [Header("Configuración de detección")]
    public Transform player;
    public float detectionRange = 10f;
    public float attackRange = 2f;

    [Header("Estadísticas del enemigo")]
    public float health = 100f;
    public float damage = 10f;
    public float attackCooldown = 2f;

    private Animator anim;
    private NavMeshAgent agent;
    private bool isDead = false;
    private float lastAttackTime = 0f;
    private PlayerHealth targetPlayerHealth;

    private bool isWindingUp = false;
    private float scheduledAttackTime = 0f;

    [Header("Ataque")]
    public float windUpDuration = 0.15f;
    public float attackLeeway = 0.25f;

    [Header("Audio")]
    public AudioClip ataqueSound;
    public AudioSource audioSourceAtaque;
    public AudioClip muerteSound;
    public AudioSource audioSourcemuerte;

    private bool agentManuallyEnabled = false;
    private bool awaitingNavMesh = true;
    private float navMeshRetryTimer = 0f;
    public float navMeshRetryInterval = 0.5f;

    [Header("Daño entrante del jugador")]
    [Tooltip("Tag que llevan los proyectiles del jugador (p.ej. 'PlayerBullet')")]
    public string playerBulletTag = "PlayerBullet";
    [Tooltip("Capa de proyectil del jugador. Deja -1 para ignorar capa.")]
    public int playerBulletLayer = -1;
    [Tooltip("Daño por defecto si no se puede leer desde el proyectil")]
    public float defaultIncomingDamage = 10f;
    [Tooltip("Destruir el proyectil al impactar en el enemigo")]
    public bool destroyProjectileOnHit = true;

    void Awake()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
            agent.enabled = false; // Evitar error antes de ubicar en NavMesh

        // Asegurar recepción de colisiones/triggers en este GameObject
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Start()
    {
        if (agent != null)
        {
            // Intentar ubicar con un radio más generoso sin Warp (agente deshabilitado)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            else
            {
                // Fallback: raycast hacia abajo y reintento de SamplePosition
                RaycastHit rh;
                Vector3 origin = transform.position + Vector3.up * 5f;
                if (Physics.Raycast(origin, Vector3.down, out rh, 50f))
                {
                    transform.position = rh.point;
                    if (NavMesh.SamplePosition(rh.point, out hit, 10f, NavMesh.AllAreas))
                        transform.position = hit.position;
                }
            }

            // Intentar habilitar solo si realmente hay NavMesh cercano
            TryEnableAgentOnNavMesh();
            if (agent.enabled)
                agent.stoppingDistance = Mathf.Max(0.05f, attackRange * 0.5f);
        }

        lastAttackTime = Time.time - attackCooldown;

        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
                player = found.transform;
            else
                Debug.LogError("No se encontró un objeto con tag 'Player'");
        }

        targetPlayerHealth = player?.GetComponent<PlayerHealth>()
                            ?? player?.GetComponentInChildren<PlayerHealth>()
                            ?? player?.GetComponentInParent<PlayerHealth>()
                            ?? FindObjectOfType<PlayerHealth>();

        if (targetPlayerHealth == null)
            Debug.LogError("No se encontró PlayerHealth en la escena!");
    }

    void Update()
    {
        if (awaitingNavMesh)
        {
            navMeshRetryTimer -= Time.deltaTime;
            if (navMeshRetryTimer <= 0f)
            {
                TryEnableAgentOnNavMesh();
                navMeshRetryTimer = navMeshRetryInterval;
            }
        }

        if (LevelManager.instance != null && !LevelManager.instance.isGameActive) return;
        if (isDead) return;

        if (player == null || targetPlayerHealth == null || targetPlayerHealth.health <= 0)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            if (anim != null) anim.SetFloat("Speed", 0f);
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        bool inAttackRange = distance <= attackRange;

        if (inAttackRange)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;

            if (!isWindingUp && Time.time >= lastAttackTime + attackCooldown)
            {
                if (anim != null)
                {
                    anim.SetFloat("Speed", 0f);

                    // Aquí puedes alternar ataques si quieres variedad:
                    int randomAttack = Random.Range(0, 3);
                    if (randomAttack == 0) anim.SetTrigger("LeftAttack");
                    else if (randomAttack == 1) anim.SetTrigger("RightAttack");
                    else anim.SetTrigger("SplashAttack");
                }

                isWindingUp = true;
                scheduledAttackTime = Time.time + Mathf.Max(0.01f, windUpDuration);
            }

            if (isWindingUp && Time.time >= scheduledAttackTime)
            {
                if (distance <= attackRange + attackLeeway)
                {
                    targetPlayerHealth?.TakeDamage(damage);

                    if (ataqueSound != null && audioSourceAtaque != null)
                        audioSourceAtaque.PlayOneShot(ataqueSound);

                    Debug.Log("Atacando al jugador de Skeleton, daño: " + damage);
                }
                isWindingUp = false;
                lastAttackTime = Time.time;
            }
        }
        else if (distance <= detectionRange)
        {
            if (agent != null && agent.enabled)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                anim?.SetFloat("Speed", agent.velocity.magnitude);
            }
            isWindingUp = false;
        }
        else
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            if (anim != null) anim.SetFloat("Speed", 0f);
            isWindingUp = false;
        }
    }

    private void TryEnableAgentOnNavMesh()
    {
        if (agent == null) { awaitingNavMesh = false; return; }
        if (agent.enabled) { awaitingNavMesh = false; return; }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 1.5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = true;
            agentManuallyEnabled = true;
            awaitingNavMesh = false;
        }
        else
        {
            awaitingNavMesh = true; // Seguir reintentando
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
            Debug.Log("[Skeleton Debug] OnTriggerEnter con '" + other.name + "' (tag=" + other.tag + ", layer=" + other.gameObject.layer + ")", this);
        TryHandleProjectileHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.collider != null)
        {
            var c = collision.collider;
            Debug.Log("[Skeleton Debug] OnCollisionEnter con '" + c.name + "' (tag=" + c.tag + ", layer=" + c.gameObject.layer + ")", this);
        }
        TryHandleProjectileHit(collision.collider);
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other == null) return;
        if (IsPlayerProjectile(other))
        {
            float dmg = ExtractDamageFromObject(other, defaultIncomingDamage);
            Debug.Log("Partícula '" + other.name + "' impactó a Skeleton '" + gameObject.name + "' con daño " + dmg);
            TakeDamage(dmg);
        }
    }

    private void TryHandleProjectileHit(Collider col)
    {
        if (col == null || isDead) return;
        GameObject go = col.gameObject;
        if (!IsPlayerProjectile(go))
        {
            Debug.Log("[Skeleton Debug] Ignorado impacto de '" + go.name + "' (tag=" + go.tag + ", layer=" + go.layer + ") por no cumplir tag/capa/heurística", this);
            return;
        }

        float dmg = ExtractDamageFromObject(go, defaultIncomingDamage);
        Debug.Log("Proyectil '" + go.name + "' impactó a Skeleton '" + gameObject.name + "' con daño " + dmg);
        TakeDamage(dmg);

        if (destroyProjectileOnHit)
            Destroy(go);
    }

    private bool IsPlayerProjectile(GameObject go)
    {
        if (go == null) return false;
        if (!string.IsNullOrEmpty(playerBulletTag) && go.CompareTag(playerBulletTag))
            return true;
        if (playerBulletLayer >= 0 && go.layer == playerBulletLayer)
            return true;

        string n = go.name.ToLowerInvariant();
        if (n.Contains("bullet") || n.Contains("projectile") || n.Contains("shot") || n.Contains("ammo"))
            return true;
        return false;
    }

    private float ExtractDamageFromObject(GameObject go, float fallback)
    {
        if (go == null) return fallback;
        Component[] comps = go.GetComponentsInParent<Component>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var c = comps[i];
            if (c == null) continue;
            float? dmg = TryGetDamageViaReflection(c);
            if (dmg.HasValue) return Mathf.Max(0f, dmg.Value);
        }
        return Mathf.Max(0f, fallback);
    }

    private float? TryGetDamageViaReflection(Component c)
    {
        var t = c.GetType();
        string[] names = { "damage", "Damage", "dmg", "Dmg", "power", "Power", "bulletDamage", "BulletDamage" };
        foreach (string name in names)
        {
            var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanRead && (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(int)))
            {
                object val = prop.GetValue(c, null);
                if (val is float f) return f;
                if (val is int i) return (float)i;
            }
            var field = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null && (field.FieldType == typeof(float) || field.FieldType == typeof(int)))
            {
                object val = field.GetValue(c);
                if (val is float f2) return f2;
                if (val is int i2) return (float)i2;
            }
        }

        var method = t.GetMethod("GetDamage", BindingFlags.Public | BindingFlags.Instance, null, System.Type.EmptyTypes, null);
        if (method != null && (method.ReturnType == typeof(float) || method.ReturnType == typeof(int)))
        {
            object val = method.Invoke(c, null);
            if (val is float f) return f;
            if (val is int i) return (float)i;
        }
        return null;
    }

    public void TakeDamage(float amount = 50f)
    {
        if (isDead) return;

        health -= amount;
        Debug.Log("Impacto en Skeleton '" + gameObject.name + "': daño " + amount + ", salud restante " + Mathf.Max(0f, health));
        anim?.SetTrigger("Damage");

        if (health <= 0f)
            Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (anim != null)
        {
            anim.SetTrigger("Death");
            anim.SetFloat("Speed", 0f);
        }

        if (muerteSound != null && audioSourcemuerte != null)
            audioSourcemuerte.PlayOneShot(muerteSound);

        if (agent != null && agent.enabled) agent.isStopped = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 5f);
    }
}
