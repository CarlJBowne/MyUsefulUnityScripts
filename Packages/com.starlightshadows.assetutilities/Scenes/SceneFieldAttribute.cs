using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class SceneFieldAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneFieldAttribute))]
public class SceneFieldDrawer : PropertyDrawer
{
    bool isInt = false;
    Foldout foldout;
    Label primaryLabel;
    Image icon;
    ObjectField objectField;
    Label detailDisplay;
    TextField nameField;
    IntegerField indexField;

    // Plan / Pseudocode (detailed):
    // 1. Determine whether the target serialized property is an int (build index) or string (scene path).
    // 2. Create UIElements (Foldout, primary label row with icon and ObjectField, detailLabel).
    // 3. Create helper functions:
    //    - LoadSceneAssetFromPath(path): returns SceneAsset or null
    //    - FindBuildIndexForPath(path): returns index or -1
    //    - GetSceneNameFromPath(path): returns filename without extension
    //    - DetermineSceneState(asset): uses asset -> path -> build list to return SceneState (Null, NotInList, InListButDisabled, Valid)
    //    - UpdateUIForState(state, optional path/index/name): sets icon.image via GetIcon(state) and detailDisplay.text via GetText(state)
    // 4. Initialize controls differently for string-backed and int-backed properties:
    //    - Set objectField.value based on the current property value.
    //    - Update nameField/indexField values.
    //    - Determine initial SceneState via DetermineSceneState(...) and call UpdateUIForState(...)
    // 5. Implement a single local function `OnObjectFieldChanged(ChangeEvent<Object> evt)`:
    //    - Guard with suppressCallbacks.
    //    - Load asset and path from evt.newValue.
    //    - For string properties: set property.stringValue = path.
    //    - For int properties: set property.intValue = FindBuildIndexForPath(path).
    //    - Update corresponding nameField/indexField values.
    //    - Determine new SceneState and call UpdateUIForState(...)
    //    - ApplyModifiedProperties on the serialized object.
    // 6. Register `OnObjectFieldChanged` with `objectField.RegisterValueChangedCallback`.
    // 7. Return the foldout visual element.
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        isInt = property.propertyType is SerializedPropertyType.String ? false
            : property.propertyType is SerializedPropertyType.Integer ? true
            : throw new Exception("Scene Field Attribute can only be attached to fields of type string or int.");

        foldout = new();
        foldout.text = property.displayName;
        foldout.BindProperty(property);

        // Primary label row
        primaryLabel = foldout.Q<Label>();
        if (primaryLabel == null)
        {
            primaryLabel = new Label();
            foldout.Add(primaryLabel);
        }
        primaryLabel.style.flexDirection = FlexDirection.Row;
        primaryLabel.style.width = 40;
        primaryLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

        icon = new Image();
        icon.style.marginLeft = 128;
        icon.style.marginRight = 4;
        icon.style.width = 16;
        icon.style.height = 16;
        primaryLabel.Add(icon);

        objectField = new ObjectField();
        objectField.objectType = typeof(SceneAsset);
        objectField.allowSceneObjects = false;
        objectField.style.flexGrow = 1;
        primaryLabel.Add(objectField);

        // Details area
        detailDisplay = new Label();
        detailDisplay.style.unityTextAlign = TextAnchor.MiddleLeft;
        detailDisplay.style.marginTop = 4;
        foldout.Add(detailDisplay);

        bool suppressCallbacks = false;

