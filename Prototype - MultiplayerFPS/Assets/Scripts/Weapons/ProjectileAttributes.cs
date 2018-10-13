using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProjectileAttributes {

	[Space(10)][Header("Damage Attributes")]
	public int damage;					// The amount of damage this projectile does on contact with an Entity
	public float knockback;				// The knockback Vector applied to an Entity's velocity

	[Space(10)][Header("Movement Attributes")]
	public float muzzleVelocity;        // The starting velocity of this projectile when instantiated
	public int ricochetCount;           // The number of ricochets the projectile can perform
	public float ricochetMaxAngle;      // The maximum angle the projectile can ricochet (otherwise it will destroy itself)
	public float gravityScale;          // The amount of gravity applied to this projectile per second (1 = -9.81 units/second)
	public float bouncinessMin;         // The bounciness coefficient; defines how much velocity the projectile retains after each bounce
	public float bouncinessMax;         // The bounciness coefficient; defines how much velocity the projectile retains after each bounce

	[Space(10)][Header("Other Attributes")]
	public float projectileRadius;      // The radius of the projectile
	public float trailRendererTime;		// The time it takes for a trail renderer to dissipate
	public float lifespan = 0.1f;		// The amount of time after the projectile is instantiated before is destroys itself

	public ProjectileAttributes (ProjectileAttributes copiedProjectileAttributes) {
		damage = copiedProjectileAttributes.damage;
		knockback = copiedProjectileAttributes.knockback;
		muzzleVelocity = copiedProjectileAttributes.muzzleVelocity;
		ricochetCount = copiedProjectileAttributes.ricochetCount;
		ricochetMaxAngle = copiedProjectileAttributes.ricochetMaxAngle;
		gravityScale = copiedProjectileAttributes.gravityScale;
		bouncinessMin = copiedProjectileAttributes.bouncinessMin;
		bouncinessMax = copiedProjectileAttributes.bouncinessMax;
		projectileRadius = copiedProjectileAttributes.projectileRadius;
		trailRendererTime = copiedProjectileAttributes.trailRendererTime;
		lifespan = copiedProjectileAttributes.lifespan;
	}

}
