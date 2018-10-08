using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBubble : MonoBehaviour {

	public NetworkPerspective networkPerspective;

	private void OnTriggerEnter(Collider col) {
		if (col.transform.GetComponent<Player>() != null) {
			if (networkPerspective == NetworkPerspective.Server) {
				col.transform.GetComponent<Player>().Heal(25);
				Destroy(gameObject);
			}
		}
	}

}
