using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;


#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;

public class ComponentConfig
{
    private static bool showConfig = true;
    public static bool ShowConfig
    {
        get => showConfig;
        set
        {
            showConfig = value;
            OnShowConfigChanged?.Invoke(showConfig);
        }
    }
    public static Action<bool> OnShowConfigChanged;

    // Shared helper: centralizes logic to find a component of the given Type on the given MonoBehaviour's GameObject.
    // Returns the found Component or null. Does not log errors about missing required components (caller handles that).
    public static Component GetRelatedComponent(MonoBehaviour target, System.Type componentType, string subDirectory, bool addIfNotFound = false)
    {
        GameObject foundSubTarget = null;
        if(subDirectory != null)
        {
            string[] directory = subDirectory.Split('/');
            foundSubTarget = target.gameObject;

            foreach (var d in directory)
            {
                Transform child = foundSubTarget.transform.Find(d);
                if(child != null) foundSubTarget = child.gameObject;
                else
                {
                    foundSubTarget = null;
                    break;
                }
            }
        }

        Component result = null;

        if(subDirectory != null && foundSubTarget)
        {
            result = foundSubTarget.GetComponent(componentType);
            if (result) return result;

            if (addIfNotFound)
            {
                result = foundSubTarget.AddComponent(componentType);
                Undo.RegisterCreatedObjectUndo(result, "Add Related Component");
                return result;
            }
            else
            {
                result = target.GetComponent(componentType);
                return result;
            }
        }
        else
        {
            if (addIfNotFound)
            {
                result = target.gameObject.AddComponent(componentType);
                Undo.RegisterCreatedObjectUndo(result, "Add Related Component");
                return result;
            }
            else
            {
                result = target.GetComponent(componentType);
                return result;
            }

        }

    }


    public static void Reset(MonoBehaviour target)
    {
        //Run through all fields with RelatedComponentAttribute

        var fields = target.GetType().GetFields();
        foreach (var field in fields)
        {
            var attrList = (Attribute[])field.GetCustomAttributes(typeof(RelatedComponentAttribute), true);
            foreach (Attribute item in attrList)
            {
                if(item is not RelatedComponentAttribute attributeValue) continue;

                var fieldType = field.FieldType;
                if (typeof(Component).IsAssignableFrom(fieldType))
                {
                    var GetComp = GetRelatedComponent(target, fieldType, attributeValue.subLocation, attributeValue.require);
                    if (GetComp != null)
                    {
                        field.SetValue(target, GetComp);
                    }
                    else if (attributeValue.require)
                    {
                        var addComp = target.gameObject.AddComponent(fieldType);
                        field.SetValue(target, addComp);
                    }
                }
                break;
            }
        }
    }

    [MenuItem("Tools/Toggle Component Config")]
    public static void ToggleSetup()
    {
        ShowConfig = !ShowConfig;
    }

}

/// <summary>
/// <br/> Shows this field in the editor only when the global "Show Config" option is enabled. Go to "Tools" to toggle.
/// <br/> Also adds a "Get" button to auto-assign the component from the same GameObject.
/// <br/> Also includes a "Required" toggle to mark required components and log errors if not found.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Field)]
public class RelatedComponentAttribute : PropertyAttribute
{
    public bool require;
    public string subLocation;
    public RelatedComponentAttribute(bool require = false, string subLocation = null) 
    { 
        this.require = require; 
        this.subLocation = subLocation; 
   
    }
    public RelatedComponentAttribute(string subLocation) 
    { 
        this.require = false; 
        this.subLocation = subLocation;
    }

    // Enable the drawer for child properties (array elements) too
    [CustomPropertyDrawer(typeof(RelatedComponentAttribute), true)]
    public class Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create a container that can be shown/hidden dynamically
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.display = ComponentConfig.ShowConfig ? DisplayStyle.Flex : DisplayStyle.None;

            // WARNING ICON: appears to the left when required and null
            var icon = new Image();
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.marginRight = 4;
            icon.style.alignSelf = Align.Center;
            icon.tooltip = "Required";
            // Try to resolve a warning texture from common editor icon names
            Texture2D warnTex = null;
            try
            {
                warnTex = EditorGUIUtility.IconContent("console.erroricon")?.image as Texture2D
                          ?? EditorGUIUtility.IconContent("console.warn")?.image as Texture2D
                          ?? EditorGUIUtility.IconContent("console.warnicon")?.image as Texture2D
                          ?? EditorGUIUtility.FindTexture("console.warn")
                          ?? EditorGUIUtility.FindTexture("console.warnicon")
                          ?? EditorGUIUtility.IconContent("Warning")?.image as Texture2D;
            }
            catch
            {
                warnTex = null;
            }
            icon.image = warnTex;
            icon.scaleMode = ScaleMode.ScaleToFit;

            // Determine initial visibility: only if attribute.require is true AND property is null
            var relatedAttr = attribute as RelatedComponentAttribute;
            bool isRequired = relatedAttr != null && relatedAttr.require;
            bool isNull = property != null && property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null;
            icon.style.display = (isRequired && isNull) ? DisplayStyle.Flex : DisplayStyle.None;

