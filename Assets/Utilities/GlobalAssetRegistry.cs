using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class GlobalAssetRegistry : Singleton.Asset<GlobalAssetRegistry>
{
    public List<Singleton.Asset> assets;
    public List<GameObject> typedPrefabs;

    [Serializable]
    private struct NamedPrefab
    {
        public string name;
        public GameObject prefab;
    }

    [SerializeField] private List<NamedPrefab> namedPrefabs;

    public override void OnEnable()
    {
        base.OnEnable();
        for (int i = 0; i < assets.Count && assets[i] != null; i++) assets[i].OnEnable();
        for (int i = 0; i < typedPrefabs.Count && typedPrefabs[i] != null; i++) Singleton.IGlobalPrefab.RegisterPrefab(typedPrefabs[i]);
        for (int i = 0; i < namedPrefabs.Count; i++) Singleton.IGlobalPrefab.RegisterPrefab(namedPrefabs[i].prefab, namedPrefabs[i].name);
    }

#if UNITY_EDITOR

    public class PostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!GlobalAssetRegistry.TryGet(out GlobalAssetRegistry registry))
                registry = GetOrCreate(typeof(GlobalAssetRegistry)) as GlobalAssetRegistry;

            registry.OnEnable();

            Type[] globalAssetTypes = GetAllChildTypes(typeof(Singleton.Asset<>));
            foreach (Type type in globalAssetTypes)
            {
                if (type == typeof(GlobalAssetRegistry)) continue;

                // Look for a static "_instance" field on the concrete asset type
                FieldInfo instanceField = type.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                object currentInstance = instanceField?.GetValue(null);

                // If no existing in-memory instance is found, ensure an asset exists on disk
                if (currentInstance == null)
                {
                    Singleton.Asset created = GetOrCreate(type);
                    if (created != null)
                    {
                        registry.assets ??= new List<Singleton.Asset>();
                        if (!registry.assets.Contains(created)) registry.assets.Add(created);
                        // Initialize the created asset if needed
                        created.OnEnable();
                    }
                }
            }

            // Ensure the GlobalAssetRegistry asset is added to PlayerSettings preloaded assets
            try
            {
                UnityEngine.Object[] preloaded = PlayerSettings.GetPreloadedAssets() ?? Array.Empty<UnityEngine.Object>();
                if (!preloaded.Contains(registry))
                {
                    var list = preloaded.ToList();
                    list.Add(registry);
                    PlayerSettings.SetPreloadedAssets(list.ToArray());
                }
            }
            catch (Exception) { }// If PlayerSettings APIs change or fail in some contexts, fail silently to avoid breaking import pipeline

            //last minute run through of registry's assets to get rid of Null values.
            for (int i = registry.assets.Count - 1; i >= 0; i--)
                if (registry.assets[i] == null) registry.assets.RemoveAt(i); 

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Non-generic variant to create/load assets by runtime Type
        static Singleton.Asset GetOrCreate(Type t)
        {
            if (t == null) return null;

            string searchFilter = $"t:{t.Name}";
            string[] guids = AssetDatabase.FindAssets(searchFilter);

            if (guids != null && guids.Length > 0)
            {
                if (guids.Length > 1)
                {
                    for (int i = guids.Length - 1; i > 0; i--)
                    {
                        UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]));
                        if (obj != null) Destroy(obj);
                    }
                }

                UnityEngine.Object loaded = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (loaded is Singleton.Asset asset) return asset;
            }

            // Create new ScriptableObject instance of the requested Type
            ScriptableObject created = ScriptableObject.CreateInstance(t);
            if (created == null) return null;

            AssetDatabase.CreateAsset(created, $"Assets/{t.Name}.asset");
            AssetDatabase.SaveAssets();

            return created as Singleton.Asset;
        }

        static Type[] GetAllChildTypes(Type T) => Assembly.GetAssembly(T).GetTypes().Where(i => ImplementsOrDerives(i, T) && !i.IsAbstract).ToArray();

        static bool ImplementsOrDerives(Type @this, Type from)
        {
            if (from is null)
                return false;

            if (!from.IsGenericType || !from.IsGenericTypeDefinition)
                return from.IsAssignableFrom(@this);

            if (from.IsInterface)
                foreach (Type @interface in @this.GetInterfaces())
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == from)
                        return true;

            if (@this.IsGenericType && @this.GetGenericTypeDefinition() == from)
                return true;

            return @this.BaseType != null && ImplementsOrDerives(@this.BaseType, from);
        }

    }
#endif
}