using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLS.AssetUtilties;
using SLS.Singletons;

public interface IGlobalScene
{
    /// <summary>
    /// Dictionary mapping a component Type to its registered <see cref="SceneAsset"/> Wrappers
    /// </summary>
    public static Dictionary<Type, SceneAsset> typedScenes { get; } = new();

    /// <summary>
    /// Dictionary mapping a string name to its registered <see cref="SceneAsset"/> Wrappers
    /// </summary>
    public static Dictionary<string, SceneAsset> namedScenes { get; } = new();

    /// <summary>
    /// Registers a prefab using the type of the <see cref="IGlobalPrefab"/> component found on the prefab's root GameObject.
    /// If a prefab for that type is already registered, the existing entry is preserved.
    /// </summary>
    /// <param name="prefab">The GameObject prefab to register. Must contain a component implementing <see cref="IGlobalPrefab"/>.</param>
    public static void RegisterScene(SceneReference input, Type type)
    {
        if (typedScenes.ContainsKey(type)) return;
        typedScenes[type] = SceneAsset.CreateRuntime(input);
    }

    /// <summary>
    /// Registers a prefab under the specified name. If a prefab with the same name already exists, it is preserved.
    /// </summary>
    /// <param name="prefab">The GameObject prefab to register.</param>
    /// <param name="name">The name under which to register the prefab.</param>
    public static void RegisterScene(SceneReference input, string name)
    {
        if (namedScenes.ContainsKey(name)) return;
        namedScenes[name] = SceneAsset.CreateRuntime(input);
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
    public static void Load<T>()
    {
        if (typedScenes.TryGetValue(typeof(T), out SceneAsset asset)) asset.Load();
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
    public static void Load(string name)
    {
        if (namedScenes.TryGetValue(name, out SceneAsset scene)) scene.Load();
    }
}
