using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A component that marks a GameObject as being poolable.
/// </summary>
public class Spawnable : MonoBehaviour
{
    #region State Data
    /// <summary> Whether this <see cref="Spawnable"/> is currently active in the game. </summary>
    public bool Active => gameObject.activeSelf && !IsPrefab;
    /// <summary> Whether this <see cref="Spawnable"/> is inactive, isn't a prefab, and hasn't been labeled as Altered. </summary>
    public bool Ready => !Active && !IsAltered() && !Reserved;
    /// <summary> If this is true, this <see cref="Spawnable"/> is a prefab and should not be used for anything besides instantiation. </summary>
    public bool IsPrefab { get; private set; } = true;

    /// <summary>
    /// Set this to true if you want to set this <see cref="Spawnable"/> as reserved for a specific function despite being disabled, preventing it from being reused.
    /// </summary>
    public bool Reserved;

    /// <summary> The current active script client for this <see cref="Spawnable"/></summary>
    public object currentClient { set; get; }
    ///// <summary>If this Spawnable has an active association with a client. </summary>
    //public bool HasClient => currentClient != null;

    public float spawnTime { private set; get; }
    #endregion


    /// <summary>
    /// This is only to be used within Spawner Clients (Object Pools, Entity Spawns, etc.)
    /// </summary>
    /// <returns></returns>
    public static Spawnable Instantiate(GameObject prefab, Transform parent)
    {
        GameObject instance = GameObject.Instantiate(prefab);
        if (!instance.TryGetComponent(out Spawnable result))
            result = instance.AddComponent<Spawnable>();
        result.IsPrefab = false;
        return result;
    }

    public void Spawn(Placement placement)
    {
        transform.SetPositionAndRotation(placement.Position, placement.Rotation);
        gameObject.SetActive(true);
    }

    public void Despawn() => gameObject.SetActive(false);

    private void OnEnable()
    {
        onActivate?.Invoke();
        spawnTime = Time.time;
    }
    private void OnDisable() => onDeactivate?.Invoke();

    public event Action onActivate;
    public event Action onDeactivate;

    #region Alterations Management
    public void SetAlterations(Action addedUnoder)
    {
        if (addedUnoder is null) return;
        alterationsUndoer += addedUnoder;
    }
    public void ResetAlterations()
    {
        alterationsUndoer?.Invoke();
        alterationsUndoer = null;
    }
    public bool IsAltered() => alterationsUndoer is not null;
    private event Action alterationsUndoer;
    #endregion

    #region Helpers

    /// <summary>
    /// Simple function for if this <see cref="GameObject"/> is a <see cref="Spawnable"/>. <br/>
    /// Not to be confused with <see cref="IsSpawnable(GameObject)"/>, which also checks if the object instance is available for reuse.
    /// </summary>
    public static bool IsASpawnable(GameObject subject) => subject.TryGetComponent(out Spawnable _);
    /// <summary>
    /// Simple function for if this <see cref="GameObject"/> is a <see cref="Spawnable"/>. <br/>
    /// Not to be confused with <see cref="IsSpawnable(GameObject, out Spawnable)"/>, which also checks if the object instance is available for reuse.
    /// </summary>
    public static bool IsASpawnable(GameObject subject, out Spawnable result) => subject.TryGetComponent(out result);

    /// <summary>
    /// Function that checks if this <see cref="GameObject"/> is a <see cref="Spawnable"/> and if it is available for reuse. <br/>
    /// Use <see cref="IsASpawnable(GameObject)"/> if you only want to check if the object is a <see cref="Spawnable"/>.
    /// </summary>
    public static bool IsSpawnable(GameObject subject) =>
        subject.TryGetComponent(out Spawnable spawnable) && spawnable.Ready;
    /// <summary>
    /// Function that checks if this <see cref="GameObject"/> is a <see cref="Spawnable"/> and if it is available for reuse. <br/>
    /// Use <see cref="IsASpawnable(GameObject, out Spawnable)"/> if you only want to check if the object is a <see cref="Spawnable"/>.
    /// </summary>
    public static bool IsSpawnable(GameObject subject, out Spawnable spawnable) =>
        subject.TryGetComponent(out spawnable) && spawnable.Ready;

    public static void DestroyOrDisable(GameObject subject)
    {
        if (!IsASpawnable(subject, out Spawnable spawnable)) Destroy(subject);
        else spawnable.Despawn();
    }
    #endregion
}

public static class Xtensions_Spawnables
{
    public static void DestroyOrDisable(this GameObject subject) => Spawnable.DestroyOrDisable(subject);
    public static bool IsSpawnable(this GameObject subject) => Spawnable.IsSpawnable(subject);
    public static bool IsSpawnable(this GameObject subject, out Spawnable spawnable) => Spawnable.IsSpawnable(subject, out spawnable);
}