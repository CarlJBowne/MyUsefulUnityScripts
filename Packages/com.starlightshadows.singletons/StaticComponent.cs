using SLS.Singletons;
using UnityEngine;

/// <summary>
/// A static helper class tracking a single persistant GameObject on which any user can add a component.
/// </summary>
[DefaultExecutionOrder(-9999888)]
public static class StaticComponents
{
    /// <summary>
    /// The single persistant GameObject on which any user can add a component.
    /// </summary>
    public static GameObject gameObject { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void INIT()
    {
        gameObject = new("--Static Components--");
        GameObject.DontDestroyOnLoad(gameObject);
        gameObject.hideFlags = HideFlags.NotEditable;
    }

    /// <summary>
    /// Adds a component of type T to the static GameObject and returns it.
    /// </summary>
    public static T Add<T>() where T : Component
    {
        if (gameObject.TryGetComponent(out T already)) return already;
        T result = gameObject.AddComponent<T>();
        return result;
    }
}