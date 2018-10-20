using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProjectileAttributes {

	[Space(10)][Header("Damage Attributes")]
	public int damage;					// The amount of damage this projectile does on contact with an Entity
	public float knockback;				// The knockback Vector applied to an Entity's velocity
	
	[Space(10)][Header("Explosion Attributes")]
	public bool isExplosive;			// Does this projectile explode on destruction?
	public int explosionDamage;         // Explosive maximum damage
	public float explosionBlastRadius;  // The blast radius of this projectile's explosion
	public float explosionKnockback;	// The maximum knockback the explosion can give to an entity

	[Space(10)][Header("Movement Attributes")]
	public float muzzleVelocity;        // The starting velocity of this projectile when instantiated
	public int ricochetCount;           // The number of ricochets the projectile can perform
	public float ricochetMaxAngle;      // The maximum angle the projectile can ricochet (otherwise it will destroy itself)
	public float gravityScale;          // The amount of gravity applied to this projectile per second (1 = -9.81 units/second)
	public float bouncinessMin;         // The bounciness coefficient; defines how much velocity the projectile retains after each bounce
	public float bouncinessMax;         // The bounciness coefficient; defines how much velocity the projectile retains after each bounce
	public bool entityPenetration;      // Can this projectile penetrate through an entity?

	[Space(10)][Header("Sticky Attributes")]
	public bool isSticky;               // Is this projectile sticky? Ie: does it stick to surfaces on contact?
	public float stickyLifespan;        // The amount of time the projectile will stick to it's target before destroying

	[Space(10)][Header("Detonation Attributes")]
	public bool hasDelayedCollisionDetonation;		// Does this collision detonate after it's first collision AFTER delayedCollisionDetonationTime has elapsed?			// TODO: Maybe enabled this functionality only after all ricochets have been used?
	public float delayedCollisionDetonationTime;

	[Space(10)][Header("Other Attributes")]
	public float projectileRadius;      // The radius of the projectile
	public float trailRendererTime;		// The time it takes for a trail renderer to dissipate
	public float lifespan = 0.1f;		// The amount of time after the projectile is instantiated before is destroys itself

	[Space(10)][Header("Prefabs")]
	public GameObject prefab_Explosion;	// The explosion prefab which is instantiated once this projectile explosed (if isExplosive is enabled)

	[Space(10)][Header("Audio")]
	public AudioClip clip_Collision;
	public AudioClip clip_Explosion;

	public ProjectileAttributes (ProjectileAttributes copiedProjectileAttributes) {
		damage = copiedProjectileAttributes.damage;
		knockback = copiedProjectileAttributes.knockback;

		isExplosive = copiedProjectileAttributes.isExplosive;
		explosionDamage = copiedProjectileAttributes.explosionDamage;
		explosionBlastRadius = copiedProjectileAttributes.explosionBlastRadius;
		explosionKnockback = copiedProjectileAttributes.explosionKnockback;

		muzzleVelocity = copiedProjectileAttributes.muzzleVelocity;
		ricochetCount = copiedProjectileAttributes.ricochetCount;
		ricochetMaxAngle = copiedProjectileAttributes.ricochetMaxAngle;
		gravityScale = copiedProjectileAttributes.gravityScale;
		bouncinessMin = copiedProjectileAttributes.bouncinessMin;
		bouncinessMax = copiedProjectileAttributes.bouncinessMax;
		entityPenetration = copiedProjectileAttributes.entityPenetration;
		isSticky = copiedProjectileAttributes.isSticky;
		stickyLifespan = copiedProjectileAttributes.stickyLifespan;

		hasDelayedCollisionDetonation = copiedProjectileAttributes.hasDelayedCollisionDetonation;
		delayedCollisionDetonationTime = copiedProjectileAttributes.delayedCollisionDetonationTime;

		projectileRadius = copiedProjectileAttributes.projectileRadius;
		trailRendererTime = copiedProjectileAttributes.trailRendererTime;
		lifespan = copiedProjectileAttributes.lifespan;

		prefab_Explosion = copiedProjectileAttributes.prefab_Explosion;

		clip_Collision = copiedProjectileAttributes.clip_Collision;
		clip_Explosion = copiedProjectileAttributes.clip_Explosion;
	}

}