            // Create the default property field
            var fieldElement = new PropertyField(property);
            // Ensure the field grows to take available space
            var fieldContainer = new VisualElement();
            fieldContainer.style.flexGrow = 1;
            fieldContainer.Add(fieldElement);

            // Create a small button to the right of the field
            var button = new Button(() => { GetComponent(property); }) { text = "Get" };
            button.style.width = 56;
            button.style.marginLeft = 4;
            button.style.flexShrink = 0;
            button.style.alignSelf = Align.Center;

            // Add the icon first so it appears to the left of the slot
            container.Add(icon);
            container.Add(fieldContainer);
            container.Add(button);

            // Contextual menu on the field: Get and Turn Off Config
            fieldElement.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.AppendAction("Get", action => { GetComponent(property); });
                evt.menu.AppendAction("Hide Config", action => { ComponentConfig.ShowConfig = false; });
            });

            // Subscribe to global show/hide changes so the property toggles visibility dynamically
            Action<bool> handler = (visible) =>
            {
                // UIElements callbacks must run on the UI thread; setting style is fine here
                container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            };
            ComponentConfig.OnShowConfigChanged += handler;

            // Update icon visibility on editor updates so changes in the inspector are reflected
            EditorApplication.CallbackFunction updateCallback = null;
            updateCallback = () =>
            {
                try
                {
                    bool currentlyNull = property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null;
                    bool shouldShowIcon = (relatedAttr != null && relatedAttr.require) && currentlyNull && ComponentConfig.ShowConfig;
                    icon.style.display = shouldShowIcon ? DisplayStyle.Flex : DisplayStyle.None;
                }
                catch
                {
                    // property can be invalid during domain reloads; ignore
                }
            };
            EditorApplication.update += updateCallback;

            // Unsubscribe when element is detached to avoid leaking the handler
            container.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                ComponentConfig.OnShowConfigChanged -= handler;
                if (updateCallback != null)
                {
                    EditorApplication.update -= updateCallback;
                    updateCallback = null;
                }
            });

            return container;
        }

        private void GetComponent(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject as MonoBehaviour;
            if (targetObject == null) return;
            Type componentType = fieldInfo.FieldType;


            // Access the RelatedComponentAttribute instance (if present) for extra data (e.g. subLocation)
            var relatedAttr = attribute as RelatedComponentAttribute;
            string subLocation = relatedAttr?.subLocation;

            // Use the centralized lookup with optional sub-location
            var comp = ComponentConfig.GetRelatedComponent(targetObject, componentType, subLocation, false);
            if (comp != null)
            {
                property.objectReferenceValue = comp;
                property.serializedObject.ApplyModifiedProperties();
            }
            else if(relatedAttr.require)
            {
                //Show Dialouge popup to ask the user if they'd like to add the component, since it wasn't found.
                if (EditorUtility.DisplayDialog("Component Not Found", $"The required component of type '{componentType.Name}' was not found on GameObject '{targetObject.gameObject.name}' and is required for this to work. Would you like to add it?", "Yes", "No"))
                {
                    var addedComp = ComponentConfig.GetRelatedComponent(targetObject, componentType, subLocation, true);
                    property.objectReferenceValue = addedComp;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

}

/// <summary>
/// Shows this field in the editor only when the global "Show Config" option is enabled. Go to "Tools" to toggle.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Field)]
public class ConfigOnlyAttribute : PropertyAttribute
{
    public ConfigOnlyAttribute() { }

    // Enable the drawer for child properties (array elements) too
    [CustomPropertyDrawer(typeof(ConfigOnlyAttribute), true)]
    public class Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create a container that can be shown/hidden dynamically
            var container = new VisualElement();
            container.style.display = ComponentConfig.ShowConfig ? DisplayStyle.Flex : DisplayStyle.None;
            // Create the default property field
            var fieldElement = new PropertyField(property);
            container.Add(fieldElement);
            // Subscribe to global show/hide changes so the property toggles visibility dynamically
            Action<bool> handler = (visible) =>
            {
                // UIElements callbacks must run on the UI thread; setting style is fine here
                container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            };
            ComponentConfig.OnShowConfigChanged += handler;
            // Unsubscribe when element is detached to avoid leaking the handler
            container.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                ComponentConfig.OnShowConfigChanged -= handler;
            });
            return container;
        }
    }

}

[System.AttributeUsage(System.AttributeTargets.Field)]
public class HideAttribute : PropertyAttribute
{
    public HideAttribute() { }
    // Enable the drawer for child properties (array elements) too
    [CustomPropertyDrawer(typeof(HideAttribute), true)]
    public class Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create a container that is always hidden
            var container = new VisualElement();
            container.style.display = DisplayStyle.None;
            return container;
        }
    }
}

#else

public class ComponentConfig
{
    public static bool ShowConfig = true;

    // Shared helper for runtime/non-editor builds as well.
    public static Component GetRelatedComponent(MonoBehaviour target, System.Type componentType)
    {
        if (target == null || componentType == null) return null;
        if (!typeof(Component).IsAssignableFrom(componentType)) return null;
        try
        {
            return target.GetComponent(componentType);
        }
        catch
        {
            return null;
        }
    }

    public static void Reset(MonoBehaviour target)
    {}
}
#endif