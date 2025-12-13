using UnityEngine;


//==========================================
//           Player Attack Controller         
//==========================================

//what this handles: basic and special attacks, damage application
//why this is separate: keeps player controller cleaner, allows for more modularity
//what this interacts with: PlayerWeaponController, PlayerController, MeleeEnemy
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

        // Simple circle hit (can be replaced later)
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            origin + direction * range * 0.5f,
            range,
            enemyLayers
        );

        foreach (Collider2D hit in hits)
        {
            hit.GetComponent<MeleeEnemy>()?.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        //MORE GIZMOS MY FAVORITE NEW THING WOOOOOOOOOOOOO

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, 0.5f);
    }
}

