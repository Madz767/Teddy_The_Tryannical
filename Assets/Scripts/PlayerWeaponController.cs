using UnityEngine;
using System.Collections.Generic;


//==========================================
//           Player Weapon Controller
//==========================================
//what this handles: managing player's weapons, equipping, cycling
//why this is separate: keeps player controller cleaner, allows for more modularity
//what this interacts with: Collectable_Weapon, PlayerAttackController
//why this note is here: as a guide to the weary traveler


public class PlayerWeaponController : MonoBehaviour
{
    public List<WeaponData> ownedWeapons = new();
    public WeaponData currentWeapon;

    private int currentWeaponIndex;

    private void Start()
    {
        if (ownedWeapons.Count > 0)
        {
            EquipWeapon(0);
        }
        else
        {
            Debug.LogWarning("PlayerWeaponController: No weapons in ownedWeapons list! Add at least one weapon in the Inspector.");
        }
    }

    public void AddWeapon(WeaponData newWeapon)
    {
        if (ownedWeapons.Contains(newWeapon))
            return;

        ownedWeapons.Add(newWeapon);
        EquipWeapon(ownedWeapons.Count - 1);
    }

    private void Update()
    {
        if (ownedWeapons.Count <= 1)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
            CycleWeapon();
    }

    private void CycleWeapon()
    {
        currentWeaponIndex = (currentWeaponIndex + 1) % ownedWeapons.Count;
        EquipWeapon(currentWeaponIndex);
    }

    private void EquipWeapon(int index)
    {
        currentWeapon = ownedWeapons[index];
    }


}
