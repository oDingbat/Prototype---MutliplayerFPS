using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {

	public NetworkPerspective networkPerspective;

	public int jukeboxCount = 32;
	public int jukeboxIndex;
	public List<AudioSource> jukeboxes;

	public GameObject prefab_Jukebox;

	private void Start () {
		InitializeJukeboxes();
	}

	private void InitializeJukeboxes () {
		for (int i = 0; i < jukeboxCount; i++) {
			AudioSource newJukebox = Instantiate(prefab_Jukebox, transform.position, Quaternion.identity, transform).GetComponent<AudioSource>();
			jukeboxes.Add(newJukebox);
		}
	}

	public void PlayClipAtPoint(Vector3 localPosition, AudioClip clip) {
		PlayClipAtPoint(localPosition, clip, 1f, 1f, transform);
	}
	public void PlayClipAtPoint(Vector3 localPosition, AudioClip clip, float volume) {
		PlayClipAtPoint(localPosition, clip, volume, 1f, transform);
	}
	public void PlayClipAtPoint(Vector3 localPosition, AudioClip clip, float volume, float pitch) {
		PlayClipAtPoint(localPosition, clip, volume, pitch, transform);
	}
	public void PlayClipAtPoint(Vector3 localPosition, AudioClip clip, float volume, Transform parent) {
		PlayClipAtPoint(localPosition, clip, volume, 1f, parent);
	}
	public void PlayClipAtPoint(Vector3 localPosition, AudioClip clip, Transform parent) {
		PlayClipAtPoint(localPosition, clip, 1f, 1f, parent);
	}
	public void PlayClipAtPoint(Vector3 localPosition, AudioClip clip, float volume, float pitch, Transform parent) {
		PlayClipAtPoint(localPosition, clip, volume, pitch, 25, parent);
	}

	public void PlayClipAtPoint (Vector3 localPosition, AudioClip clip, float volume, float pitch, float maxDistance, Transform parent) {
		// Make sure we have any jukeboxes
		if (jukeboxes.Count == 0) {
			Debug.LogError("Error: No jukeboxes found!");
			return;
		}

		// Increment through jukebox list
		jukeboxIndex++;
		if (jukeboxIndex >= jukeboxCount) {
			jukeboxIndex = 0;
		}

		// Get current jukebox
		AudioSource jukeboxCurrent = jukeboxes[jukeboxIndex];

		// Setup current jukebox
		jukeboxCurrent.transform.parent = parent;
		jukeboxCurrent.transform.localPosition = localPosition;
		jukeboxCurrent.clip = clip;
		jukeboxCurrent.volume = volume;
		jukeboxCurrent.pitch = pitch;
		jukeboxCurrent.maxDistance = maxDistance;

		// Play jukebox
		jukeboxCurrent.Stop();
		if (networkPerspective != NetworkPerspective.Server) {
			jukeboxCurrent.Play();
		}
	}

}
