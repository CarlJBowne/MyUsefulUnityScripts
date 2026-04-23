using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// A representation of a Prefab (GameObject asset) with improved serialization and runtime/addressable friendliness.
/// Keeps a direct GameObject reference for fast in-editor usage and an asset GUID for robust serialization.
/// Optionally supports an Addressables key via <see cref="addressableKey"/>.
/// </summary>
[Serializable]
public class Prefab
{
    // Primary runtime/editor reference (serialized so inspector can show it).
    [field: SerializeField] public GameObject readOnlyObject { get; private set; } = null;

    // Friendly editor path (editor only, but stored for debugging)
    [field: SerializeField] public string path { get; private set; } = string.Empty;

    // Stable identifier for the asset (GUID) - allows robust editor-side re-resolve.
    // NOTE: GUID cannot be resolved at runtime without Addressables or embedding paths into builds.
    [field: SerializeField] public string assetGuid { get; private set; } = string.Empty;

    // Optional addressable key you can populate if using Addressables to load at runtime.
    [field: SerializeField] public string addressableKey { get; private set; } = string.Empty;

    public Prefab() { }

    public Prefab(GameObject existingInstance)
    {
        readOnlyObject = existingInstance;
#if UNITY_EDITOR
        path = AssetDatabase.GetAssetPath(existingInstance);
        assetGuid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
#endif
    }

    public Prefab(string assetPath)
    {
        path = assetPath;
#if UNITY_EDITOR
        readOnlyObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        assetGuid = string.IsNullOrEmpty(assetPath) ? string.Empty : AssetDatabase.AssetPathToGUID(assetPath);
        if (readOnlyObject == null)
            Debug.LogError($"Prefab: failed to find an asset at path: {assetPath}");
#else
        // At runtime we can't use AssetDatabase. Expect addressableKey to be used or a serialized GameObject reference.
#endif
    }

    // Tries to ensure readOnlyObject is resolved (Editor only or if addressable/other runtime resolver registered).
    public bool TryResolve()
    {
        if (readOnlyObject != null) return true;

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(assetGuid))
        {
            string p = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (!string.IsNullOrEmpty(p))
            {
                readOnlyObject = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                path = p;
                return readOnlyObject != null;
            }
        }
        if (!string.IsNullOrEmpty(path))
        {
            readOnlyObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return readOnlyObject != null;
        }
#endif

