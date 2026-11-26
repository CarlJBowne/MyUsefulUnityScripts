using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.Linq;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace SLS.EditorUtilities.ComponentHeaders
{
    /// <summary>
    /// Places this field into a custom header on any <see cref="MonoBehaviour"/>
    /// </summary>
    public class HeaderItemAttribute : PropertyAttribute
    {
        public bool require;
        public string subLocation;
        /// <summary>
        /// Optional name of an instance method (on the target MonoBehaviour) to call via reflection
        /// to obtain the component or a GameObject/Transform that contains the component.
        /// The method must be parameterless.
        /// Use as a named argument: [HeaderItem(methodName = "GetTarget")]
        /// </summary>
        public string methodName;

        /// <summary>
        /// Places this field into a custom header on any <see cref="MonoBehaviour"/>
        /// </summary>
        /// <param name="require">Whether this parameter is considered required. Purely Editor functionality to warn of missing components</param>
        /// <param name="subLocation">The intended sub-path where the attribute should look for a component, separated by / marks.</param>
        /// <param name="methodName">Optional name of a parameterless method on the target that returns a Component/GameObject/Transform to resolve the component from.</param>
        public HeaderItemAttribute(bool require = false, string subLocation = null, string methodName = null)
        {
            this.require = require;
            this.subLocation = subLocation;
            this.methodName = methodName;
        }

        /// <summary>
        /// Places this field into a custom header on any <see cref="MonoBehaviour"/>
        /// </summary>
        /// <param name="subLocation">The intended sub-path where the attribute should look for a component, separated by / marks</param>
        public HeaderItemAttribute(string subLocation)
        {
            this.require = false;
            this.subLocation = subLocation;
            this.methodName = null;
        }

        public static Component GetHeaderItem(MonoBehaviour target, System.Type componentType, string subDirectory, string methodName = null, bool addIfNotFound = false) => ComponentHeaders.GetHeaderItem(target, componentType, subDirectory, methodName, addIfNotFound);

        public static void Reset(MonoBehaviour target) => ComponentHeaders.Reset(target);


#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(HeaderItemAttribute))]
        public class ComponentHeadersEditors : UnityEditor.PropertyDrawer
        {
            public const float iconSize = 16;
            public const string headerClassName = "HeaderItem__Header";
            Item item;

            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                MonoBehaviour behaviour = property.serializedObject.targetObject as MonoBehaviour;

                //if (item != null)
                //{
                //    Selection.activeGameObject = null;
                //    item.RegisterCallbackOnce<DetachFromPanelEvent>(ev => Selection.activeGameObject = //context.gameObject);
                //    return null;
                //}

                var headerItemAtt = attribute as HeaderItemAttribute;
                item = new Item(property, headerItemAtt != null && headerItemAtt.require, headerItemAtt?.subLocation, headerItemAtt?.methodName);


                VisualElement Blank = new()
                {
                    style =
                {
                    height = 0,
                    width = 0,
                }
                };

                Blank.RegisterCallbackOnce<AttachToPanelEvent>(evt =>
                {
                    // Find the inspector root element
                    VisualElement root = Blank;
                    while (root != null && !root.ClassListContains("unity-inspector-element__custom-inspector-container"))
                        root = root.parent;
                    if (root == null) return;

                    // Search for an existing header by class name
                    var existing = root.Q(null, headerClassName);
                    Header header;
                    if (existing is not null and Header foundHeader)
                    {
                        header = foundHeader;
                    }
                    else
                    {
                        header = new Header(behaviour, root);
                        header.AddTo(root);
                    }

                    header.AddItem(item);
                    // Header is now owning the property's visible field, and item will add the ObjectField into PropertyHolder.
                    item.AttachPropertyHolder(header);
                    item.UpdateVisuals();
                });

                return Blank;
            }

            public static void RepaintInspector(SerializedObject BaseObject)
            {
                var inspWin = typeof(EditorApplication).Assembly.GetType("UnityEditor.InspectorWindow");
                var m_RepaintInspectors = inspWin.GetMethod("RepaintAllInspectors", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                m_RepaintInspectors.Invoke(null, null);
            }

            public class Item : VisualElement
            {
                public readonly SerializedProperty property;
                public readonly bool isRequired;
                public readonly VisualElement itemBack;
                public readonly VisualElement icon;

                public readonly ObjectField propertyDraw;

                readonly Type fieldType;
                readonly MonoBehaviour context;
                readonly string subLocation;
                readonly string methodName;

                public Header header { get; private set; }

                bool isSelected => header?.SelectedDrawer == this;
                bool makeRed => isRequired && property.objectReferenceValue == null;

                public Item(SerializedProperty prop, bool required, string subLocationPath, string methodName)
                {
                    property = prop;
                    isRequired = required;
                    context = property.serializedObject.targetObject as MonoBehaviour;
                    subLocation = subLocationPath;
                    this.methodName = methodName;

                    // Determine declared field type via reflection
                    Type resolvedType = typeof(UnityEngine.Object);
                    try
                    {
                        var target = property.serializedObject.targetObject;
                        var field = target.GetType().GetField(property.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (field != null)
                            resolvedType = field.FieldType;
                    }
                    catch
                    {
                        resolvedType = typeof(UnityEngine.Object);
                    }
                    // If the resolved type is not UnityEngine.Object or a UnityEngine.Object-derived type, bound ObjectField will still accept UnityEngine.Object.
                    fieldType = typeof(UnityEngine.Object).IsAssignableFrom(resolvedType) ? resolvedType : typeof(UnityEngine.Object);

                    // Build UI pieces
                    itemBack = new VisualElement();
                    itemBack.style.width = iconSize + 3.5f;
                    itemBack.style.height = iconSize + 2;
                    itemBack.style.borderBottomWidth = 1.5f;
                    itemBack.style.borderTopWidth = 1.5f;
                    itemBack.style.borderLeftWidth = 1.5f;
                    itemBack.style.borderRightWidth = 1.5f;
                    itemBack.style.marginLeft = 5;
                    itemBack.style.marginRight = 5;
                    itemBack.style.marginTop = 4;
                    Add(itemBack);

                    icon = new VisualElement();
                    icon.style.width = iconSize;
                    icon.style.height = iconSize;
                    itemBack.Add(icon);
                    itemBack.RegisterCallback<ClickEvent>(ev => OnIconClicked());

                    propertyDraw = new ObjectField(property.displayName)
                    {
                        objectType = fieldType,
                        allowSceneObjects = true,
                    };
                    propertyDraw.BindProperty(property);
                    propertyDraw.style.flexGrow = 1;
                    propertyDraw.style.flexShrink = 1;
                    propertyDraw.style.marginRight = 4;
                    propertyDraw.style.display = DisplayStyle.None;
                    propertyDraw.RegisterValueChangedCallback(ev => UpdateVisuals());
                }

                // Called by Header to add the property row into the header property holder.
                public void AttachPropertyHolder(Header hostHeader)
                {
                    header = hostHeader;
                    // Add the serialized property field UI into the header's property holder.
                    header.PropertyHolder.Add(propertyDraw);
                }

                // Called by Header when adding the item (to ensure header has control of selection).
                public void AttachToHeader(Header hostHeader)
                {
                    header = hostHeader;
                    // Nothing else here; AttachPropertyHolder will be called by the drawer to place the property field.
                }

                internal void UpdateVisuals()
                {
                    // Update border visuals
                    if (isSelected)
                    {
                        var borderColor = !makeRed ? Color.gray4 : new Color(1, .4f, .4f);
                        itemBack.style.borderBottomColor = borderColor;
                        itemBack.style.borderTopColor = borderColor;
                        itemBack.style.borderLeftColor = borderColor;
                        itemBack.style.borderRightColor = borderColor;
                    }
                    else
                    {
                        itemBack.style.borderBottomColor = Color.clear;
                        itemBack.style.borderTopColor = Color.clear;
                        itemBack.style.borderLeftColor = Color.clear;
                        itemBack.style.borderRightColor = Color.clear;
                    }

                    itemBack.style.backgroundColor = !makeRed ? Color.clear : new Color(1, 0, 0, .4f);

                    // Show or hide the property drawer and button depending on selection
                    propertyDraw.style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;

                    // Update opacity and icon
                    icon.style.opacity = property.objectReferenceValue != null ? 1 : .5f;

                    Texture2D typeIconTex = null;
                    string typeName = property.type;
                    if (property.objectReferenceValue != null)
                    {
                        var obj = property.objectReferenceValue;
                        var objType = obj.GetType();
                        typeName = objType.Name;
                        var content = UnityEditor.EditorGUIUtility.ObjectContent(obj, objType);
                        typeIconTex = content?.image as Texture2D;
                    }
                    else
                    {
                        try
                        {
                            var target = property.serializedObject.targetObject;
                            var field = target.GetType().GetField(property.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (field != null)
                            {
                                typeName = field.FieldType.Name;
                                typeIconTex = UnityEditor.EditorGUIUtility.ObjectContent(null, field.FieldType)?.image as Texture2D;
                            }
                            else
                            {
                                typeIconTex = UnityEditor.EditorGUIUtility.ObjectContent(null, typeof(UnityEditor.MonoScript))?.image as Texture2D;
                            }
                        }
                        catch
                        {
                            typeIconTex = UnityEditor.EditorGUIUtility.ObjectContent(null, typeof(UnityEditor.MonoScript))?.image as Texture2D;
                        }
                    }

                    icon.style.backgroundImage = typeIconTex != null ? typeIconTex : null;
                }

                void OnIconClicked()
                {
                    if (header == null) return;
                    if (!isSelected)
                        header.SelectItem(this);
                    else
                        header.SelectItem(null);

                    // Ensure visuals update on both this and previously selected
                    header.SelectedDrawer?.UpdateVisuals();
                    UpdateVisuals();
                }

                public void GetClicked()
                {
                    if (context == null) return;
                    // Try to get the component from the context's GameObject, supporting optional sub-location path
                    UnityEngine.Object found = null;
                    try
                    {
                        var go = context.gameObject;
                        if (fieldType != null && typeof(Component).IsAssignableFrom(fieldType))
                        {
                            // Use centralized lookup with optional sub-location and do not add yet
                            found = GetHeaderItem(context, fieldType, subLocation, methodName, false);
                        }
                        else if (fieldType == typeof(GameObject))
                        {
                            found = go;
                        }
                        else
                        {
                            // fallback: try centralized lookup which will return null for non-components
                            found = GetHeaderItem(context, fieldType, subLocation, methodName, false);
                        }
                    }
                    catch
                    {
                        found = null;
                    }

                    if (found != null)
                    {
                        property.objectReferenceValue = found;
                        property.serializedObject.ApplyModifiedProperties();
                        propertyDraw.SetValueWithoutNotify(found);
                    }
                    else
                    {
                        if (isRequired)
                        {
                            // Ask user if they'd like to add the component if it's required
                            if (EditorUtility.DisplayDialog("Component Not Found", $"The required component of type '{fieldType.Name}' was not found on GameObject '{context.gameObject.name}' and is required for this to work. Would you like to add it?", "Yes", "No"))
                            {
                                var addedComp = GetHeaderItem(context, fieldType, subLocation, methodName, true);
                                property.objectReferenceValue = addedComp;
                                property.serializedObject.ApplyModifiedProperties();
                                propertyDraw.SetValueWithoutNotify(addedComp);
                            }
                            else
                            {
                                property.objectReferenceValue = null;
                                property.serializedObject.ApplyModifiedProperties();
                                propertyDraw.SetValueWithoutNotify(null);
                            }
                        }
                        else
                        {
                            // If nothing found and not required, clear value
                            property.objectReferenceValue = null;
                            property.serializedObject.ApplyModifiedProperties();
                            propertyDraw.SetValueWithoutNotify(null);
                        }
                    }

                    UpdateVisuals();
                    //RepaintInspector(property.serializedObject);
                }
            }

            public class Header : VisualElement
            {
                public Header(MonoBehaviour behavior, VisualElement context)
                {
                    this.behavior = behavior;
                    this.context = context;

                    name = "superHeader";
                    this.AddToClassList(headerClassName);
                    style.flexGrow = 1f;
                    style.height = iconSize + 10;
                    style.marginTop = -4;
                    style.borderBottomWidth = 2;
                    style.borderLeftWidth = 0;
                    style.borderRightWidth = 0;
                    style.borderBottomColor = Gray(0.1294118f);
                    style.backgroundColor = Gray(0.24f);
                    style.marginLeft = -15;
                    style.marginRight = -6;
                    style.paddingLeft = 13;
                    style.flexDirection = FlexDirection.Row;

                    PropertyHolder = new VisualElement();
                    PropertyHolder.style.minHeight = 4;
                    PropertyHolder.style.flexGrow = 1f;
                    PropertyHolder.style.flexShrink = 1f;
                    PropertyHolder.style.borderTopWidth = 0;
                    PropertyHolder.style.borderRightWidth = 0;
                    PropertyHolder.style.borderLeftWidth = 2;
                    PropertyHolder.style.borderBottomWidth = 2;
                    PropertyHolder.style.borderBottomLeftRadius = 8;
                    PropertyHolder.style.backgroundColor = Color.clear;
                    PropertyHolder.style.borderBottomColor = Gray(0.1294118f);
                    PropertyHolder.style.borderLeftColor = Gray(0.1294118f);
                    PropertyHolder.style.marginRight = -6;
                    PropertyHolder.style.marginLeft = -2;
                    PropertyHolder.style.display = DisplayStyle.None;

                    PostSection = new VisualElement()
                    {
                        style =
                        {
                            flexGrow = 1,
                            flexShrink = 1,
                            width = 6,
                            height = iconSize + 10,
                            flexDirection = FlexDirection.RowReverse
                        }
                    };
                    this.Add(PostSection);

                    GetButton = new(GetButtonPressed)
                    {
                        text = "GET",
                        style =
                        {
                            width = 36,
                            height = iconSize + 8,
                            marginRight = 0,
                            display = DisplayStyle.None
                        }
                    };
                    PostSection.Add(GetButton);

                    var scriptField = context.Q<PropertyField>(classes: "unity-property-field__inspector-property");
                    if (scriptField != null) scriptField.style.display = DisplayStyle.None;

                    //This part feels like a good idea but might not be necessary, the existing functionality seems to be working fine.
                    //this.RegisterCallbackOnce<AttachToPanelEvent>(e =>
                    //{
                    //    ActiveHeaders[context] = this;
                    //});
                    //this.RegisterCallbackOnce<DetachFromPanelEvent>(e =>
                    //{
                    //    Demolish();
                    //    ActiveHeaders.Remove(context);
                    //});
                }

                public void AddTo(VisualElement selector)
                {
                    selector ??= RootElement;
                    if (selector == null) return;

                    while (!selector.ClassListContains("unity-inspector-element__custom-inspector-container"))
                        selector = selector.parent;
                    RootElement = selector;
                    var children = RootElement.Children().ToList();

                    RootElement.Add(this);
                    RootElement.Add(PropertyHolder);

                    for (int i = 0; i < children.Count; i++)
                    {
                        if ((i == 0 && children[i] is PropertyField pf && pf.bindingPath == "m_Script")
                            || children[i] == this || children[i] == PropertyHolder) continue;
                        children[i].BringToFront();
                    }
                }

                public MonoBehaviour behavior;
                public VisualElement context;
                public VisualElement RootElement;
                public VisualElement PropertyHolder;
                public VisualElement PostSection;
                public Button GetButton;

                public Item SelectedDrawer { get; private set; }

                // Controlled method for adding items. Ensures the item is registered but keeps internal state encapsulated.
                public void AddItem(Item item)
                {
                    if (item == null) return;
                    if (registeredItems.ContainsKey(item.property.propertyPath))
                    // Insert the visual element (icon) into this header element
                    this.Add(item);
                    item.AttachToHeader(this);
                    PostSection.BringToFront();
                    registeredItems[item.property.propertyPath] = item;
                }

                // Selects the provided item (or clears selection when null). Ensures only header manages selection state.
                public void SelectItem(Item item)
                {
                    Item prev = SelectedDrawer;
                    SelectedDrawer = item;
                    prev?.UpdateVisuals();
                    SelectedDrawer?.UpdateVisuals();
                    PropertyHolder.style.display = SelectedDrawer != null ? DisplayStyle.Flex : DisplayStyle.None;
                    GetButton.style.display = SelectedDrawer != null ? DisplayStyle.Flex : DisplayStyle.None;
                }

                public void Demolish(bool remove = true)
                {
                    parent?.Remove(PropertyHolder);
                    parent?.Remove(this);
                }

                //public static Dictionary<MonoBehaviour, Header> ActiveHeaders = new();

                void GetButtonPressed()
                {
                    if (SelectedDrawer == null) return;
                    SelectedDrawer.GetClicked();
                }
                public Dictionary<string, Item> registeredItems = new();

            }
            internal static Color Gray(float inp) => new(inp, inp, inp);
        }
#endif
    }

    public static class ComponentHeaders
    {
        /// <summary>
        /// Shared helper: centralizes logic to find a component of the given Type on the given MonoBehaviour's GameObject.
        /// </summary>
        /// <returns>The found Component or null. Does not log errors about missing required components (caller handles that).</returns>
        public static Component GetHeaderItem(MonoBehaviour target, System.Type componentType, string subDirectory, string accessorMethodName = null, bool addIfNotFound = false)
        {
            // First, if an accessor method name is provided, attempt to invoke it and resolve from the returned object.
            if (!string.IsNullOrEmpty(accessorMethodName))
            {
                try
                {
                    var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                    var method = target.GetType().GetMethod(accessorMethodName, flags);
                    if (method != null && method.GetParameters().Length == 0)
                    {
                        var returned = method.Invoke(target, null);
                        if (returned != null)
                        {
                            // If the returned object is directly the requested component type
                            if (returned is Component directComp && componentType.IsAssignableFrom(directComp.GetType()))
                            {
                                return directComp;
                            }

                            // If returned a GameObject, get/add the component there
                            if (returned is GameObject go)
                            {
                                var comp = go.GetComponent(componentType);
                                if (comp) return comp;
                                if (addIfNotFound)
                                {
                                    comp = go.AddComponent(componentType);
#if UNITY_EDITOR
                                    Undo.RegisterCreatedObjectUndo(comp, "Add Related Component");
#endif
                                    return comp;
                                }
                                // Not found; continue to fallback behavior
                            }

                            // If returned a Transform, search on its GameObject
                            if (returned is Transform t)
                            {
                                var comp = t.gameObject.GetComponent(componentType);
                                if (comp) return comp;
                                if (addIfNotFound)
                                {
                                    comp = t.gameObject.AddComponent(componentType);
#if UNITY_EDITOR
                                    Undo.RegisterCreatedObjectUndo(comp, "Add Related Component");
#endif
                                    return comp;
                                }
                            }

                            // If returned some other Component, try to locate requested component on its GameObject
                            if (returned is Component someComp)
                            {
                                // If returned component is assignable, return it already handled above.
                                var comp = someComp.GetComponent(componentType);
                                if (comp) return comp;
                                if (addIfNotFound)
                                {
                                    comp = someComp.gameObject.AddComponent(componentType);
#if UNITY_EDITOR
                                    Undo.RegisterCreatedObjectUndo(comp, "Add Related Component");
#endif
                                    return comp;
                                }
                            }

                            // If returned object itself is of a type assignable to the requested type (non-Component)
                            // unlikely for Unity components, but attempt a cast if possible.
                            if (componentType.IsInstanceOfType(returned) && returned is Component asComp)
                            {
                                return asComp;
                            }
                        }
                    }
                }
                catch
                {
                    // Swallow reflection exceptions and fall back to original behavior.
                }
            }

            // Fallback: existing subDirectory-based search/add logic.
            GameObject foundSubTarget = null;
            if (subDirectory != null)
            {
                string[] directory = subDirectory.Split('/');
                foundSubTarget = target.gameObject;

                foreach (var d in directory)
                {
                    Transform child = foundSubTarget.transform.Find(d);
                    if (child != null) foundSubTarget = child.gameObject;
                    else
                    {
                        foundSubTarget = null;
                        break;
                    }
                }
            }

            Component result = null;

            if (subDirectory != null && foundSubTarget)
            {
                result = foundSubTarget.GetComponent(componentType);
                if (result) return result;

                if (addIfNotFound)
                {
                    result = foundSubTarget.AddComponent(componentType);
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(result, "Add Related Component");
#endif
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
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(result, "Add Related Component");
#endif
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
            //Run through all fields with HeaderItemAttribute or PlaceInHeaderAttribute
            var fields = target.GetType().GetFields();
            foreach (var field in fields)
            {
                // Get all attributes and check for either HeaderItemAttribute or PlaceInHeaderAttribute
                var attrs = field.GetCustomAttributes(true);
                foreach (var a in attrs)
                {
                    if (a is HeaderItemAttribute placeAttr)
                    {
                        var fieldType = field.FieldType;
                        if (typeof(Component).IsAssignableFrom(fieldType))
                        {
                            var GetComp = GetHeaderItem(target, fieldType, placeAttr.subLocation, placeAttr.methodName, placeAttr.require);
                            if (GetComp != null)
                            {
                                field.SetValue(target, GetComp);
                            }
                            else if (placeAttr.require)
                            {
                                var addComp = target.gameObject.AddComponent(fieldType);
                                field.SetValue(target, addComp);
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}