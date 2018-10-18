using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathCamera : MonoBehaviour {

	public Transform targetRagdoll;
	public LayerMask cameraCollisionMask;

	Vector3 positionCurrent;
	Vector3 positionTarget;
	Vector3 positionCamera;

	float camDistanceCurrent;
	float camDistanceDesired = 5;
	float camZoomSpeed = 2f;
	float wallDistance = 0.5f;

	private void Update () {
		positionTarget = targetRagdoll.position;
		positionCurrent = Vector3.Lerp(positionCurrent, positionTarget, 10 * Time.deltaTime);

		RaycastHit hit;
		float camDistanceNext = Mathf.Lerp(camDistanceCurrent, camDistanceDesired, camZoomSpeed * Time.deltaTime);
		if (Physics.Raycast(positionCurrent, transform.forward * -1, out hit, camDistanceNext + wallDistance, cameraCollisionMask)) {
			camDistanceCurrent = Mathf.Clamp(hit.distance - wallDistance, 0, camDistanceDesired);
		} else {
			camDistanceCurrent = camDistanceNext;
		}

		transform.position = positionCurrent + (camDistanceCurrent * transform.forward * -1);
	}

	public void InitializeDeathCamera (Transform newTargetRagdoll, Quaternion newRotation) {
		targetRagdoll = newTargetRagdoll;
		transform.rotation = newRotation;

		positionTarget = targetRagdoll.position;
		positionCurrent = positionTarget;
		camDistanceCurrent = 0;
	}

}
