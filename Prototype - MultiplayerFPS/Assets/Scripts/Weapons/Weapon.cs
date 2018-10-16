using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon {

	// Weapon class, responsible for containing information on how a weapon functions

	[Space(10)][Header("Weapon Info")]
	public string name;
	public int prefabPoolIndex;
	public WeaponAttributes weaponAttributes;

	[Space(10)][Header("Projectile Attributes")]
	public ProjectileAttributes projectileAttributes;

	[Space(10)][Header("Audio")]
	public AudioClip clip_Fire;

	[Space(10)][Header("Prefabs")]
	public Transform prefab_Projectile;

	public Weapon (Weapon copiedWeapon) {
		name = copiedWeapon.name;
		prefabPoolIndex = copiedWeapon.prefabPoolIndex;
		weaponAttributes = copiedWeapon.weaponAttributes;

		projectileAttributes = new ProjectileAttributes(copiedWeapon.projectileAttributes);

		clip_Fire = copiedWeapon.clip_Fire;

		prefab_Projectile = copiedWeapon.prefab_Projectile;
	}

	public Weapon () {

	}

}