        // If Addressables are used in your project you could resolve here using Addressables.LoadAssetAsync<GameObject>(addressableKey)
        return false;
    }

    // Synchronous instantiate (returns GameObject)
    public GameObject Instantiate(Transform parent = null)
    {
        if (!TryResolve())
        {
            Debug.LogError("Prefab.Instantiate: cannot resolve prefab asset.");
            return null;
        }
        return UnityEngine.Object.Instantiate(readOnlyObject, parent);
    }

    public GameObject Instantiate(Vector3 position, Quaternion rotation = default, Transform parent = null)
    {
        if (!TryResolve())
        {
            Debug.LogError("Prefab.Instantiate: cannot resolve prefab asset.");
            return null;
        }
        return UnityEngine.Object.Instantiate(readOnlyObject, position, rotation, parent);
    }

    // Async wrappers preserve behavior of Unity's InstantiateAsync which returns an AsyncInstantiateOperation-like type in your project.
    // This file keeps the same return type used previously in your project (AsyncInstantiateOperation).
    public AsyncInstantiateOperation InstantiateAsync(Transform parent = null)
    {
        if (!TryResolve())
        {
            Debug.LogError("Prefab.InstantiateAsync: cannot resolve prefab asset.");
            return null;
        }
        return UnityEngine.Object.InstantiateAsync(readOnlyObject, parent);
    }

    public AsyncInstantiateOperation InstantiateAsync(Vector3 position, Quaternion rotation = default, Transform parent = null)
    {
        if (!TryResolve())
        {
            Debug.LogError("Prefab.InstantiateAsync: cannot resolve prefab asset.");
            return null;
        }

        var op = UnityEngine.Object.InstantiateAsync(readOnlyObject, parent);
        op.completed += _ =>
        {
            // Ensure correct transform for created instance(s)
            if (op.Result != null && op.Result.Length > 0)
                op.Result[0].transform.SetPositionAndRotation(position, rotation);
        };
        return op;
    }

    // Instantiate many (sync/async)
    public AsyncInstantiateOperation InstantiateMany(int count, Transform parent = null)
    {
        if (!TryResolve())
        {
            Debug.LogError("Prefab.InstantiateMany: cannot resolve prefab asset.");
            return null;
        }
        return UnityEngine.Object.InstantiateAsync(readOnlyObject, count, parent);
    }

    public AsyncInstantiateOperation InstantiateMany(int count, Vector3 position, Quaternion rotation = default, Transform parent = null)
    {
        if (!TryResolve())
        {
            Debug.LogError("Prefab.InstantiateMany: cannot resolve prefab asset.");
            return null;
        }
        var op = UnityEngine.Object.InstantiateAsync(readOnlyObject, count, parent);
        op.completed += _ =>
        {
            for (int i = 0; i < op.Result.Length; i++)
                op.Result[i].transform.SetPositionAndRotation(position, rotation);
        };
        return op;
    }

    public static implicit operator bool(Prefab prefab) => prefab != null && prefab.TryResolve();
    public static bool operator ==(Prefab prefab, GameObject gameObject) => prefab != null && prefab.TryResolve() && prefab.readOnlyObject == gameObject;
    public static bool operator !=(Prefab prefab, GameObject gameObject) => prefab == null || !prefab.TryResolve() || prefab.readOnlyObject != gameObject;
    public override bool Equals(object obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();

#if UNITY_EDITOR
    // Editor helpers for editing the prefab asset contents directly.
    public GameObject editableObject { get; private set; } = null;
    public bool opened { get; private set; } = false;

    public void Open()
    {
        if (opened || string.IsNullOrEmpty(path)) return;
        editableObject = PrefabUtility.LoadPrefabContents(path);
        if (editableObject == null)
        {
            Debug.LogError($"Prefab.Open: failed to open prefab at path {path}");
            return;
        }
        opened = true;
    }

    public void Close(bool withoutSaving = false)
    {
        if (!opened) return;
        if (!withoutSaving)
        {
            EditorSceneManager.MarkSceneDirty(editableObject.scene);
            PrefabUtility.SaveAsPrefabAsset(editableObject, path);
        }
        PrefabUtility.UnloadPrefabContents(editableObject);
        opened = false;
    }

    // Called by inspector/property drawer to keep GUID/path consistent.
    internal void EditorRefreshFromObjectReference()
    {
        if (readOnlyObject == null)
        {
            path = string.Empty;
            assetGuid = string.Empty;
            return;
        }
        path = AssetDatabase.GetAssetPath(readOnlyObject);
        assetGuid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
    }


#endif
}

/// <summary>
/// Strongly-typed prefab wrapper that returns components of type T on instantiate.
/// Useful for avoiding GetComponent calls after instantiate.
/// </summary>
[Serializable]
public class Prefab<T> where T : Component
{
    [SerializeField] private Prefab inner = new();

    public Prefab() { }
    public Prefab(GameObject go) => inner = new Prefab(go);
    public Prefab(string path) => inner = new Prefab(path);

    public GameObject GameObject => inner.readOnlyObject;
    public string path => inner.path;
    public string assetGuid => inner.assetGuid;
    public string addressableKey => inner.addressableKey;

    public bool TryResolve() => inner.TryResolve();

    public T Instantiate(Transform parent = null)
    {
        GameObject go = inner.Instantiate(parent);
        return go != null ? go.GetComponent<T>() : null;
    }

    public T Instantiate(Vector3 position, Quaternion rotation = default, Transform parent = null)
    {
        GameObject go = inner.Instantiate(position, rotation, parent);
        return go != null ? go.GetComponent<T>() : null;
    }

    public AsyncInstantiateOperation InstantiateAsync(Transform parent = null) => inner.InstantiateAsync(parent);

    public AsyncInstantiateOperation InstantiateAsync(Vector3 position, Quaternion rotation = default, Transform parent = null) => inner.InstantiateAsync(position, rotation, parent);

    public AsyncInstantiateOperation InstantiateMany(int count, Transform parent = null) => inner.InstantiateMany(count, parent);

    public AsyncInstantiateOperation InstantiateMany(int count, Vector3 position, Quaternion rotation = default, Transform parent = null) => inner.InstantiateMany(count, position, rotation, parent);

