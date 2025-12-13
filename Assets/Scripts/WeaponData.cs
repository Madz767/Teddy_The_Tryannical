using UnityEngine;


//==========================================
//           Weapon Data
//==========================================
//what this handles: storing weapon stats and info
//why this is separate: allows easy creation and management of different weapons
//what this interacts with: PlayerWeaponController, PlayerAttackController, Collectable_Weapon, 
//and by proxy the PlayerController and Enemies
//why this note is here: to help anyone reading this to understand what is happening here



[CreateAssetMenu(menuName = "Weapons/Melee Weapon")]
public class WeaponData : MonoBehaviour
{
    public string weaponID;
    public string weaponName;

    [Header("Basic Attack")]
    public int basicDamage;
    public float basicCooldown;
    public float basicRange;

    [Header("Special Attack")]
    public int specialDamage;
    public float specialCooldown;
    public float specialRange;

    public Sprite icon;
}
