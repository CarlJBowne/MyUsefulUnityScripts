using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.SceneManagement;

/// <summary>
/// A representation of a Prefab.
/// </summary>
[System.Serializable]
public class Prefab
{
    [field: SerializeField] public GameObject readOnlyObject { get; private set; } = null;
    [field: SerializeField] public string path { get; private set; } = string.Empty;


    public Prefab(GameObject existingInstance)
    {
        readOnlyObject = existingInstance;
    }
    public Prefab(string path)
    {
        this.path = path;
        readOnlyObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (readOnlyObject == null)
        {
            Debug.LogError($"Failed to find a prefab at path: {path}");
            return;
        }
    }



    public GameObject Instantiate(Transform parent = null)
    {
        if (readOnlyObject == null)
        {
            Debug.LogError("Cannot instantiate a prefab that has not been assigned a valid GameObject.");
            return null;
        }
        return GameObject.Instantiate(readOnlyObject, parent);
    }
    public GameObject Instantiate(Vector3 position, Quaternion rotation = default, Transform parent = null)
    {
        if (readOnlyObject == null)
        {
            Debug.LogError("Cannot instantiate a prefab that has not been assigned a valid GameObject.");
            return null;
        }
        return GameObject.Instantiate(readOnlyObject, position, rotation, parent);
    }
    public AsyncInstantiateOperation InstantiateAsync(Transform parent = null)
    {
        if (readOnlyObject == null)
        {
            Debug.LogError("Cannot instantiate a prefab that has not been assigned a valid GameObject.");
            return null;
        }
        return UnityEngine.Object.InstantiateAsync(readOnlyObject, parent);
    }
    public AsyncInstantiateOperation InstantiateAsync(Vector3 position, Quaternion rotation = default, Transform parent = null)
    {
        if (readOnlyObject == null)
        {
            Debug.LogError("Cannot instantiate a prefab that has not been assigned a valid GameObject.");
            return null;
        }
        var op = UnityEngine.Object.InstantiateAsync(readOnlyObject, parent);
        op.completed += _ => { op.Result[0].transform.SetPositionAndRotation(position, rotation); };
        return op;
    }

    public AsyncInstantiateOperation InstantiateMany(int count, Transform parent = null)
    {
        if (readOnlyObject == null)
        {
            Debug.LogError("Cannot instantiate a prefab that has not been assigned a valid GameObject.");
            return null;
        }
        return UnityEngine.Object.InstantiateAsync(readOnlyObject, count, parent);
    }
    public AsyncInstantiateOperation InstantiateMany(int count, Vector3 position, Quaternion rotation = default, Transform parent = null)
    {
        if (readOnlyObject == null)
        {
            Debug.LogError("Cannot instantiate a prefab that has not been assigned a valid GameObject.");
            return null;
        }
        var op = UnityEngine.Object.InstantiateAsync(readOnlyObject, count, parent);
        op.completed += _ =>
        {
            for (int i = 0; i < op.Result.Length; i++) op.Result[i].transform.SetPositionAndRotation(position, rotation);
        };
        return op;
    }


#if UNITY_EDITOR

    public GameObject editableObject { get; private set; } = null;
    public bool opened { get; private set; } = false;

    public Prefab(GameObject existingInstance, bool openForEditing = false)
    {
        readOnlyObject = existingInstance;
        path = AssetDatabase.GetAssetPath(existingInstance);
        if (!string.IsNullOrEmpty(path) && openForEditing) Open();
    }
    public Prefab(string path, bool openForEditing = false)
    {
        this.path = path;
        readOnlyObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (readOnlyObject == null)
        {
            Debug.LogError($"Failed to find a prefab at path: {path}");
            return;
        }
        if (openForEditing) Open();
    }


    public void Open()
    {
        if (opened) return;
        editableObject = PrefabUtility.LoadPrefabContents(path);
        if (editableObject == null)
        {
            Debug.LogError($"Failed to open prefab at path: {path}");
            return;
        }
        opened = true;
    }

    public void Close(bool withoutSaving = false)
    {
        if (!opened) return;
        if (!withoutSaving)
        {
            EditorSceneManager.MarkSceneDirty(editableObject.scene); // Mark the scene as dirty  
            PrefabUtility.SaveAsPrefabAsset(editableObject, path);
        }
        PrefabUtility.UnloadPrefabContents(editableObject);
        opened = false;
    }
#endif

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Prefab))]
public class PrefabDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get the GameObject reference and path properties
        var objProp = property.FindPropertyRelative("<readOnlyObject>k__BackingField");
        var pathProp = property.FindPropertyRelative("<path>k__BackingField");

        // Show the path as a tooltip on the label
        string path = pathProp.stringValue;
        GUIContent labelWithTooltip = new(label.text, string.IsNullOrEmpty(path) ? "No prefab path assigned." : path);

        EditorGUI.BeginChangeCheck();
        EditorGUI.ObjectField(position, objProp, typeof(GameObject), labelWithTooltip);
        if (EditorGUI.EndChangeCheck())
        {
            GameObject go = objProp.objectReferenceValue as GameObject;
            string newPath = go != null ? AssetDatabase.GetAssetPath(go) : string.Empty;
            pathProp.stringValue = newPath;
            property.serializedObject.ApplyModifiedProperties();
        }

        Rect prefabLabelRect = position;
        prefabLabelRect.xMax -= EditorGUIUtility.singleLineHeight;
        prefabLabelRect.xMin = prefabLabelRect.xMax - 60;
        EditorGUI.LabelField(prefabLabelRect, "(Prefab)");

        EditorGUI.EndProperty();
    }
}


#endif