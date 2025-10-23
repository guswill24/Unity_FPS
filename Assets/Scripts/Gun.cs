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

        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out showRaycastHit, shotDistance, shotMask))
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

            //  Si golpea un enemigo
            CrawlerEnemy enemy = showRaycastHit.collider.GetComponentInParent<CrawlerEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(10f);
                return; // 
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
