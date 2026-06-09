using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static SLS.EditorUtilities.ComponentHeaders.HeaderItemAttribute;

namespace SLS.EditorUtilities.ComponentHeaders
{

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(HeaderItemAttribute))]
    public class ComponentHeadersEditors : UnityEditor.PropertyDrawer
    {
        public const float iconSize = 16;
        public const string headerClassName = "RelatedComponent__Header";
        Item item;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            MonoBehaviour context = property.serializedObject.targetObject as MonoBehaviour;

            if (item != null)
            {
                Selection.activeGameObject = null;
                item.RegisterCallbackOnce<DetachFromPanelEvent>(ev => Selection.activeGameObject = context.gameObject);
                return null;
            }

            var relatedAttr = attribute as HeaderItemAttribute;
            item = new Item(property, relatedAttr != null && relatedAttr.require, relatedAttr?.subLocation, relatedAttr?.methodName);


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
                    header = new Header(context);
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
            readonly SerializedProperty property;
            readonly bool isRequired;
            readonly VisualElement itemBack;
            readonly VisualElement icon;

            readonly VisualElement propertyRow;
            readonly ObjectField propertyDraw;
            readonly Button getButton;

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

                // Property row contains the actual ObjectField and the "Get" button
                propertyRow = new VisualElement();
                propertyRow.style.flexDirection = FlexDirection.Row;
                propertyRow.style.alignItems = Align.Center;
                propertyRow.style.display = DisplayStyle.None;

                propertyDraw = new ObjectField(property.displayName)
                {
                    objectType = fieldType,
                    allowSceneObjects = true,
                };
                propertyDraw.BindProperty(property);
                propertyDraw.style.flexGrow = 1;
                propertyDraw.style.flexShrink = 1;
                propertyDraw.style.marginRight = 4;
                propertyDraw.RegisterValueChangedCallback(ev => UpdateVisuals());

                propertyDraw.labelElement.style.flexShrink = 1f;
                propertyDraw.labelElement.style.flexGrow = 0f;
                propertyDraw.labelElement.style.minWidth = 0;
                propertyDraw.labelElement.style.marginLeft = 2;
                propertyDraw.labelElement.style.marginRight = 4;
                propertyDraw.labelElement.focusable = true;
                propertyDraw.labelElement.RegisterCallback<FocusEvent>(ev => propertyDraw.Focus());
                propertyDraw.labelElement.RegisterCallback<BlurEvent>(ev => propertyDraw.Blur());

                getButton = new Button(OnGetClicked) { text = "Get" };
                getButton.style.width = 36;
                getButton.style.height = iconSize + 2;
                getButton.style.marginRight = 0;

                propertyDraw.Remove(propertyDraw.labelElement);

                propertyRow.Add(propertyDraw.labelElement);
                propertyRow.Add(getButton);
                propertyRow.Add(propertyDraw);

                //propertyRow.Add(propertyDraw);
                //propertyRow.Add(getButton);
            }

            // Called by Header to add the property row into the header property holder.
            public void AttachPropertyHolder(Header hostHeader)
            {
                header = hostHeader;
                // Add the serialized property field UI into the header's property holder.
                header.PropertyHolder.Add(propertyRow);
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
                propertyRow.style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;

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

            void OnGetClicked()
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
                        found = GetRelatedComponent(context, fieldType, subLocation, methodName, false);
                    }
                    else if (fieldType == typeof(GameObject))
                    {
                        found = go;
                    }
                    else
                    {
                        // fallback: try centralized lookup which will return null for non-components
                        found = GetRelatedComponent(context, fieldType, subLocation, methodName, false);
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
                            var addedComp = GetRelatedComponent(context, fieldType, subLocation, methodName, true);
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
                RepaintInspector(property.serializedObject);
            }
        }

        public class Header : VisualElement
        {
            public Header(MonoBehaviour context)
            {
                this.context = context;

                name = "superHeader";
                this.AddToClassList(headerClassName);
                style.flexGrow = 1f;
                style.height = iconSize + 14;
                style.borderTopWidth = 2;
                style.borderBottomWidth = 2;
                style.borderLeftWidth = 0;
                style.borderRightWidth = 0;
                style.borderBottomColor = Gray(0.1294118f);
                style.borderTopColor = Gray(0.0509804f);
                style.backgroundColor = Gray(0.1647059f);
                style.marginLeft = -15;
                style.marginRight = -6;
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
                PropertyHolder.style.backgroundColor = Gray(0.1647059f);
                PropertyHolder.style.borderBottomColor = Gray(0.1294118f);
                PropertyHolder.style.borderBottomColor = Gray(0.1294118f);
                PropertyHolder.style.marginRight = -6;
                PropertyHolder.style.marginLeft = -2;
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

            public MonoBehaviour context;
            public VisualElement RootElement;
            public VisualElement PropertyHolder;

            public Item SelectedDrawer { get; private set; }

            // Controlled method for adding items. Ensures the item is registered but keeps internal state encapsulated.
            public void AddItem(Item item)
            {
                if (item == null) return;
                // Insert the visual element (icon) into this header element
                this.Add(item);
                item.AttachToHeader(this);
            }

            // Selects the provided item (or clears selection when null). Ensures only header manages selection state.
            public void SelectItem(Item item)
            {
                var prev = SelectedDrawer;
                SelectedDrawer = item;
                prev?.UpdateVisuals();
                SelectedDrawer?.UpdateVisuals();
            }

            public void Demolish(bool remove = true)
            {
                parent?.Remove(PropertyHolder);
                parent?.Remove(this);
            }
        }
        internal static Color Gray(float inp) => new(inp, inp, inp);
    }
#endif
}