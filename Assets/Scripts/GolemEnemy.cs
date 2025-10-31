using UnityEngine;
using UnityEngine.AI;
using System.Reflection;

public class GolemEnemy : MonoBehaviour
{
    [Header("Detección del jugador")]
    public Transform player;
    public float detectionRange = 10f;
    public float attackRange = 2.5f;

    [Header("Estadísticas del Golem")]
    public float health = 150f;
    public float damage = 20f;
    public float attackCooldown = 2f;

    private Animator anim;
    private NavMeshAgent agent;
    private bool isDead = false;
    private float lastAttackTime = 0f;

    private PlayerHealth targetPlayerHealth;

    [Header("Ataque")]
    public float windUpDuration = 0.3f;
    private bool isWindingUp = false;
    private float scheduledAttackTime = 0f;

    [Header("Audio")]
    public AudioClip attackSound;
    public AudioSource audioAttack;
    public AudioClip deathSound;
    public AudioSource audioDeath;

    [Header("Terreno y navegación")]
    public LayerMask groundMask = ~0;
    public float groundRaycastHeight = 5f;
    public float groundRaycastDistance = 50f;
    public bool snapToNavMeshOnStart = true;

    [Header("Alineación visual")]
    public Transform visualRoot; // Asigna aquí el objeto del modelo/mesh
    public bool autoCalibrateBaseOffset = true;

    [Header("Daño entrante del jugador")]
    [Tooltip("Tag que llevan los proyectiles del jugador (p.ej. 'PlayerBullet')")]
    public string playerBulletTag = "PlayerBullet";
    [Tooltip("Capa de proyectil del jugador. Deja -1 para ignorar capa.")]
    public int playerBulletLayer = -1;
    [Tooltip("Daño por defecto si no se puede leer desde el proyectil")]
    public float defaultIncomingDamage = 10f;
    [Tooltip("Destruir el proyectil al impactar en el Golem")]
    public bool destroyProjectileOnHit = true;

