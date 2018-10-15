using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackPickup : Pickup {

	[Space(10)][Header("Stack Pickup Settings")]
	public int health = 25;                 // The amount of health this pickup gives players
	public int armor = 0;                   // The amount of armor this pickup gives players
	public int overhealPotential;           // The maximum amount of overheal this item can provide
	public int overarmorPotential;          // The maximum amount of overarmor this item can provide

	#region Pickup Methods
	public override bool AttemptAquirePickup (Player player) {
		// Attempt to give this pickup to specified player
		bool pickupAquired = false;

		// Heal player's health (if this is a health pickup)
		if (health > 0 && player.vitals.healthCurrent < (player.vitals.healthMaximum + overhealPotential)) {
			player.HealHealth(health, overhealPotential);
			pickupAquired = true;
		}

		// Heal player's armor (if this is an armor pickup)
		if (armor > 0 && player.vitals.armorCurrent < (player.vitals.armorMaximum + overarmorPotential)) {
			player.HealArmor(armor, overarmorPotential);
			pickupAquired = true;
		}

		return pickupAquired;
	}
	#endregion
}
