using UnityEditorInternal.Profiling.Memory.Experimental.FileFormat;
using UnityEngine;


public enum EnemyType
{
    BaseMelee,
    TankMelee
}

public class MeleeEnemy : MonoBehaviour
{
    [Header("Setup")]
    public EnemyType enemyType;
    public Transform player;
    public Rigidbody2D rb;

    [Header("Detection / Attack")]
    public float detectRange = 7f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1f;

    private float attackTimer = 0f;

    [Header("Stats")]
    public float moveSpeed;
    public int maxHealth;
    public int damage;

    private int currentHealth;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        LoadStats(enemyType);
        currentHealth = maxHealth;
    }


    private void Update()
    {
        attackTimer -= Time.deltaTime;

        if (player == null)
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
                moveSpeed = 2.5f;
                maxHealth = 40;
                damage = 10;
                attackCooldown = 1.0f;
                break;

            case EnemyType.TankMelee:
                moveSpeed = 1.2f;
                maxHealth = 120;
                damage = 20;
                attackCooldown = 2.2f;
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

        // Call your animation event or hit detection here
        Debug.Log(enemyType + " attacks for " + damage + " damage.");

        // Example: damaging player
        // player.GetComponent<PlayerHealth>().TakeDamage(damage);
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
