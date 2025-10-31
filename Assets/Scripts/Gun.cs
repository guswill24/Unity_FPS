using UnityEngine;

public class Gun : MonoBehaviour
{
    public Transform playerCamera;
    public float shotDistance = 10f;
    public float impactForce = 5f;
    public LayerMask shotMask;
    public GameObject destroyEffect;
    public ParticleSystem shootParticles;
    public GameObject hitEffect;

    [Header("Audio")]
    public AudioClip hitSound;
    public AudioSource audioSource;

    [Header("Daño")]
    public float damage = 10f;

    private RaycastHit showRaycastHit;

    void Update()
    {
        if (LevelManager.instance != null && !LevelManager.instance.isGameActive) return;
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (shootParticles != null)
            shootParticles.Play();

        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out showRaycastHit, shotDistance, shotMask, QueryTriggerInteraction.Collide))
        {
            //Debug.Log("Shot hit: " + showRaycastHit.collider.name);

            //  Efecto de impacto visual
            if (hitEffect != null)
                Instantiate(hitEffect, showRaycastHit.point, Quaternion.LookRotation(showRaycastHit.normal));

            //  Si tiene rigidbody, aplicar fuerza
            Rigidbody rb = showRaycastHit.collider.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(-showRaycastHit.normal * impactForce, ForceMode.Impulse);

            //  Reproducir sonido de impacto
            if (hitSound != null && audioSource != null)
                audioSource.PlayOneShot(hitSound);

            //  Si golpea un Hitbox (prioritario)
            var hb = showRaycastHit.collider.GetComponent<Hitbox>();
            if (hb != null)
            {
                hb.ApplyDamage(damage);
                Debug.Log("Disparo impactó Hitbox en '" + hb.gameObject.name + "' (dueño='" + (hb.owner != null ? hb.owner.name : "null") + "') con daño " + damage);
                return;
            }

            //  Si golpea un enemigo (genérico)
            var ce = showRaycastHit.collider.GetComponentInParent<CrawlerEnemy>();
            if (ce != null) { ce.TakeDamage(damage); Debug.Log("Disparo impactó Crawler '" + ce.name + "' con daño " + damage); return; }
            var ge = showRaycastHit.collider.GetComponentInParent<GolemEnemy>();
            if (ge != null) { ge.TakeDamage(damage); Debug.Log("Disparo impactó Golem '" + ge.name + "' con daño " + damage); return; }
            var se = showRaycastHit.collider.GetComponentInParent<SkeletonEnemy>();
            if (se != null) { se.TakeDamage(damage); Debug.Log("Disparo impactó Skeleton '" + se.name + "' con daño " + damage); return; }
            var ez = showRaycastHit.collider.GetComponentInParent<EnemyZombi>();
            if (ez != null) { ez.TakeDamage(damage); Debug.Log("Disparo impactó EnemyZombi '" + ez.name + "' con daño " + damage); return; }

            //  Fallback por reflexión: busca método TakeDamage(float)
            var mb = showRaycastHit.collider.GetComponentInParent<MonoBehaviour>();
            if (mb != null)
            {
                var t = mb.GetType();
                var m = t.GetMethod("TakeDamage", new System.Type[] { typeof(float) });
                if (m != null)
                {
                    m.Invoke(mb, new object[] { damage });
                    Debug.Log("Disparo impactó '" + mb.name + "' (" + t.Name + ") con daño " + damage + " via reflexión");
                    return;
                }
            }

            //  Si golpea un barril
            if (showRaycastHit.collider.CompareTag("Barrel"))
            {
                if (LevelManager.instance != null)
                    LevelManager.instance.levelScore++;

                if (destroyEffect != null)
                    Instantiate(destroyEffect, showRaycastHit.point, Quaternion.LookRotation(showRaycastHit.normal));

                Destroy(showRaycastHit.collider.gameObject);
            }
        }
    }
}
