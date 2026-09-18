using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using System.Reflection;


#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using SLS.EditorUtilities.Editor;
#endif

[System.Serializable]
public class SceneReference : ISerializationCallbackReceiver
{

    [field: SerializeField] public string sceneName { get; private set; }

    public static implicit operator SceneReference(string s) => new(s);
    public static implicit operator string(SceneReference R) => R.sceneName;
    public static implicit operator bool(SceneReference R) =>
        !string.IsNullOrEmpty(R.sceneName) && R.sceneName != null && R.sceneName != "";


    public SceneReference(string sceneName) => this.sceneName = sceneName;


    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        ValidateSerialized();
#endif
    }
    public void OnAfterDeserialize() { }

#if UNITY_EDITOR

    private enum SceneRefState
    {
        Null,
        NotInList,
        InListButDisabled,
        Valid,
    }

    [field: SerializeField] public UnityEngine.Object asset { get; private set; }

    public SceneReference(UnityEngine.Object sceneAsset)
    {
        asset = sceneAsset;
        ValidateSerialized();
    }

    internal void ValidateSerialized()
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

    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        // Immediate-mode fallback kept minimal for compatibility.
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var assetProp = property.FindPropertyRelative($"<{nameof(SceneReference.asset)}>k__BackingField");
            var sceneNameProp = property.FindPropertyRelative($"<{nameof(SceneReference.sceneName)}>k__BackingField");

            // Layout: left = label, middle = small icon, right = object field
            float fullWidth = position.width;
            float labelWidth = EditorGUIUtility.labelWidth;
            float iconSize = EditorGUIUtility.singleLineHeight;
            float objFieldWidth = fullWidth - labelWidth - iconSize - 6f;

            Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            Rect objRect = new Rect(position.x + labelWidth, position.y, objFieldWidth, position.height);
            Rect iconRect = new Rect(objRect.x + objRect.width + 4f, position.y, iconSize, iconSize);

            EditorGUI.LabelField(labelRect, label);

            EditorGUI.BeginChangeCheck();
            var newAsset = EditorGUI.ObjectField(objRect, GUIContent.none, assetProp.objectReferenceValue, typeof(UnityEditor.SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.Update();
                assetProp.objectReferenceValue = newAsset;

                // Update sceneName directly via serialized property (no reflection)
                if (newAsset != null)
                {
                    string path = AssetDatabase.GetAssetPath(newAsset);
                    if (!string.IsNullOrEmpty(path) && path.EndsWith(".unity"))
                    {
                        sceneNameProp.stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
                    }
                    else
                    {
                        sceneNameProp.stringValue = null;
                    }
                }
                else
                {
                    sceneNameProp.stringValue = null;
                }

                property.serializedObject.ApplyModifiedProperties();
            }

            // Determine state and icon
            var state = GetSceneState(assetProp.objectReferenceValue, out string tooltip);
            Texture2D icon = GetIconForState(state);

            GUIContent iconContent = new GUIContent(icon, tooltip);
            if (GUI.Button(iconRect, iconContent, GUIStyle.none))
            {
                if (state == SceneRefState.NotInList || state == SceneRefState.InListButDisabled)
                {
                    var buildWindowType = System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor");
                    if (buildWindowType != null) EditorWindow.GetWindow(buildWindowType);
                }
            }

            // Foldout details area
            bool detailsShown = EditorPrefs.GetBool("SceneReference_DetailsShow", true);
            Rect foldoutRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
            detailsShown = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), detailsShown, GUIContent.none, true);
            EditorPrefs.SetBool("SceneReference_DetailsShow", detailsShown);

            if (detailsShown)
            {
                EditorGUI.indentLevel++;
                Rect detailRect = foldoutRect;
                detailRect.y += 2;
                detailRect.height = EditorGUIUtility.singleLineHeight;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.TextField(detailRect, "Scene Name:", sceneNameProp.stringValue);
                EditorGUI.EndDisabledGroup();

                detailRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(detailRect, tooltip);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        // UIElements drawer implementing the exact requested layout
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Foldout foldout = new()
            {
                text = property.displayName,
                value = EditorPrefs.GetBool("SceneReference_DetailsShow", true)
            };
            foldout.BindProperty(property);
            foldout.SetupHeader(out VisualElement header);
            foldout.RegisterValueChangedCallback(evt => EditorPrefs.SetBool("SceneReference_DetailsShow", evt.newValue));

            // ObjectField (right of name)
            var assetProp = property.FindPropertyRelative
                ($"<{nameof(SceneReference.asset)}>k__BackingField");
            var sceneNameProp = property.FindPropertyRelative
                ($"<{nameof(SceneReference.sceneName)}>k__BackingField");

            ObjectField objField = new()
            {
                objectType = typeof(UnityEditor.SceneAsset),
                allowSceneObjects = false,
                style =
                {
                    flexGrow = 1,
                    marginTop = 0,
                    marginBottom = 0,
                },
                value = assetProp.objectReferenceValue
            };

            // Small icon slot to the left of the object field
            Image stateIcon = new()
            {
                style =
                {
                    width = 18,
                    height = 18,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center
                }
            };

            header.Add(stateIcon);
            header.Add(objField);

            var sceneNameField = new TextField("Scene Name");
            sceneNameField.SetEnabled(false);
            sceneNameField.style.marginBottom = 2;

            Label tooltipLabel = new()
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };

            foldout.Add(sceneNameField);
            foldout.Add(tooltipLabel);

            // Helper to refresh visuals and serialized values (no reflection)
            void Refresh()
            {
                UnityEngine.Object curAsset = assetProp.objectReferenceValue;
                objField.SetValueWithoutNotify(curAsset);

                var state = GetSceneState(curAsset, out string stateTooltip);
                var icon = GetIconForState(state);
                stateIcon.Clear();

                Image img = new()
                {
                    image = icon,
                    scaleMode = ScaleMode.ScaleToFit,
                    style =
                    {
                        width = 16,
                        height = 16,
                    },
                    tooltip = stateTooltip
                };
                stateIcon.tooltip = stateTooltip;
                stateIcon.Add(img);

                // Set scene name text
                var sName = sceneNameProp.stringValue;
                sceneNameField.SetValueWithoutNotify(sName ?? "");

                tooltipLabel.text = stateTooltip;
            }

            // When user changes ObjectField in UIElements, update serialized properties safely
            objField.RegisterValueChangedCallback(evt =>
            {
                property.serializedObject.Update();
                assetProp.objectReferenceValue = evt.newValue;

                // Update sceneName serialized property directly
                if (evt.newValue != null)
                {
                    string path = AssetDatabase.GetAssetPath(evt.newValue);
                    sceneNameProp.stringValue = !string.IsNullOrEmpty(path) && path.EndsWith(".unity") ? System.IO.Path.GetFileNameWithoutExtension(path) : null;
                }
                else
                {
                    sceneNameProp.stringValue = null;
                }

                property.serializedObject.ApplyModifiedProperties();
                Refresh();
            });

            // Icon click opens Build Settings when appropriate
            stateIcon.RegisterCallback<ClickEvent>(e =>
            {
                var state = GetSceneState(assetProp.objectReferenceValue, out _);
                if (state == SceneRefState.NotInList || state == SceneRefState.InListButDisabled)
                {
                    var buildWindowType = System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor");
                    if (buildWindowType != null) EditorWindow.GetWindow(buildWindowType);
                }
            });

            // Keep UI in sync with serialized property (undo/redo)
            property.serializedObject.Update();
            Refresh();
            property.serializedObject.ApplyModifiedProperties();

            return foldout;
        }

        // Utility: returns state and tooltip for given asset
        private static SceneRefState GetSceneState(UnityEngine.Object asset, out string tooltip)
        {
            tooltip = "";
            if (asset == null)
            {
                tooltip = "This Scene Reference is Null. Ensure it is filled with a valid scene before use.";
                return SceneRefState.Null;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                tooltip = "Invalid scene asset.";
                return SceneRefState.Null;
            }

            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == path)
                {
                    if (scenes[i].enabled)
                    {
                        tooltip = "This Scene is validly set up.";
                        return SceneRefState.Valid;
                    }
                    else
                    {
                        tooltip = "This Scene is in the Build List, but is not enabled. Click to open Build Settings.";
                        return SceneRefState.InListButDisabled;
                    }
                }
            }

            tooltip = "This Scene is not in the Build List. Click to open Build Settings.";
            return SceneRefState.NotInList;
        }

        // Utility: pick an editor icon for each state
        private static Texture2D GetIconForState(SceneRefState state)
        {
            switch (state)
            {
                case SceneRefState.Null:
                    return EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
                case SceneRefState.NotInList:
                case SceneRefState.InListButDisabled:
                    return EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                case SceneRefState.Valid:
                    return EditorGUIUtility.IconContent("TestPassed").image as Texture2D;
                default:
                    return null;
            }
        }
    }

#endif
}