    private bool agentManuallyEnabled = false;
    private bool awaitingNavMesh = true;
    private float navMeshRetryTimer = 0f;
    public float navMeshRetryInterval = 0.5f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.enabled = false; // Evita el error antes de ubicarnos en el NavMesh
        }

        // Asegurar recepción de colisiones/triggers en este GameObject
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
                player = found.transform;
        }

        if (player != null)
        {
            targetPlayerHealth = player.GetComponent<PlayerHealth>()
                ?? player.GetComponentInChildren<PlayerHealth>()
                ?? player.GetComponentInParent<PlayerHealth>();
        }

        lastAttackTime = Time.time - attackCooldown;

        if (snapToNavMeshOnStart)
            SnapToGroundOrNavMesh();

        // Habilitar solo si realmente hay NavMesh cerca
        TryEnableAgentOnNavMesh();

        if (autoCalibrateBaseOffset)
            CalibrateBaseOffsetFromRenderers();
    }

    void Update()
    {
        // Si aún no hay NavMesh disponible, reintenta periódicamente habilitar el agente
        if (awaitingNavMesh)
        {
            navMeshRetryTimer -= Time.deltaTime;
            if (navMeshRetryTimer <= 0f)
            {
                TryEnableAgentOnNavMesh();
                navMeshRetryTimer = navMeshRetryInterval;
            }
        }

        if (isDead || player == null || targetPlayerHealth == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        bool inAttackRange = distance <= attackRange;

        // --- Muerte del jugador ---
        if (targetPlayerHealth.health <= 0)
        {
            agent.isStopped = true;
            anim.SetBool("isWalking", false);
            anim.SetBool("isIdle", true);
            return;
        }

        // --- Ataque ---
        if (inAttackRange)
        {
            agent.isStopped = true;
            transform.LookAt(player);

            anim.SetBool("isWalking", false);
            anim.SetBool("isIdle", false);

            if (!isWindingUp && Time.time >= lastAttackTime + attackCooldown)
            {
                anim.SetTrigger("Attack");
                isWindingUp = true;
                scheduledAttackTime = Time.time + windUpDuration;
            }

            if (isWindingUp && Time.time >= scheduledAttackTime)
            {
                isWindingUp = false;
                lastAttackTime = Time.time;

                // Aplicar daño si sigue cerca
                if (distance <= attackRange)
                {
                    targetPlayerHealth.TakeDamage(damage);

                    if (audioAttack != null && attackSound != null)
                        audioAttack.PlayOneShot(attackSound);

                    Debug.Log("Golem ataca e inflige " + damage + " de daño.");
                }
            }
        }
        // --- Persecución ---
        else if (distance <= detectionRange)
        {
            if (agent.isStopped) agent.isStopped = false;
            agent.SetDestination(player.position);

            anim.SetBool("isWalking", true);
            anim.SetBool("isIdle", false);
            isWindingUp = false;
        }
        // --- Idle ---
        else
        {
            agent.isStopped = true;
            anim.SetBool("isWalking", false);
            anim.SetBool("isIdle", true);
            isWindingUp = false;
        }
    }

    // --- Daño recibido ---
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        Debug.Log("Impacto en Golem '" + gameObject.name + "': daño " + amount + ", salud restante " + Mathf.Max(0f, health));

        if (health <= 0)
            Die();
    }

    // --- Muerte del Golem ---
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        anim.SetTrigger("Die");
        anim.SetBool("isWalking", false);
        anim.SetBool("isIdle", false);

        if (audioDeath != null && deathSound != null)
            audioDeath.PlayOneShot(deathSound);

        if (agent != null && agent.enabled) agent.isStopped = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 5f);
    }

    private void SnapToGroundOrNavMesh()
    {
        Vector3 startPos = transform.position;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(startPos, out navHit, 10f, NavMesh.AllAreas))
        {
            // Coloca primero el transform; habilitaremos el agente luego
            transform.position = navHit.position;
            return;
        }

        RaycastHit hit;
        Vector3 rayOrigin = startPos + Vector3.up * groundRaycastHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundRaycastDistance, groundMask))
        {
            Vector3 pos = hit.point;
            transform.position = pos;

            // Reintenta ajustar a NavMesh cerca del punto de suelo
            if (NavMesh.SamplePosition(pos, out navHit, 10f, NavMesh.AllAreas))
                transform.position = navHit.position;
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

    private void CalibrateBaseOffsetFromRenderers()
    {
        if (agent == null) return;

        Transform target = visualRoot != null ? visualRoot : transform;
        var renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combined.Encapsulate(renderers[i].bounds);

        float feetToPivot = transform.position.y - combined.min.y;
        if (feetToPivot < 0f) feetToPivot = 0f;

        agent.baseOffset = feetToPivot;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleProjectileHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHandleProjectileHit(collision.collider);
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other == null) return;
        if (IsPlayerProjectile(other))
        {
            float dmg = ExtractDamageFromObject(other, defaultIncomingDamage);
            Debug.Log("Partícula '" + other.name + "' impactó a Golem '" + gameObject.name + "' con daño " + dmg);
            TakeDamage(dmg);
        }
    }

    private void TryHandleProjectileHit(Collider col)
    {
        if (col == null) return;
        GameObject go = col.gameObject;
        if (!IsPlayerProjectile(go)) return;

        float dmg = ExtractDamageFromObject(go, defaultIncomingDamage);
        Debug.Log("Proyectil '" + go.name + "' impactó a Golem '" + gameObject.name + "' con daño " + dmg);
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

        // Heurística de respaldo por nombre
        string n = go.name.ToLowerInvariant();
        if (n.Contains("bullet") || n.Contains("projectile") || n.Contains("shot") || n.Contains("ammo"))
            return true;

        return false;
    }

    private float ExtractDamageFromObject(GameObject go, float fallback)
    {
        if (go == null) return fallback;

        // Revisa componentes en el objeto y sus padres cercanos
        Component[] comps = go.GetComponentsInParent<Component>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var c = comps[i];
            if (c == null) continue;
            float? dmg = TryGetDamageViaReflection(c);
            if (dmg.HasValue)
                return Mathf.Max(0f, dmg.Value);
        }
        return Mathf.Max(0f, fallback);
    }

    private float? TryGetDamageViaReflection(Component c)
    {
        var t = c.GetType();
        // Propiedades comunes
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

        // Método GetDamage()
        var method = t.GetMethod("GetDamage", BindingFlags.Public | BindingFlags.Instance, null, System.Type.EmptyTypes, null);
        if (method != null && (method.ReturnType == typeof(float) || method.ReturnType == typeof(int)))
        {
            object val = method.Invoke(c, null);
            if (val is float f) return f;
            if (val is int i) return (float)i;
        }
        return null;
    }
}
