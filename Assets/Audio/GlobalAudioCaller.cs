using AYellowpaper.SerializedCollections;
using UnityEngine;
using Utilities.Singletons;

public class GlobalAudioCaller : MonoBehaviour
{
    private static GlobalAudioCaller instance;

    public static GlobalAudioCaller Get => Singleton.Get(ref instance);
    public static bool TryGet(out GlobalAudioCaller res) => Singleton.TryGet(Get, out res);

    private static AudioSource source;
    private static bool initialized;

    public SerializedDictionary<string, AudioClip> clips;

    private void Awake()
    {
        Singleton.Register(ref instance, this);
        Init();
    }
    private void OnDestroy() => Singleton.Unregister(ref instance, this);


    static void Init()
    {
        if (initialized) return;
        Get.TryGetComponent(out source);
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        initialized = true;
    }

    public static void PlaySound(string name, float volume = 1, bool warn = true)
    {
        Init();

        bool nameExists = Get.clips.TryGetValue(name, out AudioClip clip);
        if (!nameExists) { if (warn) Debug.LogWarningFormat("No sound with name {0} found on {1}.", name, Get.gameObject); }
        else if (clip == null) Debug.LogWarningFormat("Open sound slot with intended name \"{1}\" on {0} found, ensure to fill at some point.", Get.gameObject, name);
        else source.PlayOneShot(clip, volume);

    }
    public static void PlaySound(string name) => PlaySound(name);
    public static void PlaySound(AudioClip clip, float volume = 1)
    {
        Init();
        source.PlayOneShot(clip, volume);
    }

}
