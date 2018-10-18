using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour {

	[Space(10)][Header("References")]
	public Client client;
	public Player player;

	[Space(10)][Header("UI References")]
	public Text text_Health;
	public Text text_Armor;
	public Text text_WeaponAmmo;
	public Text text_WeaponName;
	public Text text_InteractionDescription;
	public RectTransform ui_Crosshair;

	[Space(10)][Header("Colors")]
	public Color color_White;
	public Color color_Red;


	void Start () {
		if (GameObject.Find("[Client]")) {
			client = GameObject.Find("[Client]").GetComponent<Client>();
		} else {
			if (player == null) {
				gameObject.SetActive(false);
			}
		}
	}

	void Update () {
		if (client || player) {
			if (player == null) {
				player = client.clientPlayer;
				return;
			}

			// Set Player Vitals Text
			text_Health.text = (player.vitals.healthCurrent + " / " + player.vitals.healthMaximum);
			text_Armor.text = (player.vitals.armorCurrent + " / " + player.vitals.armorMaximum);

			// Set Player Vitals Colors
			text_Health.color = (player.vitals.healthCurrent < player.vitals.healthMaximum / 2) ? color_Red : color_White;
			text_Armor.color = (player.vitals.armorCurrent < player.vitals.armorMaximum / 2) ? color_Red : color_White;

			// Interaction text
			text_InteractionDescription.text = player.interactionDescription;

			UpdateWeaponInfo();
		}
	}

	private void UpdateWeaponInfo () {
		if (player.weapons.Count > 0) {
			Weapon playerWeapon = player.weapons[player.weaponCurrentIndex];

			// Ammo
			text_WeaponAmmo.text = playerWeapon.weaponAttributes.ammoCurrent + " / " + playerWeapon.weaponAttributes.ammoMax;
			text_WeaponName.text = playerWeapon.name;

			// Crosshair
			float crosshairScale = 2 - Mathf.Clamp01((Time.time - playerWeapon.weaponAttributes.timeLastFired) / (1 / playerWeapon.weaponAttributes.firerate));
			ui_Crosshair.localScale = new Vector3(crosshairScale, crosshairScale, 1);
		} else {
			// Ammo
			text_WeaponAmmo.text = "";
			text_WeaponName.text = "";

			// Crosshair
			ui_Crosshair.localScale = new Vector3(1, 1, 1);
		}
	}

}
