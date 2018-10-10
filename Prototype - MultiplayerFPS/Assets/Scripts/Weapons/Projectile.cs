using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

	public Material material;

	private void Start () {
		material = transform.Find("(Model)").GetComponent<Renderer>().material;
	}

	private void Update () {
		material.color = new Color(material.color.r, material.color.g, material.color.b, Mathf.Clamp01(material.color.a - (Time.deltaTime * 2.5f)));
		if (material.color.a == 0) {
			Destroy(gameObject);
		}
	}

}
