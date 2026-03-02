using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A base class for globally accessible "Singleton" type Scriptable Object Assets.
/// Inherit from `GlobalAsset&lt;YourType&gt;` to gain automatic registration and availability.
/// </summary>
/// <remarks>
/// <see cref="GlobalAsset{T}"/>s are automatically registered with the <see cref="GlobalAssetRegistry"/>, which is itself automatically registered with Preloaded Assets. 
/// </remarks>
/// <typeparam name="T">The concrete type inheriting this base class (the singleton type).</typeparam>
public abstract class GlobalAsset<T> : Singleton.Z_Asset where T : class
{
    /// <summary>
    /// Backing field for the singleton asset instance.
    /// </summary>
    private static T _instance;

    /// <summary>
    /// Gets the registered asset singleton instance.
    /// </summary>
    public static T Get => Singleton.Get(ref _instance);

    /// <summary>
    /// Attempts to get the currently registered asset singleton.
    /// </summary>
    /// <param name="instance">Out parameter that receives the instance if present.</param>
    /// <returns>True if an instance is present; otherwise false.</returns>
    public static bool TryGet(out T instance) => Singleton.TryGet(Get, out instance);

    /// <summary>
    /// Unity OnEnable callback override - registers this ScriptableObject as the singleton instance.
    /// </summary>
    public override void OnEnable() => Singleton.Register(ref _instance, this as T);

    /// <summary>
    /// Unity OnDisable callback - unregisters this ScriptableObject if it is registered.
    /// </summary>
    private void OnDisable()
    {
        Singleton.Unregister(ref _instance, this as T);

    }

    public virtual void OnInit() { }
    public virtual void OnDeInit() { }
}

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

/// <summary>
/// Interface providing a global prefab registry and instantiation helper.
/// Implementing components can be registered so prefabs can be instantiated by type or name.
/// Note: This interface provides static helper members for registry management.
/// </summary>
public interface IGlobalPrefab
{
    /// <summary>
    /// Dictionary mapping a component Type to its registered <see cref="Prefab"/> wrapper.
    /// </summary>
    public static Dictionary<Type, Prefab> typedPrefabs { get; } = new();

    /// <summary>
    /// Dictionary mapping a string name to its registered <see cref="Prefab"/> wrapper.
    /// </summary>
    public static Dictionary<string, Prefab> namedPrefabs { get; } = new();

    /// <summary>
    /// Registers a prefab using the type of the <see cref="IGlobalPrefab"/> component found on the prefab's root GameObject.
    /// If a prefab for that type is already registered, the existing entry is preserved.
    /// </summary>
    /// <param name="prefab">The GameObject prefab to register. Must contain a component implementing <see cref="IGlobalPrefab"/>.</param>
    public static void RegisterPrefab(GameObject prefab)
    {
        Type type = prefab.GetComponent<IGlobalPrefab>().GetType();
        if (!typedPrefabs.ContainsKey(type)) typedPrefabs[type] = new(prefab);
    }

    /// <summary>
    /// Registers a prefab under the specified name. If a prefab with the same name already exists, it is preserved.
    /// </summary>
    /// <param name="prefab">The GameObject prefab to register.</param>
    /// <param name="name">The name under which to register the prefab.</param>
    public static void RegisterPrefab(GameObject prefab, string name)
    {
        if (!namedPrefabs.ContainsKey(name)) namedPrefabs[name] = new(prefab);
    }
    /// <summary>
    /// Instantiates a registered prefab of the specified type and optionally sets its parent transform.
    /// </summary>
    /// <remarks>If no prefab is registered for the specified type parameter, a warning is logged and
    /// the method returns null.</remarks>
    /// <typeparam name="T">The type of the prefab to instantiate. Must be a type that has been registered with a corresponding prefab.</typeparam>
    /// <param name="parent">The transform to set as the parent of the instantiated object. If null, the object is instantiated at the
    /// root level.</param>
    /// <returns>The instantiated GameObject if a prefab is registered for the specified type; otherwise, null.</returns>
    public static GameObject Instantiate<T>(Transform parent = null)
    {
        if (typedPrefabs.TryGetValue(typeof(T), out Prefab prefab)) return prefab.Instantiate(parent);
        Debug.LogWarning($"No prefab registered for type {typeof(T)}");
        return null;
    }
    /// <summary>
    /// Instantiates a registered prefab by name and optionally sets its parent transform.
    /// </summary>
    /// <remarks>If the specified name does not correspond to a registered prefab, a warning is logged
    /// and null is returned.</remarks>
    /// <param name="name">The name of the registered prefab to instantiate. Must correspond to a prefab that has been registered
    /// previously.</param>
    /// <param name="parent">The transform to set as the parent of the instantiated object. If null, the object is instantiated at the
    /// root level.</param>
    /// <returns>A new instance of the specified prefab as a GameObject, or null if no prefab is registered with the given
    /// name.</returns>
    public static GameObject Instantiate(string name, Transform parent = null)
    {
        if (namedPrefabs.TryGetValue(name, out Prefab prefab)) return prefab.Instantiate(parent);
        Debug.LogWarning($"No prefab registered for name {name}");
        return null;
    }
}