using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponAttributes {

	[Space(10)][Header("Firing Info")]
	public float firerate;                                      // The number of times this weapon can fire per second (ex: 10 = 10 shots per second)
	public float timeLastFired;                                 // The time at which the weapon was last fired

	[Space(10)][Header("Zoom Info")]
	public float zoomIncrement;
	public float zoomDecrement;
	public float zoomCurrent;
	public float zoomFOVMultiplier;
}
