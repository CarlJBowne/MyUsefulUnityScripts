using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SceneReference : ISerializationCallbackReceiver
{

    [field: SerializeField] public string sceneName { get; private set; }

    public static implicit operator string(SceneReference R) => R.sceneName;

#if UNITY_EDITOR 

    [field: SerializeField] public UnityEngine.Object asset { get; private set; }

    public SceneReference(UnityEngine.Object sceneAsset)
    {
        asset = sceneAsset;
        ValidateSerialized();
    }

    public void ValidateSerialized()
    {
        sceneName = null;

        if (asset == null) return;

        string path = AssetDatabase.GetAssetPath(asset);
        if (!path.EndsWith(".unity")) throw new System.ArgumentException("Error 1 : SceneObject constructor expects a scene asset.");
        sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        int buildIndex = 0;
        for (int i = 0, disableds = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == path)
            {
                buildIndex = i - disableds;
                break;
            }
        }
    }

#endif

    public SceneReference(string sceneName) => this.sceneName = sceneName;


    public void OnBeforeSerialize() => ValidateSerialized();
    public void OnAfterDeserialize() { }


}

public enum SceneState
{
    NULL = -2,
    INVALID = -1,
    Valid = 0,
    Loaded = 1,
    Loading = 2,
    Unloading = 3
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferenceDrawer : PropertyDrawer
{
    private enum SceneRefState
    {
        Null,
        NotInList,
        InListButDisabled,
        Valid,
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);



        Rect iconRect = new Rect(
            position.x + EditorGUIUtility.labelWidth - EditorGUIUtility.singleLineHeight,
            position.y,
            EditorGUIUtility.singleLineHeight,
            EditorGUIUtility.singleLineHeight
        );
        Rect detailsRect = new(
            position.x,
            position.y + EditorGUIUtility.singleLineHeight,
            position.width,
            EditorGUIUtility.singleLineHeight * 2
            );


        SerializedProperty assetProp = property.FindPropertyRelative($"<{nameof(SceneReference.asset)}>k__BackingField");
        EditorGUI.BeginChangeCheck();
        var asset = EditorGUI.ObjectField(
            position,
            label,
            assetProp.objectReferenceValue,
            typeof(UnityEditor.SceneAsset),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            property.serializedObject.Update();
            assetProp.objectReferenceValue = asset; 
            property.serializedObject.ApplyModifiedProperties();

            // Use reflection to call ValidateAsset() on the SceneReference instance
            var sceneRef = GetTargetObjectOfProperty(property) as SceneReference;
            if (sceneRef != null)
            {
                var method = typeof(SceneReference).GetMethod(nameof(SceneReference.ValidateSerialized), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null) method.Invoke(sceneRef, null);
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        // Icon and Tooltip
        SceneRefState state = SceneRefState.Null;
        string scenePath = AssetDatabase.GetAssetPath(asset);

        if (asset != null && !string.IsNullOrEmpty(scenePath))
        {
            state = SceneRefState.NotInList;
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    state = scenes[i].enabled ? SceneRefState.Valid : SceneRefState.InListButDisabled;
                    break;
                }
            }
        }

        string tooltip = "";
        Texture2D icon = null;

        switch (state)
        {
            case SceneRefState.Null:
                tooltip = "This Scene Reference is Null. Ensure it is filled with a valid scene before use.";
                icon = EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
                break;
            case SceneRefState.NotInList:
                tooltip = "This Scene is not in the Build List. Click to open Build Settings.";
                icon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                break;
            case SceneRefState.InListButDisabled:
                tooltip = "This Scene is in the Build List, but is not enabled. Click to open Build Settings.";
                icon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                break;
            case SceneRefState.Valid:
                tooltip = "This Scene is validly set up.";
                icon = EditorGUIUtility.IconContent("TestPassed").image as Texture2D;
                break;
        }

        GUIContent iconContent = new(icon, tooltip);
        if (GUI.Button(iconRect, iconContent, GUIStyle.none))
            if (state is SceneRefState.NotInList or SceneRefState.InListButDisabled)
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));



        bool sceneReferenceDetailsShow = EditorPrefs.GetBool("SceneReference_DetailsShow", true);
        GUIContent dropdownIcon = new(EditorGUIUtility.IconContent(sceneReferenceDetailsShow ? "IN Foldout on" : "IN Foldout").image);
        sceneReferenceDetailsShow = EditorGUI.Foldout(position, sceneReferenceDetailsShow, "", true);
        EditorPrefs.SetBool("SceneReference_DetailsShow", sceneReferenceDetailsShow);



        // Draw dropdown if open
        if (sceneReferenceDetailsShow)
        {
            EditorGUILayout.Space(detailsRect.height);
            EditorGUI.indentLevel++;
            Rect detailRect = position;
            detailRect.y += EditorGUIUtility.singleLineHeight + 2;
            detailRect.height = EditorGUIUtility.singleLineHeight;


            EditorGUI.BeginDisabledGroup(true);
            // Show SceneReference data
            EditorGUI.TextField(detailRect, "Scene Name:", property.FindPropertyRelative(
                $"<{nameof(SceneReference.sceneName)}>k__BackingField").stringValue);
            detailRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.EndDisabledGroup();
            EditorGUI.LabelField(detailRect, tooltip);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public static object GetTargetObjectOfProperty(SerializedProperty prop)
    {
        if (prop == null) return null;

        string[] path = prop.propertyPath.Replace(".Array.data[", "[")
            .Split('.');
        object obj = prop.serializedObject.targetObject;
        foreach (string element in path)
        {
            if (element.Contains("["))
            {
                string elementName = element.Substring(0, element.IndexOf("["));
                int index = Convert.ToInt32(
                    element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", "")
                );
                var field = obj.GetType().GetField(elementName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var list = field.GetValue(obj) as IList;
                obj = list[index];
            }
            else
            {
                var field = obj.GetType().GetField(element, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                obj = field.GetValue(obj);
            }
        }
        return obj;
    }

}


#endif