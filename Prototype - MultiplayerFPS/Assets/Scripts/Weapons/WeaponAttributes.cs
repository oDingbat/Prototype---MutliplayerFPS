using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponAttributes {

	[Space(10)][Header("Ammo Info")]
	public int ammoCurrent;						// The current amount of ammo this weapon has
	public int ammoMax;                         // The maximum amount of ammo this weapon can have
	public string ammoType;						// The string specifying the ammo type this weapon carries
	public int consumption;                     // The amount of ammo that is consumed per shot
	
	[Space(10)][Header("Firing Info")]
	public bool automatic;
	public float firerate;                                      // The number of times this weapon can fire per second (ex: 10 = 10 shots per second)
	public float timeLastFired;                                 // The time at which the weapon was last fired
	public int burstCount;                                      // The number of bursts of projectiles created when firing this weapon
	public float burstDelay;

	[Space(10)][Header("Zoom Info")]
	public float zoomIncrement;
	public float zoomDecrement;
	public float zoomCurrent;
	public float zoomFOVMultiplier;
}
