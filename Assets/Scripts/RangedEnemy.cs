using UnityEngine;

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

    private float shootTimer = 0f;

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
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        if (proj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.linearVelocity = firePoint.right * projectileSpeed; 
        }
    }
}
