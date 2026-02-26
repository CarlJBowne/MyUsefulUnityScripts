using AYellowpaper.SerializedCollections;
using UnityEngine;

public class AudioCaller : MonoBehaviour
{
	public SerializedDictionary<string, AudioClip> clips;
	public AudioCaller remote;

	private AudioSource source;
	private bool initialized;

	private void Awake() => Initialize();
	private void Initialize()
	{
		if (initialized) return;
		source = GetComponent<AudioSource>();
		if (source == null) source = gameObject.AddComponent<AudioSource>();
		source.playOnAwake = false;
		source.loop = false;
		source.spatialBlend = 1f;
		initialized = true;
	}

	private bool GetClip(out AudioClip clip, string name, bool warn = true)
	{
		bool nameExists = clips.TryGetValue(name, out clip);
		if (!nameExists) { if (warn) Debug.LogWarningFormat("No sound with name {0} found on {1}.", name, gameObject); }
		else if (clip == null) Debug.LogWarningFormat("Open sound slot with intended name \"{1}\" on {0} found, ensure to fill at some point.", gameObject, name);
		return clip != null;
	}

	/// <summary>
	/// Plays a Sound using the Source attached to this object.
	/// </summary>
	/// <param name="name">The name identifier of the sound.</param>
	/// <param name="volume">The volume you want to play it at.</param>
	/// <param name="warn">Whether the lack of a correctly named sound slot will produce a warning. (On by defualt.)</param>
	public void PlaySound(string name, float volume = 1, bool warn = true)
	{
		if (remote) { remote.PlaySound(name, volume, warn); return; }
		Initialize();

		if (GetClip(out AudioClip clip, name, warn)) 
			source.PlayOneShot(clip, volume);
	}
	/// <summary>
	/// Plays a Sound using the Source attached to this object.
	/// </summary>
	/// <param name="name">The name identifier of the sound.</param>
	public void PlaySound(string name) => PlaySound(name);

	/// <summary>
	/// Plays a Sound by creating a new temporary object to play it from. (Use if the sound is likely to play when an object is destroyed.)
	/// </summary>
	/// <param name="name">The name identifier of the sound.</param>
	/// <param name="volume">The volume you want to play it at.</param>
	/// <param name="warn">Whether the lack of a correctly named sound slot will produce a warning. (On by defualt.)</param>
	public void PlaySoundPersisting(string name, float volume = 1, bool warn = true)
	{
		if (remote) { remote.PlaySound(name, volume, warn); return; }

		if (GetClip(out AudioClip clip, name, warn))
			AudioSource.PlayClipAtPoint(clip, transform.position, 1);
	}
	/// <summary>
	/// Plays a Sound by creating a new temporary object to play it from. (Use if the sound is likely to play when an object is destroyed.)
	/// </summary>
	/// <param name="name">The name identifier of the sound.</param>
	public void PlaySoundPersisting(string name) => PlaySoundPersisting(name);

	/// <summary>
	/// Plays a Sound using the Global Audio Caller. (Not affected by 3D Space.)
	/// </summary>
	/// <param name="name">The name identifier of the sound.</param>
	/// <param name="volume">The volume you want to play it at.</param>
	/// <param name="warn">Whether the lack of a correctly named sound slot will produce a warning. (On by defualt.)</param>
	public void PlaySoundGlobal(string name, float volume = 1, bool warn = true)
	{
		if (remote) { remote.PlaySound(name, volume, warn); return; }

		//if (GetClip(out AudioClip clip, name, warn))
        //    GlobalAudioCaller.Get().PlaySound(clip, volume);
	}
	/// <summary>
	/// Plays a Sound using the Global Audio Caller. (Not affected by 3D Space.)
	/// </summary>
	/// <param name="name">The name identifier of the sound.</param>
	public void PlaySoundGlobal(string name) => PlaySoundGlobal(name);

}
