using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour {

	[Space(10)][Header("References")]
	public Client client;
	public Player player;

	[Space(10)][Header("Panels")]
	public GameObject panel_PauseMenu;
	public GameObject panel_MainMenu;

	public bool isMainMenu;
	public bool isPaused;

	private void Start () {
		if (GameObject.Find("[Client]") != null) {
			client = GameObject.Find("[Client]").GetComponent<Client>();
		}
	}

	private void Update() {
		UpdateInput();
	}

	private void UpdateInput () {
		// Pause Menu Toggle
		if (Input.GetKeyDown(KeyCode.Escape)) {
			TogglePauseMenu();
		}

		// Fullscreen toggle
		if (Input.GetKeyDown(KeyCode.F11)) {
			Screen.fullScreen = !Screen.fullScreen;
		}
	}

	public void ToggleMainMenu (bool toggle) {
		if (toggle == true) {
			panel_MainMenu.SetActive(true);

			// Set Cursor Info
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.Confined;
		} else {
			panel_MainMenu.SetActive(false);

			// Set Cursor Info
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
		}
	}

	public void TogglePauseMenu () {
		if (isPaused == true) {
			isPaused = false;

			// Set Cursor Info
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;

			panel_PauseMenu.SetActive(false);
		} else {
			isPaused = true;

			// Set Cursor Info
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.Confined;

			panel_PauseMenu.SetActive(true);
		}
	}

	public void Quit () {
		Application.Quit();
	}

}
