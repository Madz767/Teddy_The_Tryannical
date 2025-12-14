using System;
using System.Collections;
using UnityEngine;

//==========================================
//           TutorialBoss.cs
//==========================================
//what this handles:
// - Boss AI used in tutorial: chases player, chooses between two attacks (melee AOE slam and ranged projectile volley).
// - Attack windups, cooldowns, and basic health/damage handling.
//why this is separate:
// - Encapsulates boss-specific behavior and tuning separate from generic enemy classes to keep fight logic isolated and editable.
//what this interacts with:
// - Player (found by tag or PlayerController reference).
// - Rigidbody2D (movement).
// - Projectile prefab and firePoint (for ranged volley).
// - ProjectileController (to set projectile damage/velocity).
// - PlayerHealth (to apply damage).
// - Optional scene systems (spawner/player locator) for robust player resolution.

[RequireComponent(typeof(Rigidbody2D))]
public class TutorialBoss : MonoBehaviour
{
    // Simple boss with two moves:
    // 1) Melee Slam — short wind-up, AOE damage around boss.
    // 2) Ranged Volley — fires a burst of projectiles in the player's direction.

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Sensing / Movement")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("Melee Slam")]
    [SerializeField] private float meleeRange = 1.8f;
    [SerializeField] private float meleeWindup = 0.5f;
    [SerializeField] private float meleeCooldown = 2.0f;
    [SerializeField] private float meleeAOERadius = 2.0f;
    [SerializeField] private int meleeDamage = 20;

    [Header("Ranged Volley")]
    [SerializeField] private float rangedRange = 10f;
    [SerializeField] private float volleyCooldown = 3.0f;
    [SerializeField] private int volleyCount = 3;
    [SerializeField] private float volleyDelayBetweenShots = 0.12f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int projectileDamage = 8;
    [SerializeField, Range(0f, 90f)] private float volleySpreadAngle = 12f;

    private float meleeTimer;
    private float rangedTimer;
    private bool isAttacking;
    private int currentHealth = 300;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // ensure firePoint exists if projectilePrefab provided
        if (projectilePrefab != null && firePoint == null)
        {
            firePoint = transform;
        }

        // If player not assigned at Awake, keep trying in Start
    }

    private void Start()
    {
        if (player == null)
            StartCoroutine(EnsurePlayerAssigned());
    }

    private System.Collections.IEnumerator EnsurePlayerAssigned()
    {
        while (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
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

    private void Update()
    {
        meleeTimer -= Time.deltaTime;
        rangedTimer -= Time.deltaTime;

        if (player == null || isAttacking) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > detectionRange)
            return;

        RotateTowardPlayer();

        // Prioritize melee if in range and ready, otherwise use ranged if in range and ready
        if (distance <= meleeRange && meleeTimer <= 0f)
        {
            StartCoroutine(MeleeSlamRoutine());
        }
        else if (distance <= rangedRange && rangedTimer <= 0f)
        {
            StartCoroutine(RangedVolleyRoutine());
        }
        else
        {
            // Chase the player
            MoveTowardPlayer();
        }
    }

    private void MoveTowardPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * chaseSpeed * Time.deltaTime);
    }

    private void RotateTowardPlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private IEnumerator MeleeSlamRoutine()
    {
        isAttacking = true;
        meleeTimer = meleeCooldown;

        // Wind-up
        yield return new WaitForSeconds(meleeWindup);

        // Damage all players in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, meleeAOERadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                var ph = hit.GetComponent<PlayerHealth>() ?? hit.gameObject.GetComponent<PlayerHealth>();
                ph?.TakeDamage(meleeDamage);
            }
        }

        // short recovery
        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    private IEnumerator RangedVolleyRoutine()
    {
        isAttacking = true;
        rangedTimer = volleyCooldown;

        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("TutorialBoss: projectilePrefab or firePoint not assigned. Skipping ranged attack.");
            isAttacking = false;
            yield break;
        }

        Vector2 toPlayer = (player.position - firePoint.position).normalized;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        for (int i = 0; i < volleyCount; i++)
        {
            float offset = 0f;
            if (volleyCount > 1)
            {
                float step = volleySpreadAngle / Mathf.Max(1, volleyCount - 1);
                offset = -volleySpreadAngle * 0.5f + step * i;
            }

            float angle = baseAngle + offset;
            Quaternion rot = Quaternion.Euler(0, 0, angle);
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, rot);

            if (proj.TryGetComponent<Rigidbody2D>(out var prb))
            {
                Vector2 vel = rot * Vector2.right * projectileSpeed;
                prb.linearVelocity = vel;
            }

            var pctrl = proj.GetComponent<ProjectileController>();
            if (pctrl != null)
            {
                pctrl.damage = projectileDamage;
            }

            yield return new WaitForSeconds(volleyDelayBetweenShots);
        }

        // short recovery after volley
        yield return new WaitForSeconds(0.25f);
        isAttacking = false;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAOERadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangedRange);
    }
}
