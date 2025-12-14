using UnityEngine;


//==========================================
//           Ranged Enemy
//==========================================
//what this handles: enemy that detects player, rotates to face them, and shoots projectiles
//why this is separate: because it was easier to manage separately than with melee enemies
//what this interacts with: Player for tracking, Projectile for shooting




public class RangedEnemy : MonoBehaviour
{
    [Header("Targeting")]
    public Transform player;
    public float detectionRange = 10f;
    public float rotationSpeed = 5f;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float shootCooldown = 1.5f;
    public float projectileSpeed = 12f;

    [Header("Stats")]
    public int maxHealth = 30;
    public int damage = 8;

    private float shootTimer = 0f;
    private int currentHealth;

    private void Awake()
    {
        // ensure references that should always exist are set
        if (firePoint == null)
            firePoint = transform;
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (player == null)
            StartCoroutine(EnsurePlayerAssigned());
    }

    private System.Collections.IEnumerator EnsurePlayerAssigned()
    {
        while (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
            {
                player = pObj.transform;
                yield break;
            }

            var pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                player = pc.transform;
                yield break;
            }

            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= detectionRange)
        {
            RotateTowardsPlayer();
            HandleShooting();
        }
    }
    void RotateTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
            );
    }
    void HandleShooting()
    {
        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0f)
        {
            Shoot();
            shootTimer = shootCooldown;
        }
    }
    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        if (proj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.linearVelocity = firePoint.right * projectileSpeed; 
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