    public static bool operator ==(Prefab<T> prefab, GameObject gameObject) => prefab != null && prefab.TryResolve() && prefab.inner.readOnlyObject == gameObject;
    public static bool operator !=(Prefab<T> prefab, GameObject gameObject) => prefab == null || !prefab.TryResolve() || prefab.inner.readOnlyObject != gameObject;
    public override bool Equals(object obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();

#if UNITY_EDITOR
    internal void EditorRefreshFromObjectReference() => inner.EditorRefreshFromObjectReference();
#endif
}

/*
/// <summary>
/// Extensions to help integrate Prefab<> with your ObjectPool system without needing to modify the pool class.
/// Uses reflection to set auto-property backing fields on ObjectPool to avoid changing existing pool type.
/// </summary>
public static class PrefabExtensions
{
    /// <summary>
    /// Creates and configures an ObjectPool instance from a prefab asset that contains a PoolableObject component.
    /// This uses reflection to set backing fields for auto-properties on ObjectPool (safe but uses non-public backing fields).
    /// Returns null if the prefab doesn't contain PoolableObject or if resolution fails.
    /// </summary>
    public static ObjectPooling.ObjectPool CreateObjectPoolFromPoolablePrefab(this Prefab prefab,
        string poolName,
        int initialSize = 5,
        bool canGrow = true,
        bool autoEnable = true,
        float autoDisableTime = -1f,
        Transform poolParentOverride = null,
        bool orphanOnDestroy = false)
    {
        if (prefab == null || !prefab.TryResolve())
        {
            Debug.LogError("CreateObjectPoolFromPoolablePrefab: prefab not resolved.");
            return null;
        }

        var go = prefab.readOnlyObject;
        if (go == null)
        {
            Debug.LogError("CreateObjectPoolFromPoolablePrefab: prefab has no GameObject reference.");
            return null;
        }

        // Get PoolableObject component from the prefab asset itself (valid on prefab asset)
        var poolable = go.GetComponent<ObjectPooling.PoolableObject>();
        if (poolable == null)
        {
            Debug.LogError($"CreateObjectPoolFromPoolablePrefab: prefab does not contain a PoolableObject component: {go.name}");
            return null;
        }

        // Create pool instance and set internal backing fields.
        var pool = new ObjectPooling.ObjectPool();

        // Set backing fields via reflection for auto-properties with private setters.
        void SetBackingField(string propName, object value)
        {
            var field = typeof(ObjectPooling.ObjectPool).GetField($"<{propName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) field.SetValue(pool, value);
            else Debug.LogWarning($"PrefabExtensions: failed to set backing field for {propName}");
        }

        SetBackingField("name", poolName);
        SetBackingField("prefab", poolable);
        SetBackingField("initialSize", initialSize);
        SetBackingField("canGrow", canGrow);
        SetBackingField("autoEnable", autoEnable);
        SetBackingField("autoDisableTime", autoDisableTime);
        SetBackingField("poolParentOverride", poolParentOverride);
        SetBackingField("orphanOnDestroy", orphanOnDestroy);

        // poolList and data fields are NonSerialized; the pool's Initialize() will build instances.
        return pool;
    }
}
*/

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Prefab))
]
public class PrefabDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var objProp = property.FindPropertyRelative("<readOnlyObject>k__BackingField");
        var pathProp = property.FindPropertyRelative("<path>k__BackingField");
        var guidProp = property.FindPropertyRelative("<assetGuid>k__BackingField");

        string path = pathProp.stringValue;
        GUIContent labelWithTooltip = new GUIContent(label.text, string.IsNullOrEmpty(path) ? "No prefab path assigned." : path);

        EditorGUI.BeginChangeCheck();
        EditorGUI.ObjectField(position, objProp, typeof(GameObject), labelWithTooltip);
        if (EditorGUI.EndChangeCheck())
        {
            GameObject go = objProp.objectReferenceValue as GameObject;
            string newPath = go != null ? AssetDatabase.GetAssetPath(go) : string.Empty;
            pathProp.stringValue = newPath;
            guidProp.stringValue = string.IsNullOrEmpty(newPath) ? string.Empty : AssetDatabase.AssetPathToGUID(newPath);
            property.serializedObject.ApplyModifiedProperties();

            // If the Prefab class has an editor refresh helper, call it.
            var target = property.serializedObject.targetObject;
            // We don't have direct access to the instance here, but we've updated the serialized fields which is sufficient.
        }

        Rect prefabLabelRect = position;
        prefabLabelRect.xMax -= EditorGUIUtility.singleLineHeight;
        prefabLabelRect.xMin = prefabLabelRect.xMax - 60;
        EditorGUI.LabelField(prefabLabelRect, "(Prefab)");

        EditorGUI.EndProperty();
    }
}
#endif