using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(SphereCollider))]
public class Projectile : MonoBehaviour {

	[Space(10)][Header("Collision Masks")]
	public LayerMask collisionMask;
	public LayerMask playerMask;

	[Space(10)][Header("Network Settings")]
	public NetworkPerspective networkPerspective;

	[Space(10)][Header("Refereneces")]
	public Player parentPlayer;
	public SphereCollider projectileCollider;
	public TrailRenderer trailRenderer;
	public Transform model;

	[Space(10)][Header("Attributes")]
	public ProjectileAttributes projectileAttributes;
	public int projectileId;
	public bool isStuck;
	public bool hasCollided;
	public float firstCollisionTime;

	List<Entity> damagedEntities = new List<Entity>();

	// Constants
	float skinWidth = 0.05f;

	// Variables
	Vector3 velocity;
	float leftoverDeltaP;                   // Remaining delta Position from UpdateMovement which is accumulated as a result of bouncing off of surfaces. LeftoverDeltaP is used to make lineRenderers look cleaner and more accurate to projectiles actual path
	float timeCreated = 0;
	bool isDestroyed = false;
	
	public void InitializeProjectile (int newProjectileId, ProjectileAttributes newProjectileAttributes, NetworkPerspective newNetworkPerspective, Player player) {
		// Get References
		projectileCollider = GetComponent<SphereCollider>();
		model = transform.Find("(Model)");
		trailRenderer = model.GetComponent<TrailRenderer>();

		// Add parentPlayer to damagedEntities as to ignore it on collision
		//damagedEntities.Add(player);

		// Set Attributes
		projectileId = newProjectileId;
		projectileAttributes = new ProjectileAttributes(newProjectileAttributes);
		networkPerspective = newNetworkPerspective;
		parentPlayer = player;

		// Utilize Attributes
		model.transform.localScale = Vector3.one * projectileAttributes.projectileRadius * 2;
		velocity = transform.forward * projectileAttributes.muzzleVelocity;
		projectileCollider.radius = projectileAttributes.projectileRadius;
		trailRenderer.widthMultiplier = projectileAttributes.projectileRadius * 2;
		trailRenderer.time = projectileAttributes.trailRendererTime;
		timeCreated = Time.time;
	}

	private void Update () {
		if (timeCreated + projectileAttributes.lifespan >= Time.time && (projectileAttributes.hasDelayedCollisionDetonation == false || hasCollided == false || firstCollisionTime + projectileAttributes.delayedCollisionDetonationTime > Time.time)) {
			UpdateMovement();
		} else {
			if (isStuck == false) {
				StartCoroutine(DestroyProjectile(true));
			}
		}
	}

	public void UpdateMovement() {
		// Make sure this projectile isn't already destroyed/destroying
		if (isDestroyed == true) {
			return;
		}

		float deltaP = (velocity.magnitude * Time.deltaTime) + leftoverDeltaP;
		leftoverDeltaP = 0;
		RaycastHit hit;
		
		// Check for collision
		if (Physics.SphereCast(transform.position, projectileAttributes.projectileRadius, velocity, out hit, deltaP + skinWidth, collisionMask)) {
			float ricochetAngle = Vector3.Angle(velocity, Vector3.Reflect(velocity, hit.normal));
			float trueHitDistance = hit.distance - skinWidth;

			// Delayed Collision Detonation Info
			if (hasCollided == false) {
				hasCollided = true;
				firstCollisionTime = Time.time;
			}

			// Player Damage
			if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Players")) {
				if (hit.transform.GetComponent<Entity>()) {
					Entity hitEntity = hit.transform.GetComponent<Entity>();
					
					if (projectileAttributes.entityPenetration == false) {
						// Damage entity and destroy projectile
						parentPlayer.ProjectileDamage(hitEntity.entityId, projectileId, velocity.x, velocity.y, velocity.z);
						StartCoroutine(DestroyProjectile(false));
					} else {
						// If we haven't already damaged this entity, damage it, and then pass through it; otherwise just pass through it
						if (damagedEntities.Contains(hitEntity) == false) {
							// Damage the entity and add it to damagedEntities list
							parentPlayer.ProjectileDamage(hitEntity.entityId, projectileId, velocity.x, velocity.y, velocity.z);
							damagedEntities.Add(hitEntity);
							transform.position += (velocity.normalized * (hit.distance + 0.05f));
						} else {
							transform.position += (velocity.normalized * (hit.distance + 0.05f));
						}
					}

					return;
				}
			}

			if (projectileAttributes.ricochetCount > 0 && ricochetAngle <= projectileAttributes.ricochetMaxAngle) {	// Check that there are ricochets left AND the ricochet angle is less than/equal to max ricochet angle
				// Subtract a ricochcet
				projectileAttributes.ricochetCount--;

				// Subtract current distance traveled from deltaP
				deltaP -= trueHitDistance;

				// Move projectile forwards based on collisionDistance
				transform.position += (velocity.normalized * trueHitDistance);

				// Reflect the projectiles velocity
				velocity = Vector3.Reflect(velocity, hit.normal);
				velocity = velocity * Mathf.Lerp(Mathf.Clamp01(projectileAttributes.bouncinessMax), Mathf.Clamp01(projectileAttributes.bouncinessMin), ricochetAngle / projectileAttributes.ricochetMaxAngle);		// Apply bounciness
				transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);

				// Save leftover deltaP
				leftoverDeltaP = Mathf.Clamp(deltaP, 0, Mathf.Infinity);
			} else {
				// Move projectile forwards based on collisionDistance
				transform.position += (velocity.normalized * (trueHitDistance + skinWidth));

				// If this projectile is sticky, make it stick
				if (projectileAttributes.isSticky == true) {
					transform.SetParent(hit.transform, true);

					isStuck = true;

					// Destroy Projectile + stickyDelay
					StartCoroutine(DestroyProjectile(false, projectileAttributes.stickyLifespan));
				} else {
					// Destroy Projectile
					StartCoroutine(DestroyProjectile(false));
				}
			}
		} else {
			transform.position += (velocity * Time.deltaTime);
		}

		// Apply Gravity
		velocity += new Vector3(0, projectileAttributes.gravityScale * -9.81f * Time.deltaTime, 0);
	}
	
	public IEnumerator DestroyProjectile (bool instantlyDestroy = false, float additionalDelay = 0) {
		// If this projectile is explosive, explode
		if (projectileAttributes.isExplosive == true) {
			Explode();
		}

		// Disable collider
		
		projectileCollider.enabled = false;

		if (instantlyDestroy == true) {
			Destroy(gameObject);
		} else {
			isDestroyed = true;

			yield return new WaitForSeconds(projectileAttributes.trailRendererTime);

			trailRenderer.enabled = false;

			yield return new WaitForSeconds(additionalDelay);

			// Remove projectile from parentPlayer projectiles Dictionary
			if (parentPlayer.projectiles.ContainsKey(projectileId)) {
				parentPlayer.projectiles.Remove(projectileId);
			}

			Destroy(gameObject);
		}
	}

	public void Explode() {
		Vector3 explosionOrigin = transform.position + (velocity.normalized * -0.1f);
		parentPlayer.SendClientRPC("CheckProjectileExplosion", new string[] { projectileId.ToString(), explosionOrigin.x.ToString(), explosionOrigin.y.ToString(), explosionOrigin.z.ToString() });
	}

}
