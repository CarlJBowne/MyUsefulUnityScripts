using System;
using System.Collections.Generic;
using SLS.ListUtilities;
using SLS.Singletons;
using UnityEngine;

[DefaultExecutionOrder(-160)]
public class AssetRegistry : SLS.Singletons.GlobalAsset<AssetRegistry>
{
    [SerializeField] private DictionaryS<string, Prefab> namedPrefabs = new();
    [SerializeField] private DictionaryS<string, SceneSO> namedSceneAssets = new();
    [SerializeField] private DictionaryS<string, string> namedScenes = new();
    [SerializeField] private DictionaryS<string, ScriptableObject> namedSOs = new();

    public static IReadOnlyDictionary<string, Prefab> NamedPrefabs;
    public static IReadOnlyDictionary<string, SceneSO> NamedScenes;
    public static IReadOnlyDictionary<string, ScriptableObject> NamedAssets;


    public override void OnInit()
    {
        NamedPrefabs = namedPrefabs.ToNativeDictionary();
        NamedAssets = namedSOs.ToNativeDictionary();
        Dictionary<string, SceneSO> scenes = namedSceneAssets.ToNativeDictionary();
        for (int i = 0; i < namedScenes.Count; i++)
            if (!scenes.ContainsKey(namedScenes.KeyFromIndex(i)))
                scenes.Add(namedScenes.KeyFromIndex(i), SceneSO.CreateRuntime(namedScenes.ValueFromIndex(i)));

    }

    public static Prefab Prefab(string name) => NamedPrefabs[name];
    public static Prefab<T> Prefab<T>(string name) where T : Component => NamedPrefabs[name] as Prefab<T>;
    public static T Asset<T>(string name) where T : ScriptableObject => NamedAssets[name] as T;
    public static SceneSO Scene(string name) => NamedScenes[name];

    public static void LoadScene(string name) { if (NamedScenes.TryGetValue(name, out SceneSO scene)) scene.Load(); }
}
