using System.Collections.Generic;
using UnityEngine;
using System;

namespace Utilities.Singletons
{
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
}