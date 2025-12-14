using UnityEngine;


//==========================================
//           Player Attack Controller         
//==========================================

//what this handles: basic and special attacks, damage application
//why this is separate: keeps player controller cleaner, allows for more modularity
//what this interacts with: PlayerWeaponController, PlayerController, MeleeEnemy, RangedEnemy
//why this note is here: so you don't get lost in the sauce
//plus, why not it's fun to write notes



[RequireComponent(typeof(PlayerWeaponController))]
public class PlayerAttackController : MonoBehaviour
{
    [Header("Attack Origin")]
    public Transform attackPoint;
    public LayerMask enemyLayers;

    private PlayerWeaponController weaponController;

    private void Awake()
    {
        weaponController = GetComponent<PlayerWeaponController>();
    }

    public void BasicAttack(Vector2 direction)
    {
        if (weaponController.currentWeapon == null)
            return;

        var weapon = weaponController.currentWeapon;

        PerformAttack(
            direction,
            weapon.basicDamage,
            weapon.basicRange
        );
    }

    public void SpecialAttack(Vector2 direction)
    {
        if (weaponController.currentWeapon == null)
            return;

        var weapon = weaponController.currentWeapon;

        PerformAttack(
            direction,
            weapon.specialDamage,
            weapon.specialRange
        );
    }

    private void PerformAttack(Vector2 direction, int damage, float range)
    {
        Vector2 origin = attackPoint != null
            ? attackPoint.position
            : transform.position;

        Vector2 attackCenter = origin + direction * range * 0.5f;

        // Simple circle hit (can be replaced later)
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackCenter,
            range,
            enemyLayers
        );

        foreach (Collider2D hit in hits)
        {
            MeleeEnemy meleeEnemy = hit.GetComponent<MeleeEnemy>();
            RangedEnemy rangedEnemy = hit.GetComponent<RangedEnemy>();

            if (meleeEnemy != null)
            {
                meleeEnemy.TakeDamage(damage);
            }
            
            if (rangedEnemy != null)
            {
                rangedEnemy.TakeDamage(damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 origin = attackPoint != null
            ? attackPoint.position
            : transform.position;

        //MORE GIZMOS MY FAVORITE NEW THING WOOOOOOOOOOOOO

        // Draw attack point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, 0.1f);

        // Draw basic attack range if weapon is equipped
        if (weaponController != null && weaponController.currentWeapon != null && weaponController.currentWeapon.basicRange > 0)
        {
            Vector2 direction = Vector2.right; // Default direction for editor visualization
            Vector2 attackCenter = origin + direction * weaponController.currentWeapon.basicRange * 0.5f;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackCenter, weaponController.currentWeapon.basicRange);
            
            // Draw direction line
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, attackCenter);
        }
    }
}