        // Helper local functions
        SceneAsset LoadSceneAssetFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        int FindBuildIndexForPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return -1;
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (string.Equals(scenes[i].path, path, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        string GetSceneNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return Path.GetFileNameWithoutExtension(path);
        }


        SceneState DetermineSceneState(SceneAsset asset)
        {
            if (asset == null) return SceneState.Null;
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return SceneState.Null;

            int idx = FindBuildIndexForPath(path);
            if (idx < 0) return SceneState.NotInList;

            var scenes = EditorBuildSettings.scenes;
            if (idx >= 0 && idx < scenes.Length)
            {
                return scenes[idx].enabled ? SceneState.Valid : SceneState.InListButDisabled;
            }

            return SceneState.NotInList;
        }

        void UpdateUIForState(SceneState state)
        {
            icon.image = GetIcon(state);
            detailDisplay.text = GetText(state);
        }

        // Initialize and wire specific controls
        if (!isInt)
        {
            nameField = new TextField("Scene Name");
            nameField.style.marginTop = 4;
            foldout.Add(nameField);

            // Initialize from property
            nameField.value = property.stringValue ?? string.Empty;

            // Set objectField based on path if possible
            var initialAsset = LoadSceneAssetFromPath(property.stringValue);
            objectField.value = initialAsset;

            // Determine and display state
            var initState = DetermineSceneState(initialAsset);
            UpdateUIForState(initState);

            nameField.SetEnabled(false);

            objectField.RegisterValueChangedCallback(OnObjectFieldChanged);
        }
        else
        {
            indexField = new IntegerField("Build Index");
            indexField.style.marginTop = 4;
            foldout.Add(indexField);

            // Initialize from property
            indexField.value = property.intValue;

            // Try to set objectField from build settings
            var scenes = EditorBuildSettings.scenes;
            if (property.intValue >= 0 && property.intValue < scenes.Length)
            {
                var path = scenes[property.intValue].path;
                var asset = LoadSceneAssetFromPath(path);
                objectField.value = asset;

                // Determine and display state
                var initState = DetermineSceneState(asset);
                UpdateUIForState(initState);
            }
            else
            {
                objectField.value = null;
                var initState = DetermineSceneState(null);
                UpdateUIForState(initState);
            }

            indexField.SetEnabled(false);

            objectField.RegisterValueChangedCallback(OnObjectFieldChanged);
        }

        // Object field change handler pulled out as a separate function.
        void OnObjectFieldChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            if (suppressCallbacks) return;
            suppressCallbacks = true;

            var asset = evt.newValue as SceneAsset;
            string path = asset ? AssetDatabase.GetAssetPath(asset) : string.Empty;

            property.serializedObject.Update();
            if (!isInt)
            {
                property.stringValue = path ?? string.Empty;
                // update displayed name/path
                if (nameField != null)
                    nameField.value = property.stringValue ?? string.Empty;
            }
            else
            {
                int idx = FindBuildIndexForPath(path);
                property.intValue = idx;
                if (indexField != null)
                    indexField.value = idx;
            }
            property.serializedObject.ApplyModifiedProperties();

            // Determine scene state from the selected asset and update UI via GetIcon/GetText
            var state = DetermineSceneState(asset);
            UpdateUIForState(state);

            suppressCallbacks = false;
        }

        return foldout;
    }

    Texture2D GetIcon(SceneState state)
    {
        return state switch
        {
            SceneState.Null => EditorGUIUtility.IconContent("console.erroricon").image as Texture2D,
            SceneState.NotInList => EditorGUIUtility.IconContent("console.warnicon").image as Texture2D,
            SceneState.InListButDisabled => EditorGUIUtility.IconContent("console.warnicon").image as Texture2D,
            SceneState.Valid => EditorGUIUtility.IconContent("TestPassed").image as Texture2D,
            _ => throw new NotImplementedException(),
        };
    }
    string GetText(SceneState state)
    {
        return state switch
        {
            SceneState.Null => "This Scene Reference is Null. Ensure it is filled with a valid scene before use.",
            SceneState.NotInList => "This Scene is not in the Build List. Click to open Build Settings.",
            SceneState.InListButDisabled => "This Scene is in the Build List, but is not enabled. Click to open Build Settings.",
            SceneState.Valid => "This Scene is validly set up.",
            _ => throw new NotImplementedException(),
        };
    }

    enum SceneState
    {
        Null,
        NotInList,
        InListButDisabled,
        Valid
    }
}
#endif