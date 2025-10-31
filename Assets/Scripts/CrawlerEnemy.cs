using UnityEngine;
using UnityEngine.AI;

public class CrawlerEnemy : MonoBehaviour
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

    void Awake()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
            agent.enabled = false; // Evita error antes de ubicar en NavMesh
    }

    void Start()
    {
        if(agent != null)
        {
            // Intentar ubicar con un radio más generoso y sin Warp (agente aún deshabilitado)
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

        if(targetPlayerHealth == null)
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
            if(agent != null && agent.enabled) agent.isStopped = true;
            if(anim != null) anim.SetBool("Run Forward", false);
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        bool inAttackRange = distance <= attackRange;

        if(inAttackRange)
        {
            if(agent != null && agent.enabled) agent.isStopped = true;

            if(!isWindingUp && Time.time >= lastAttackTime + attackCooldown)
            {
                if(anim != null)
                {
                    anim.SetBool("Run Forward", false);
                    anim.SetTrigger("Gun Shoot Attack");
                }
                isWindingUp = true;
                scheduledAttackTime = Time.time + Mathf.Max(0.01f, windUpDuration);
            }

            if(isWindingUp && Time.time >= scheduledAttackTime)
            {
                if(distance <= attackRange + attackLeeway)
                {
                    targetPlayerHealth?.TakeDamage(damage);

                    if(ataqueSound != null && audioSourceAtaque != null)
                        audioSourceAtaque.PlayOneShot(ataqueSound);

                    Debug.Log("Atacando al jugador, daño: " + damage);
                }
                isWindingUp = false;
                lastAttackTime = Time.time;
            }
        }
        else if(distance <= detectionRange)
        {
            if(agent != null && agent.enabled)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            if(anim != null) anim.SetBool("Run Forward", true);
            isWindingUp = false;
        }
        else
        {
            if(agent != null && agent.enabled) agent.isStopped = true;
            if(anim != null) anim.SetBool("Run Forward", false);
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

    public void TakeDamage(float amount = 50f)
    {
        if(isDead) return;

        health -= amount;
        Debug.Log("Impacto en Crawler '" + gameObject.name + "': daño " + amount + ", salud restante " + Mathf.Max(0f, health));
        anim?.SetTrigger("Take Damage");

        if(health <= 0f)
            Die();
    }

    public void Die()
    {
        if(isDead) return;

        isDead = true;

        if(anim != null)
        {
            anim.SetTrigger("Die");
            anim.SetBool("Run Forward", false);
        }

        if(muerteSound != null && audioSourcemuerte != null)
            audioSourcemuerte.PlayOneShot(muerteSound);

        if(agent != null && agent.enabled) agent.isStopped = true;

        Collider col = GetComponent<Collider>();
        if(col != null) col.enabled = false;

        // ✖ Eliminado ShowMainMenu() aquí. LevelManager controlará el final
        Destroy(gameObject, 5f);
    }
}
