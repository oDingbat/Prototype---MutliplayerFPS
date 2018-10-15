using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoPickup : Pickup {

	[Space(10)][Header("Ammo Pickup Settings")]
	public string ammoType;				// The type of ammo this pickup has
	public int ammoAmount;				// The amount of ammo this pickup has

	#region Pickup Methods
	public override bool AttemptAquirePickup(Player player) {
		// Attempt to give this pickup to specified player
		bool pickupAquired = false;

		// See if player has any weapons which take this pickup's specified ammoType AND has space for more ammo
		for (int i = 0; i < player.weapons.Count; i++) {
			Weapon weaponCurrent = player.weapons[i];
			if (weaponCurrent.weaponAttributes.ammoType == ammoType && weaponCurrent.weaponAttributes.ammoCurrent < weaponCurrent.weaponAttributes.ammoMax) {
				player.PickupAmmo(i, ammoAmount);
				pickupAquired = true;
				break;
			}
		}
		
		return pickupAquired;
	}
	#endregion
}
