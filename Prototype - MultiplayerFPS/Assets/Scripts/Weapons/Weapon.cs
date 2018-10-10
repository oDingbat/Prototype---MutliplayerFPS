using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon {

	// Weapon class, responsible for containing detailed information on how a weapon functions

	[Space(10)][Header("Weapon Info")]
	public string name;
	public WeaponAttributes attributes;
	public int damage;

	[Space(10)][Header("Projectile Attributes")]
	public ProjectileAttributes projectileAttributes;

	[Space(10)][Header("Audio")]
	public AudioClip clip_Fire;

	[Space(10)][Header("Prefabs")]
	public Transform prefab_Projectile;

	public Weapon (Weapon copiedWeapon) {
		projectileAttributes = new ProjectileAttributes(copiedWeapon.projectileAttributes);
		prefab_Projectile = copiedWeapon.prefab_Projectile;
	}

}
