using UnityEngine;


public enum EnemyType
{
    BaseMelee,
    TankMelee
}

public class MeleeEnemy : MonoBehaviour
{

    //=========================================
    //           Enemy Configuration
    //           Melee Types
    //           1. Base Melee (dash attack)
    //           2. Tank Melee (AOE slam)
    //=========================================
    //what this handles: enemy that detects player, moves toward them, and attacks based on type
    //why this is separate: different behavior from ranged enemies, modular for different melee types
    //what this interacts with: Player for tracking and attacking

    //if you have any questions, you know how to reach me - Madz767 :)... what do you mean I can't use that name
    //ugh fine, - Todd... fine still have your smile :)

    //setting this up to be used for two different enemy types, base melee and tank melee
    [Header("Setup")]
    public EnemyType enemyType;
    public Transform player;
    public Rigidbody2D rb;

    [Header("Ranges")]
    public float detectRange = 7f;
    public float attackRange = 1.3f;

    [Header("Stats")]
    public float moveSpeed;
    public int maxHealth;
    public int damage;

    [Header("Attack Timing")]
    public float attackCooldown = 1.5f;
    protected float attackTimer;

    [Header("Dash (Base Melee)")]
    public float dashSpeed = 10f;
    public float dashDuration = 0.25f;

    [Header("AOE (Tank Melee)")]
    public float aoeRadius = 2f;
    public float slamDelay = 0.6f;

    private int currentHealth;
    private bool isAttacking;
    private Vector2 dashDirection;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        LoadStats(enemyType);
        currentHealth = maxHealth;

        // Try to find player, and keep trying until it exists
        if (player == null)
            StartCoroutine(EnsurePlayerAssigned());
    }

    private System.Collections.IEnumerator EnsurePlayerAssigned()
    {
        // First try tag-based find, then fall back to finding PlayerController
        while (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
            {
                player = pObj.transform;
                yield break;
            }

            var pc = FindFirstObjectByType<PlayerController>();
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
        attackTimer -= Time.deltaTime;

        if (player == null || isAttacking)
            return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= detectRange)
        {
            RotateTowardPlayer();

            if (distance > attackRange)
            {
                MoveTowardPlayer();
            }
            else
            {
                TryAttack();
            }
        }
    }


    private void LoadStats(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.BaseMelee:
                moveSpeed = 3f;
                maxHealth = 40;
                damage = 10;
                attackCooldown = 1.2f;
                dashSpeed = 12f;
                dashDuration = 0.25f;
                break;

            case EnemyType.TankMelee:
                moveSpeed = 1.3f;
                maxHealth = 140;
                damage = 20;
                attackCooldown = 2.5f;
                aoeRadius = 2.2f;
                slamDelay = 0.7f;
                break;
        }
    }


    private void MoveTowardPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.deltaTime);
    }

    private void RotateTowardPlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }




    private void TryAttack()
    {
        if (attackTimer > 0f)
            return;

        attackTimer = attackCooldown;

        if (enemyType == EnemyType.BaseMelee)
            StartCoroutine(DashAttack());
        else
            StartCoroutine(AOESlam());


    }

    private System.Collections.IEnumerator DashAttack()
    {
        isAttacking = true;
        dashDirection = (player.position - transform.position).normalized;

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        // Damage check
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            if (player == null)
            {
                Debug.LogError("MeleeEnemy: Player Transform reference is null!");
                isAttacking = false;
                yield break;
            }

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                // Try getting from gameObject
                playerHealth = player.gameObject.GetComponent<PlayerHealth>();
            }
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }

        isAttacking = false;
    }

    private System.Collections.IEnumerator AOESlam()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        // Wind-up delay
        yield return new WaitForSeconds(slamDelay);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                    // Try getting from the GameObject directly
                    playerHealth = hit.gameObject.GetComponent<PlayerHealth>();
                }
                
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }
        }

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

        //this draws the attack range for the AOE the tank uses
        //this will only show up in the editor when the tank melee is selected
        //this will help us with tweeking the radius to what fits best
        if (enemyType == EnemyType.TankMelee)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aoeRadius);
        }
    }
}
