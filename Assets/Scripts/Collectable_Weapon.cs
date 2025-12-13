using UnityEngine;


//==========================================
//           Collectable Weapon
//==========================================
//what this handles: allowing the player to pick up weapons
//why this is separate: keeps weapon management modular
//what this interacts with: PlayerWeaponController
//why this note is here: for clarity and context


public class Collectable_Weapon : MonoBehaviour
{
    public WeaponData weaponData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerWeaponController weaponController =
            other.GetComponent<PlayerWeaponController>();

        if (weaponController != null)
        {
            weaponController.AddWeapon(weaponData);
            Destroy(gameObject);
        }
    }
}
