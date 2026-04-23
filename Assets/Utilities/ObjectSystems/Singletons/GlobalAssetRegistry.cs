using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Utilities.Singletons
{
    /// <summary>
    /// A global registry that manages <see cref="GlobalAsset{t}"/>s and manually added globally accessible prefabs.
    /// </summary>
    /// <remarks>
    /// An instance of this, and all <see cref="GlobalAsset{t}"/>s are automatically created and registered if they don't already exist. <see cref="GlobalAsset{t}"/>s are registered to this, which is in turn registered to PlayerSettings preloaded assets, ensuring they are always loaded and accessible at runtime and in the editor.
    /// </remarks>
    public class GlobalAssetRegistry : GlobalAsset<GlobalAssetRegistry>
    {
        public List<Singleton.Z_Asset> assets;
        [SerializeField] private List<GameObject> typedPrefabs;

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
            for (int i = 0; i < typedPrefabs.Count && typedPrefabs[i] != null; i++) IGlobalPrefab.RegisterPrefab(typedPrefabs[i]);
            for (int i = 0; i < namedPrefabs.Count; i++) IGlobalPrefab.RegisterPrefab(namedPrefabs[i].prefab, namedPrefabs[i].name);
        }

#if UNITY_EDITOR

        public class PostProcessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (!GlobalAssetRegistry.TryGet(out GlobalAssetRegistry registry))
                    registry = GetOrCreate(typeof(GlobalAssetRegistry)) as GlobalAssetRegistry;

                registry.OnEnable();

                Type[] globalAssetTypes = GetAllChildTypes(typeof(GlobalAsset<>));
                foreach (Type type in globalAssetTypes)
                {
                    if (type == typeof(GlobalAssetRegistry)) continue;

                    // Look for a static "_instance" field on the concrete asset type
                    FieldInfo instanceField = type.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    object currentInstance = instanceField?.GetValue(null);

                    // If no existing in-memory instance is found, ensure an asset exists on disk
                    if (currentInstance == null)
                    {
                        Singleton.Z_Asset created = GetOrCreate(type);
                        if (created != null)
                        {
                            registry.assets ??= new List<Singleton.Z_Asset>();
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
            static Singleton.Z_Asset GetOrCreate(Type t)
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
                    if (loaded is Singleton.Z_Asset asset) return asset;
                }

                // Create new ScriptableObject instance of the requested Type
                ScriptableObject created = ScriptableObject.CreateInstance(t);
                if (created == null) return null;

                AssetDatabase.CreateAsset(created, $"Assets/{t.Name}.asset");
                AssetDatabase.SaveAssets();

                return created as Singleton.Z_Asset;
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

}