using UnityEngine;
using UnityEngine.AI;

public class EnemyZombi : MonoBehaviour
{
    [Header("Objetivos y combate")]
    public Transform player;
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public float damage = 10f;

    [Header("Salud")]
    public float health = 100f;

    private Animator anim;
    private NavMeshAgent agent;
    private PlayerHealth targetPlayerHealth;
    private bool isDead = false;
    private float lastAttackTime = 0f;

    [Header("Depuración de animación")]
    public bool debugAnimation = false;
    public float fallbackMoveSpeed = 1.2f; // velocidad simulada si no hay agente
    public float animMinMoveSpeedParam = 1f; // mínimo para activar Walk/Run InPlace

    void Awake()
    {
        anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        if (anim != null) anim.applyRootMotion = false;
    }

    void Start()
    {
        // Intentar ubicar en NavMesh y habilitar agente
        NavMeshHit hit;
        if (agent != null)
        {
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            else
            {
                // Fallback: raycast al suelo y reintento
                RaycastHit rh;
                Vector3 origin = transform.position + Vector3.up * 5f;
                if (Physics.Raycast(origin, Vector3.down, out rh, 50f))
                {
                    transform.position = rh.point;
                    if (NavMesh.SamplePosition(rh.point, out hit, 10f, NavMesh.AllAreas))
                        transform.position = hit.position;
                }
            }

            if (!agent.enabled) agent.enabled = true;
            // Defaults razonables por si el prefab trae valores bajos
            if (agent.speed < 0.1f) agent.speed = 3.5f;
            if (agent.acceleration < 0.1f) agent.acceleration = 8f;
            if (agent.angularSpeed < 0.1f) agent.angularSpeed = 120f;
            agent.stoppingDistance = Mathf.Max(0.05f, attackRange * 0.5f);
        }

        // Encontrar jugador y su salud
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        targetPlayerHealth = player?.GetComponent<PlayerHealth>()
                             ?? player?.GetComponentInChildren<PlayerHealth>()
                             ?? player?.GetComponentInParent<PlayerHealth>();
    }

    void Update()
    {
        if (isDead || player == null || targetPlayerHealth == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        bool inDetection = distance <= detectionRange;
        bool inAttack = distance <= attackRange;

        // Calcular Speed de forma robusta (usa desiredVelocity si velocity está 0)
        float speed = 0f;
        if (agent != null && agent.enabled)
        {
            if (!agent.isStopped)
            {
                float v = agent.velocity.magnitude;
                float dv = agent.desiredVelocity.magnitude;
                speed = v > 0.05f ? v : dv;
                // Garantiza "in place" si persigue pero aún acelera
                if (inDetection && !inAttack)
                    speed = Mathf.Max(speed, animMinMoveSpeedParam);
            }
        }
        else
        {
            // Sin agente: simula velocidad cuando persigue
            if (inDetection && !inAttack) speed = fallbackMoveSpeed;
        }
        anim?.SetFloat("Speed", speed);
        if (debugAnimation) Debug.Log($"[EnemyZombi] Speed={speed:0.00}, inDetection={inDetection}, inAttack={inAttack}", this);

        if (inAttack)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            if (anim != null)
            {
                anim.SetBool("IsAttacking", true);
                if (debugAnimation) Debug.Log("[EnemyZombi] IsAttacking=true", this);
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                targetPlayerHealth.TakeDamage(damage);
                lastAttackTime = Time.time;
            }
        }
        else if (inDetection)
        {
            if (agent != null && agent.enabled)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            if (anim != null)
            {
                anim.SetBool("IsAttacking", false);
                if (debugAnimation) Debug.Log("[EnemyZombi] IsAttacking=false (persiguiendo)", this);
            }
        }
        else
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            if (anim != null)
            {
                anim.SetBool("IsAttacking", false);
                if (debugAnimation) Debug.Log("[EnemyZombi] IsAttacking=false (idle)", this);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        health -= amount;
        anim?.SetTrigger("Hit");
        if (debugAnimation) Debug.Log($"[EnemyZombi] Hit trigger, health={health}", this);
        if (health <= 0f) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        anim?.SetTrigger("Die");
        if (debugAnimation) Debug.Log("[EnemyZombi] Die trigger", this);
        if (agent != null && agent.enabled) agent.isStopped = true;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 5f);
    }
}
