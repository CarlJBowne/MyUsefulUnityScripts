using AYellowpaper.SerializedCollections;
using UnityEngine;

public class GlobalAudioCaller : MonoBehaviour
{

	private AudioSource source;
	private bool initialized;

	public SerializedDictionary<string, AudioClip> clips;



	//protected override void OnInitialize() => Init();

    void Init()
	{
		if (initialized) return;
		source = GetComponent<AudioSource>();
		if (source == null) source = gameObject.AddComponent<AudioSource>();
		source.playOnAwake = false;
		source.loop = false;
		source.spatialBlend = 0f;
		initialized = true;
	}

	public void PlaySound(string name, float volume = 1, bool warn = true)
	{
        Init();

		bool nameExists = clips.TryGetValue(name, out AudioClip clip);
		if (!nameExists) { if (warn) Debug.LogWarningFormat("No sound with name {0} found on {1}.", name, gameObject); }
		else if (clip == null) Debug.LogWarningFormat("Open sound slot with intended name \"{1}\" on {0} found, ensure to fill at some point.", gameObject, name);
		else source.PlayOneShot(clip, volume);

	}
	public void PlaySound(string name) => PlaySound(name);
	public void PlaySound(AudioClip clip, float volume = 1)
	{
        Init();
        source.PlayOneShot(clip, volume);
	}

}
