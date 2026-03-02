using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

namespace VisualElementsHelpers
{
    public static class UIHelpers
    {

        // Generic factory: create a VisualElement (or subtype) and apply style initialization
        public static T NewVisualElement<T>(System.Action<IStyle> styleInit = null) where T : VisualElement, new()
        {
            var el = new T();
            styleInit?.Invoke(el.style);
            return el;
        }

        public static void SetStyle(this VisualElement v, IStyle input)
        {
            v.style.flexDirection = input.flexDirection;
            v.style.alignItems = input.alignItems;
            v.style.paddingTop = input.paddingTop;
            v.style.paddingBottom = input.paddingBottom;
            v.style.paddingLeft = input.paddingLeft;
            v.style.paddingRight = input.paddingRight;
            v.style.marginTop = input.marginTop;
            v.style.marginBottom = input.marginBottom;
            v.style.marginLeft = input.marginLeft;
            v.style.marginRight = input.marginRight;
            v.style.width = input.width;
            v.style.height = input.height;
            v.style.flexGrow = input.flexGrow;
            v.style.unityTextAlign = input.unityTextAlign;
            v.style.display = input.display;
            v.style.bottom = input.bottom;
            v.style.left = input.left;
            v.style.position = input.position;
            v.style.right = input.right;
            v.style.top = input.top;
            v.style.cursor = input.cursor;
        }

        public static int LabelTextWidth(this Label label)
        {
            return 0;

            //string text = label.text;
            //IStyle style = label.style;
            //Length fontSize = style.fontSize.value;
            //FontStyle fontStyle = style.unityFontStyleAndWeight.value;
            //Font font = style.unityFont.value ?? Font.CreateDynamicFontFromOSFont("Arial", (int)fontSize);
            //FontStyle fs = FontStyle.Normal;
            //switch (fontStyle)
            //{
            //    case FontStyle.Bold: fs = FontStyle.Bold; break;
            //    case FontStyle.Italic: fs = FontStyle.Italic; break;
            //    case FontStyle.BoldAndItalic: fs = FontStyle.BoldAndItalic; break;
            //    default: fs = FontStyle.Normal; break;
            //}
            //font.RequestCharactersInTexture(text, (int)fontSize, fs);
            //int totalWidth = 0;
            //foreach (char c in text)
            //{
            //    if (font.GetCharacterInfo(c, out CharacterInfo charInfo, (int)fontSize, fs))
            //    {
            //        totalWidth += charInfo.advance;
            //    }
            //}
            //return totalWidth;
        }

        public static void SetAllMargins(this VisualElement v, float value)
        {
            v.style.marginTop = value;
            v.style.marginBottom = value;
            v.style.marginLeft = value;
            v.style.marginRight = value;
        }

        public static VisualElement GetChild(this VisualElement V, int i)
            => V.hierarchy.childCount > i ? V.hierarchy.ElementAt(i) : null;

        public static VisualElement GetDescendent(this VisualElement V, params int[] path)
        {
            VisualElement.Hierarchy H = V.hierarchy;
            VisualElement R = null;
            for (int i = 0; i < path.Length; i++)
            {
                if (H.childCount <= path[i]) return null;
                R = H.ElementAt(path[i]);
                H = R.hierarchy;
            }
            return R;
        }

        public static void Iterate(this SerializedProperty property, Action<SerializedProperty> action)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty(); // one past the last child

            if (!iterator.NextVisible(true)) return;

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                action(iterator);
                if (!iterator.NextVisible(false)) break;
            }
        }

        public static void IterateAndDraw(this SerializedProperty property, VisualElement container)
        {
            // Iterate visible children of the property and add a PropertyField for each.
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty(); // one past the last child
            // Move into the first visible child
            if (!iterator.NextVisible(true))
                return;

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                // Make a copy for the PropertyField since iterator will advance
                var childProp = iterator.Copy();
                var field = new PropertyField(childProp);
                field.Bind(property.serializedObject);
                container.Add(field);

                // Advance to next visible sibling/child
                if (!iterator.NextVisible(false))
                    break;
            }

        }

        public static SerializedProperty AddArrayElement(this SerializedProperty arrayProperty)
        {
            arrayProperty.arraySize++;
            return arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1);
        }

        public static void DelayedBuild(this VisualElement V, Action result)
            => V.RegisterCallbackOnce<AttachToPanelEvent>(_ => { V.schedule.Execute(result); });


        public static bool QCache<T>(this VisualElement V, out T result, string name = null, string className = null) where T : VisualElement
        {
            result = V.Q<T>(name, className) ?? null;
            return result != null;
        }

        public static void Highlighter(this VisualElement V, Color highlightColor)
        {
            Color initialColor = V.style.color.value;

            V.RegisterCallback<MouseOverEvent>(Hover);
            V.RegisterCallback<MouseLeaveEvent>(UnHover);

            void Hover(MouseOverEvent E) => V.style.color = highlightColor;
            void UnHover(MouseLeaveEvent E) => V.style.color = initialColor;
        }
        public static void Highlighter(this VisualElement V, Color highlightColor, Color? backgroundHighlightColor = null, Color? borderHighlightColor = null)
        {
            Color initialColor = V.style.color.value;
            Color initialColorBack = V.style.backgroundColor.value;
            Color initialColorBorder = V.style.borderTopColor.value;

            V.RegisterCallback<MouseOverEvent>(Hover);
            V.RegisterCallback<MouseLeaveEvent>(UnHover);

            void Hover(MouseOverEvent E)
            {
                V.style.color = highlightColor;
                if (backgroundHighlightColor != null) V.style.backgroundColor = backgroundHighlightColor.Value;
                if (borderHighlightColor != null)
                {
                    V.style.borderTopColor = borderHighlightColor.Value;
                    V.style.borderBottomColor = borderHighlightColor.Value;
                    V.style.borderLeftColor = borderHighlightColor.Value;
                    V.style.borderRightColor = borderHighlightColor.Value;
                }
            }
            void UnHover(MouseLeaveEvent E)
            {
                V.style.color = initialColor;
                if (backgroundHighlightColor != null) V.style.backgroundColor = initialColorBack;
                if (borderHighlightColor != null)
                {
                    V.style.borderTopColor = initialColorBorder;
                    V.style.borderBottomColor = initialColorBorder;
                    V.style.borderLeftColor = initialColorBorder;
                    V.style.borderRightColor = initialColorBorder;
                }
            }
        }
        public static void Highlighter(this VisualElement V, float factor)
        {
            Color initialColor = V.style.color.value;

            V.RegisterCallback<MouseOverEvent>(Hover);
            V.RegisterCallback<MouseLeaveEvent>(UnHover);

            void Hover(MouseOverEvent E) => V.style.color = new Color(initialColor.r + factor, initialColor.g + factor, initialColor.b + factor);
            void UnHover(MouseLeaveEvent E) => V.style.color = initialColor;
        }

        public static void RegisterHoverEvents(this VisualElement V, Action<bool> hovered)
        {
            V.RegisterCallback<MouseOverEvent>(Do);
            V.RegisterCallback<MouseLeaveEvent>(Do);
            void Do(EventBase E) => hovered?.Invoke(E is MouseOverEvent);
        }
    }

    public static class CustomStyles
    {














    }

    public static class X_StyleBuilder
    {

        //Borders
        public static IStyle Border(this IStyle S,
            float? all = null,
            float? vertical = null,
            float? horizontal = null,
            float? right = null,
            float? top = null,
            float? bottom = null,
            float? left = null,
            Color? color = null
            )
        {
            if (all.HasValue)
            {
                S.borderRightWidth = all.Value;
                S.borderTopWidth = all.Value;
                S.borderBottomWidth = all.Value;
                S.borderLeftWidth = all.Value;
            }
            if (vertical.HasValue)
            {
                S.borderTopWidth = vertical.Value;
                S.borderBottomWidth = vertical.Value;
            }
            if (horizontal.HasValue)
            {
                S.borderRightWidth = horizontal.Value;
                S.borderLeftWidth = horizontal.Value;
            }
            if (right.HasValue) S.borderRightWidth = right.Value;
            if (top.HasValue) S.borderTopWidth = top.Value;
            if (bottom.HasValue) S.borderBottomWidth = bottom.Value;
            if (left.HasValue) S.borderLeftWidth = left.Value;
            if (color.HasValue)
            {
                S.borderRightColor = color.Value;
                S.borderTopColor = color.Value;
                S.borderBottomColor = color.Value;
                S.borderLeftColor = color.Value;
            }
            return S;
        }
        public static IStyle Radius(this IStyle S,
            float? all = null,
            float? top = null,
            float? bottom = null,
            float? left = null,
            float? right = null,
            float? topLeft = null,
            float? topRight = null,
            float? bottomLeft = null,
            float? bottomRight = null
            )
        {
            if (all.HasValue)
            {
                S.borderTopLeftRadius = all.Value;
                S.borderTopRightRadius = all.Value;
                S.borderBottomLeftRadius = all.Value;
                S.borderBottomRightRadius = all.Value;
            }

            if (top.HasValue)
            {
                S.borderTopLeftRadius = top.Value;
                S.borderTopRightRadius = top.Value;
            }
            if (bottom.HasValue)
            {
                S.borderBottomLeftRadius = bottom.Value;
                S.borderBottomRightRadius = bottom.Value;
            }
            if (left.HasValue)
            {
                S.borderTopLeftRadius = left.Value;
                S.borderBottomLeftRadius = left.Value;
            }
            if (right.HasValue)
            {
                S.borderTopRightRadius = right.Value;
                S.borderBottomRightRadius = right.Value;
            }
            if (topLeft.HasValue) S.borderTopLeftRadius = topLeft.Value;
            if (topRight.HasValue) S.borderTopRightRadius = topRight.Value;
            if (bottomLeft.HasValue) S.borderBottomLeftRadius = bottomLeft.Value;
            if (bottomRight.HasValue) S.borderBottomRightRadius = bottomRight.Value;


            return S;
        }
        public static IStyle BorderNull(this IStyle S)
        {
            S.borderRightWidth = 0;
            S.borderTopWidth = 0;
            S.borderBottomWidth = 0;
            S.borderLeftWidth = 0;
            S.borderTopColor = Color.clear;
            S.borderBottomColor = Color.clear;
            S.borderLeftColor = Color.clear;
            S.borderRightColor = Color.clear;
            S.borderBottomLeftRadius = 0;
            S.borderBottomRightRadius = 0;
            S.borderTopLeftRadius = 0;
            S.borderTopRightRadius = 0;
            return S;
        }



        public static IStyle FixedSize(this IStyle S,
            float? width = null,
            float? height = null
            )
        {
            if (width.HasValue) S.width = width.Value;
            if (height.HasValue) S.height = height.Value;
            return S;
        }
        public static IStyle MinMaxSize(this IStyle S,
            float? minWidth = null,
            float? minHeight = null,
            float? maxWidth = null,
            float? maxHeight = null
            )
        {
            if (minWidth.HasValue) S.minWidth = minWidth.Value;
            if (minHeight.HasValue) S.minHeight = minHeight.Value;
            if (maxWidth.HasValue) S.maxWidth = maxWidth.Value;
            if (maxHeight.HasValue) S.maxHeight = maxHeight.Value;
            return S;
        }
        public static IStyle Flex(this IStyle S,
            FlexDirection? direction = null,
            float? grow = null,
            float? shrink = null,
            StyleKeyword? basis = null
            )
        {
            if (direction.HasValue) S.flexDirection = direction.Value;
            if (grow.HasValue) S.flexGrow = grow.Value;
            if (shrink.HasValue) S.flexShrink = shrink.Value;
            if (basis.HasValue) S.flexBasis = basis.Value;
            return S;
        }
        public static IStyle Align(this IStyle S,
            Align? alignItems = null,
            Justify? justifyContent = null,
            Align? alignSelf = null
            )
        {
            if (alignItems.HasValue) S.alignItems = alignItems.Value;
            if (justifyContent.HasValue) S.justifyContent = justifyContent.Value;
            if (alignSelf.HasValue) S.alignSelf = alignSelf.Value;
            return S;
        }

        public static IStyle Padding(this IStyle S,
            float? all = null,
            float? vertical = null,
            float? horizontal = null,
            float? top = null,
            float? bottom = null,
            float? left = null,
            float? right = null
            )
        {
            if (all.HasValue)
            {
                S.paddingRight = all.Value;
                S.paddingTop = all.Value;
                S.paddingBottom = all.Value;
                S.paddingLeft = all.Value;
            }
            if (vertical.HasValue)
            {
                S.paddingTop = vertical.Value;
                S.paddingBottom = vertical.Value;
            }
            if (horizontal.HasValue)
            {
                S.paddingRight = horizontal.Value;
                S.paddingLeft = horizontal.Value;
            }
            if (right.HasValue) S.paddingRight = right.Value;
            if (top.HasValue) S.paddingTop = top.Value;
            if (bottom.HasValue) S.paddingBottom = bottom.Value;
            if (left.HasValue) S.paddingLeft = left.Value;

            return S;
        }
        public static IStyle Margins(this IStyle S,
            float? all = null,
            float? vertical = null,
            float? horizontal = null,
            float? top = null,
            float? bottom = null,
            float? left = null,
            float? right = null
            )
        {
            if (all.HasValue)
            {
                S.marginRight = all.Value;
                S.marginTop = all.Value;
                S.marginBottom = all.Value;
                S.marginLeft = all.Value;
            }
            if (vertical.HasValue)
            {
                S.marginTop = vertical.Value;
                S.marginBottom = vertical.Value;
            }
            if (horizontal.HasValue)
            {
                S.marginRight = horizontal.Value;
                S.marginLeft = horizontal.Value;
            }
            if (right.HasValue) S.marginRight = right.Value;
            if (top.HasValue) S.marginTop = top.Value;
            if (bottom.HasValue) S.marginBottom = bottom.Value;
            if (left.HasValue) S.marginLeft = left.Value;

            return S;
        }

        public static IStyle Colors(this IStyle S,
            Color? color = null,
            Color? background = null,
            Color? border = null
            )
        {
            if (color.HasValue) S.color = color.Value;
            if (background.HasValue) S.backgroundColor = background.Value;
            if (border.HasValue)
            {
                S.borderTopColor = border.Value;
                S.borderBottomColor = border.Value;
                S.borderLeftColor = border.Value;
                S.borderRightColor = border.Value;
            }
            return S;
        }

        public static IStyle Text(this IStyle S,
            int? fontSize = null,
            TextAnchor? align = null,
            FontStyle? fontStyle = null,
            Font font = null
            )
        {
            if (fontSize.HasValue) S.fontSize = fontSize.Value;
            if (align.HasValue) S.unityTextAlign = align.Value;
            if (fontStyle.HasValue) S.unityFontStyleAndWeight = fontStyle.Value;
            if (font != null) S.unityFont = font;
            return S;
        }
        public static Color Gray(this float v) => new(v, v, v, 1);








    }


    public class FoldoutPlus : Foldout
    {
        public FoldoutPlus()
        {
            header = this.GetChild(0) as Toggle;

            headerSide = new VisualElement();
            header.Add(headerSide);

            header.style.overflow = Overflow.Visible;

            headerSide.style.flexDirection = FlexDirection.Column;
            headerSide.style.position = Position.Absolute;
            headerSide.style.left = EditorGUIUtility.labelWidth;
            headerSide.style.right = 0;
            headerSide.style.maxHeight = EditorGUIUtility.singleLineHeight;
            this.contentContainer.style.marginTop = 0;

            this.RegisterCallback<AttachToPanelEvent>(EstablishElements);

            void EstablishElements(AttachToPanelEvent evt)
            {
                OnEstablishElements();
                this.UnregisterCallback<AttachToPanelEvent>(EstablishElements);
            }

            //label.RegisterCallback<GeometryChangedEvent>(evt =>
            //{
            //    var rect = label.layout; // layout is in UIElements coordinates
            //                              // Left = label's x + its width (+ small gap if you want)
            //    headerSide.style.left = rect.x + rect.width + 2;
            //    // Right = keep zero so the header side fills to the right edge of the toggle
            //    headerSide.style.right = 0;
            //});
        }
        public Toggle header { get; private set; }
        public VisualElement arrowButton { get; private set; }
        public Label label { get; private set; }
        public VisualElement headerSide { get; private set; }
        public bool expanded
        {
            get => this.value;
            set => this.value = value;
        }

        new public bool toggleOnLabelClick = true;

        public bool expandable
        {
            set
            {
                arrowButton.visible = value;
                base.toggleOnLabelClick = value && toggleOnLabelClick;
            }
        }

        protected virtual void OnEstablishElements()
        {
            arrowButton = header.GetDescendent(0, 0);
            label = header.GetDescendent(0, 1) as Label;
        }
    }

    public class FoldoutArrow : Button
    {
        public FoldoutArrow(Action<bool> clickEvent = null) : base()
        {
            this.clickEvent = clickEvent;
            SetExpanded(false);

            clicked += () => { SetExpanded(!isExpanded); };

            style.color = new StyleColor(Color.gray4);
            style.width = 18;
            style.height = 16;
            style.unityTextAlign = TextAnchor.MiddleCenter;

            style.backgroundColor = new StyleColor(Color.clear);
            style.Border(0, color: Color.clear).Radius(0).Padding(0);
        }

        public void SetExpanded(bool expanded)
        {
            isExpanded = expanded;
            base.text = expanded ? "▼" : "▶";
            clickEvent?.Invoke(isExpanded);
        }
        public bool isExpanded { get; private set; }
        private Action<bool> clickEvent;
        new private VisualElement text = null;
    }


    public class SuperList<T> : VisualElement
    {
        public SuperList(SerializedProperty listProperty, PropertyToVisualElementDelegate drawElementBody = null)
        {
            property = listProperty;
            serializedObject = listProperty.serializedObject;
            DrawElementBody = drawElementBody;

            CreateVisualElements();

            // Initialize elements list if property exists
            items = new List<Item>();
            if (property != null)
            {
                property.serializedObject.Update();
                for (int i = 0; i < arraySize; i++) CreateItemElement(i);
                arraySize = arraySize; // force UI counter update
            }

            this.Bind(listProperty.serializedObject);

            // Register polling to detect external changes (Reset, script changes, etc.)
            RegisterEditorUpdate();
            // Ensure we unregister when the element is removed from the panel
            this.RegisterCallback<DetachFromPanelEvent>((evt) => { UnregisterEditorUpdate(); });
        }

        #region Visual Pieces
        public VisualElement headerBar { get; private set; }
        public Label label { get; private set; }
        public Button addButton { get; private set; }
        public FoldoutArrow foldoutArrow { get; private set; }
        public Label elementCounter { get; private set; }

        //Content Section
        public VisualElement collectionBackground { get; private set; }
        #endregion


        //Callbacks
        public PassListDelegate preAddCallback { get; set; }
        public RemoveElementDelegate preRemoveCallback { get; set; }
        public PassListDelegate preClearCallback { get; set; }

        //Data
        public SerializedProperty property { get; private set; }
        public SerializedObject serializedObject { get; private set; }
        public List<Item> items { get; private set; }

        public int arraySize
        {
            get => (property != null) ? property.arraySize : 0;
            set
            {
                if (property == null) return;

                if (value > property.arraySize) expanded = true;
                expandable = value > 0;

                property.serializedObject.Update();
                property.arraySize = value;
                if (elementCounter != null)
                    elementCounter.text = (property != null) ? property.arraySize.ToString() : "0";
                // Do not ApplyModifiedProperties here — callers should apply as needed, but keep UI in sync
            }
        }
        public bool expanded
        {
            get => _expanded;
            set
            {
                _expanded = value;
                if (collectionBackground != null) collectionBackground.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                if (foldoutArrow != null) foldoutArrow.text = value ? "▼" : "▶";
            }
        }
        bool _expanded = false;

        public bool expandable
        {
            set
            {
                if (foldoutArrow != null) foldoutArrow.visible = value;
                if (!value) expanded = false;
            }
        }


        #region Add Systems

        protected virtual void AddButtonPressed_Default()
        {
            if (preAddCallback != null) preAddCallback(this);
            else
            {
                CreatePropertySlot(out int newID);
                SetOrCreateItemValue(newID);
                CreateItemElement(newID);
            }
        }

        public virtual void CreatePropertySlot(out int newID)
        {
            if (property == null) throw new InvalidOperationException("Property is null");

            property.serializedObject.Update();

            arraySize++;

            property.serializedObject.ApplyModifiedProperties();

            newID = property.arraySize - 1;
        }

        public virtual void SetOrCreateItemValue(int ID, object input = null)
        {
            if (property == null) throw new InvalidOperationException("Property is null");
            property.serializedObject.Update();
            SerializedProperty targetProperty = property.GetArrayElementAtIndex(ID) ?? throw new ArgumentOutOfRangeException(nameof(ID));

            // If input is null, provide a sensible default depending on the property type.
            if (input == null)
            {
                switch (targetProperty.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        targetProperty.intValue = 0;
                        break;
                    case SerializedPropertyType.Boolean:
                        targetProperty.boolValue = false;
                        break;
                    case SerializedPropertyType.Float:
                        targetProperty.floatValue = 0f;
                        break;
                    case SerializedPropertyType.String:
                        targetProperty.stringValue = string.Empty;
                        break;
                    case SerializedPropertyType.Enum:
                        targetProperty.intValue = 0;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        targetProperty.objectReferenceValue = null;
                        break;
                    case SerializedPropertyType.ManagedReference:
                        try { targetProperty.managedReferenceValue = Activator.CreateInstance(typeof(T)); }
                        catch { targetProperty.managedReferenceValue = null; }
                        break;
                    default:
                        // Try managed reference as fallback
                        try { targetProperty.managedReferenceValue = Activator.CreateInstance(typeof(T)); } catch { }
                        break;
                }
            }
            else
            {
                // Convert input to the appropriate underlying serialized value.
                try
                {
                    switch (targetProperty.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            targetProperty.intValue = Convert.ToInt32(input);
                            break;
                        case SerializedPropertyType.Boolean:
                            targetProperty.boolValue = Convert.ToBoolean(input);
                            break;
                        case SerializedPropertyType.Float:
                            targetProperty.floatValue = Convert.ToSingle(input);
                            break;
                        case SerializedPropertyType.String:
                            targetProperty.stringValue = Convert.ToString(input);
                            break;
                        case SerializedPropertyType.Enum:
                            // Enums are stored as intValue
                            targetProperty.intValue = Convert.ToInt32(input);
                            break;
                        case SerializedPropertyType.ObjectReference:
                            targetProperty.objectReferenceValue = input as UnityEngine.Object;
                            break;
                        case SerializedPropertyType.ManagedReference:
                            targetProperty.managedReferenceValue = input;
                            break;
                        default:
                            // best-effort fallback
                            try { targetProperty.managedReferenceValue = input; } catch { }
                            break;
                    }
                }
                catch
                {
                    // If conversion fails, attempt a safe fallback default
                    switch (targetProperty.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            targetProperty.intValue = 0;
                            break;
                        case SerializedPropertyType.Boolean:
                            targetProperty.boolValue = false;
                            break;
                        case SerializedPropertyType.Float:
                            targetProperty.floatValue = 0f;
                            break;
                        case SerializedPropertyType.String:
                            targetProperty.stringValue = string.Empty;
                            break;
                        default:
                            // leave as-is for object/managed references
                            break;
                    }
                }
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        public virtual void CreateItemElement(int ID)
        {
            if (property == null) throw new InvalidOperationException("Property is null");
            if (serializedObject == null) throw new InvalidOperationException("Owner object is null");

            // Ensure the SerializedObject is up-to-date before getting the element
            property.serializedObject.Update();

            // Compute the element path so we can resolve a fresh SerializedProperty whenever needed.
            string elementPath = $"{property.propertyPath}.Array.data[{ID}]";

            Item element = new(serializedObject, elementPath, this, DrawElementBody);
            items.Add(element);
            collectionBackground.Add(element);

            // Bind the newly created element to the owner object so it displays immediately and reacts to changes.
            try { element.Bind(serializedObject); } catch { }
        }

        #endregion

        #region Remove Systems

        protected virtual void RemoveButtonPressed_Default(Item E)
        {
            // Return early if index invalid
            if (items == null || E == null || !items.Contains(E)) return;
            int index = items.IndexOf(E);
            if (preRemoveCallback != null) preRemoveCallback(this, index);
            else
            {
                if (property == null) return;
                if (items == null || index < 0 || index >= items.Count) return;

                // Remove from serialized property first
                DeletePropertySlotAt(index);

                // Then remove the UI element and internal list entry
                RemoveItemElementAt(index);
            }
        }

        public virtual void DeletePropertySlotAt(int index)
        {
            if (property == null) return;
            property.serializedObject.Update();

            // Delete once. For object reference slots Unity may leave a null placeholder and require a second delete call.
            property.DeleteArrayElementAtIndex(index);

            // If the array still has an element at this index and it's an object reference that is null,
            // delete it again to fully remove the slot.
            if (index < property.arraySize)
            {
                SerializedProperty maybeElem = property.GetArrayElementAtIndex(index);
                if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
                {
                    property.DeleteArrayElementAtIndex(index);
                }
            }

            // Keep UI counter accurate
            arraySize = property.arraySize;
            property.serializedObject.ApplyModifiedProperties();
        }

        public virtual void RemoveItemElementAt(int index)
        {
            if (items == null) return;
            if (index < 0 || index >= items.Count) return;

            if (collectionBackground != null && items[index] != null) collectionBackground.Remove(items[index]);
            items.RemoveAt(index);
        }


        protected virtual void ClearButtonPressed_Default()
        {
            if (preClearCallback != null) preClearCallback(this);
            else
            {
                if (property != null)
                {
                    property.serializedObject.Update();
                    property.arraySize = 0;
                    property.serializedObject.ApplyModifiedProperties();
                }

                if (items != null)
                {
                    foreach (var el in items)
                    {
                        collectionBackground.Remove(el);
                    }
                    items.Clear();
                }
                arraySize = 0;
            }
        }
        #endregion


        // The delegate callers should set to produce the body VisualElement for a given element property.
        public PropertyToVisualElementDelegate DrawElementBody
        { get; set; }

        public delegate VisualElement PropertyToVisualElementDelegate(SerializedProperty elementProperty);
        public delegate void PassListDelegate(SuperList<T> list);
        public delegate void RemoveElementDelegate(SuperList<T> list, int index);

        // Editor update registration
        private bool updateRegistered = false;

        private void RegisterEditorUpdate()
        {
#if UNITY_EDITOR
            if (updateRegistered) return;
            EditorApplication.update += EditorUpdate;
            updateRegistered = true;
#endif
        }

        private void UnregisterEditorUpdate()
        {
#if UNITY_EDITOR
            if (!updateRegistered) return;
            EditorApplication.update -= EditorUpdate;
            updateRegistered = false;
#endif
        }

        private void EditorUpdate()
        {
#if UNITY_EDITOR
            if (property == null) return;
            try
            {
                property.serializedObject.Update();
                int size = property.arraySize;
                if (items == null) items = new List<Item>();
                if (size != items.Count)
                {
                    // External change detected (Reset, undo, etc.) -> rebuild UI to match serialized data
                    RebuildItems();
                }
            }
            catch
            {
                // swallow exceptions to avoid EditorApplication update throwing
            }
#endif
        }

        private void RebuildItems()
        {
            if (property == null) return;
            // Clear existing visuals
            if (collectionBackground != null)
            {
                collectionBackground.Clear();
            }
            if (items != null)
            {
                items.Clear();
            }
            // Recreate elements from serialized property
            property.serializedObject.Update();
            int size = property.arraySize;
            for (int i = 0; i < size; i++)
            {
                CreateItemElement(i);
            }
            // Update counter
            if (elementCounter != null) elementCounter.text = size.ToString();
            // Ensure display state aligns with expandability
            expandable = size > 0;
        }

        private void CreateVisualElements()
        {
            //HeaderBar()
            {
                var headerBar = new VisualElement();
                headerBar.name = "superlist-headerbar";

                headerBar.style
                    .Flex(FlexDirection.Row)
                    .Align(Align.Center)
                    .FixedSize(height: 20)
                    .Colors(null, .2078432f.Gray(), .1411765f.Gray())
                    .Border(1)
                    .Radius(0, top: 6);

                //FoldoutArrow()
                {
                    foldoutArrow = new FoldoutArrow((value) => { expanded = value; })
                    {
                        name = "superlist-foldout"
                    };
                    headerBar.Add(foldoutArrow);
                }

                //Label()
                {
                    label = new Label(property != null ? property.displayName : "Super List");
                    label.name = "superlist-label";
                    label.style
                        .Flex(grow: 1)
                        .Text(12, TextAnchor.MiddleLeft)
                        .Colors(color: .82f.Gray());
                    label.RegisterCallback<ClickEvent>((evt) =>
                    {
                        if (arraySize == 0) return;
                        foldoutArrow.SetExpanded(!expanded);
                    });
                    label.focusable = true;
                    //label.RegisterCallback < UnityEngine.UIElements.Focus >
                    headerBar.Add(label);
                }

                //ElementCounter()
                {
                    elementCounter = new Label((property != null) ? property.arraySize.ToString() : "0");

                    elementCounter.name = "superlist-counter";
                    elementCounter.style
                        .FixedSize(width: 36)
                        .Text(null, TextAnchor.MiddleRight)
                        .Colors(color: .85f.Gray())
                        .Margins(right: 6);
                    headerBar.Add(elementCounter);
                }

                //AddButton()
                {
                    addButton = new Button(() => { AddButtonPressed_Default(); })
                    {
                        text = "+",
                        name = "superlist-add"
                    };
                    //addButton.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

                    addButton.style
                        .FixedSize(24, 18)
                        .Colors(null, Color.clear, Color.clear)
                        .Text(14, TextAnchor.LowerCenter)
                        .Border(0)
                        .Radius(0, topRight: 6)
                        .Margins(0)
                        .Padding(0);

                    headerBar.Add(addButton);
                }

                this.headerBar = headerBar;
                this.Add(headerBar);
            }

            //CollectionBackground()
            {
                collectionBackground = new() { name = "superlist-collection" };
                collectionBackground.style
                    .Colors(null, .254902f.Gray(), .1411765f.Gray())
                    .Padding(horizontal: 4)
                    .Border(1, top: 0)
                    .Radius(0, bottom: 4)
                    .Flex(FlexDirection.Column);
                collectionBackground.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;

                this.Add(collectionBackground);
            }
        }

        public class Item : VisualElement
        {
            public Item(SerializedObject owner, string elementPath, SuperList<T> parent, PropertyToVisualElementDelegate drawBody)
            {
                ownerObjectLocal = owner;
                elementPropertyPath = elementPath;
                parentList = parent;

                // Background container is the Item root itself
                name = "superlist-item";

                style.Flex(FlexDirection.Row, 1).Align(Align.Center, Justify.FlexStart)
                    .Padding(vertical: 4).Border(vertical: 1)
                    .Colors(null, Color.clear, new(0, 0, 0, .1f));
                style.flexGrow = 1;
                style.minHeight = 18;

                //Drag Handle (container that stretches full height so clicks work anywhere in the column)
                {
                    // Use a Button so the handle is an accessible, clickable control, but style it to have no background or border.
                    var handleBtn = new Button() { name = "superlist-item-grab-symbol" };

                    // Fixed width column, stretched vertically to match the item's height.
                    handleBtn.style
                        .FixedSize(width: 18)
                        .Align(null, null, Align.Stretch)
                        .Flex(shrink: 0)
                        .Margins(left: 2, right: 2);

                    // Make the button visually minimal: no background, no border, transparent hover/active.
                    handleBtn.style
                        .Colors(null, Color.clear, Color.clear)
                        .Border(0)
                        .Padding(0);

                    // Center contents inside the container
                    handleBtn.style.justifyContent = Justify.Center;
                    handleBtn.style.alignItems = Align.Center;

                    // Ensure it sits above potential overlapping content and is positioned normally
                    handleBtn.style.position = Position.Relative;

                    // Make the control focusable so it reliably receives pointer/click events across editor versions.
                    handleBtn.focusable = true;

                    // Inner glyph label (purely visual)
                    var glyph = new Label("≡") { name = "superlist-item-grab-glyph" };
                    glyph.style
                        .Text(null, TextAnchor.MiddleCenter)
                        .Align(null, null, Align.Center)
                        .FixedSize(width: 16);
                    glyph.style.flexGrow = 0;
                    glyph.style.maxWidth = 16;
                    glyph.style.marginTop = 0;
                    glyph.style.marginBottom = 0;

                    handleBtn.Add(glyph);

                    // Use PointerDownEvent for robust selection handling in the editor.
                    // Stop propagation so clicks on the handle don't select other UI or trigger other callbacks.
                    handleBtn.RegisterCallback<PointerDownEvent>((evt) =>
                    {
                        SelectSelf();
                        evt.StopPropagation();
                    });

                    // Also handle PointerUp to stop bubbling just in case (prevents accidental parent handlers).
                    handleBtn.RegisterCallback<PointerUpEvent>((evt) =>
                    {
                        evt.StopPropagation();
                    });

                    // Assign to the public property (type is VisualElement) so rest of code stays unchanged.
                    dragHandle = handleBtn;

                    this.Add(dragHandle);
                }

                //Content
                {
                    content = new VisualElement();
                    content.style.Flex(FlexDirection.Row, 1, 1).Align(Align.Center);
                    content.style.alignSelf = Align.Stretch;
                    this.Add(content);
                }

                //Remove Button
                {
                    removeButton = new Button(() => { parentList?.RemoveButtonPressed_Default(this); })
                    {
                        text = "-",
                        name = "superlist-item-remove"
                    };
                    removeButton.style
                        .FixedSize(16, 16)
                        .Margins(left: 6)
                        .Border(0)
                        .Text(null, TextAnchor.MiddleCenter)
                        .Colors(.78f.Gray());

                    // Prevent remove button clicks from selecting the item
                    removeButton.RegisterCallback<ClickEvent>((evt) => evt.StopPropagation());
                    this.Add(removeButton);
                }



#if UNITY_EDITOR
                // Build the body using a fresh SerializedProperty resolved from the owner object.
                VisualElement body = null;
                try
                {
                    SerializedProperty freshProp = ownerObjectLocal.FindProperty(elementPropertyPath);
                    if (drawBody != null)
                    {
                        try
                        {
                            body = drawBody(freshProp);
                        }
                        catch
                        {
                            body = null;
                        }
                    }

                    if (body == null && freshProp != null)
                    {
                        // fallback: a simple PropertyField bound to the property
                        var pf = new UnityEditor.UIElements.PropertyField(freshProp);
                        pf.style.minHeight = 16;
                        pf.style.height = StyleKeyword.Auto;
                        pf.style.flexGrow = 1;
                        pf.style.flexShrink = 1;
                        pf.style.alignSelf = Align.Stretch;
                        body = pf;
                    }

                    if (body != null)
                    {
                        // Make the body stretch to fit available space
                        body.style.flexGrow = 1;
                        body.style.flexShrink = 1;
                        body.style.alignSelf = Align.Stretch;

                        // Do NOT register the body to select the item; selection is only via drag handle.

                        // Bind the body to the owner object so it updates correctly
                        try { body.Bind(ownerObjectLocal); } catch { }

                        content.Add(body);
                    }

                }
                catch
                {

                }
#endif
            }

            public VisualElement dragHandle { get; private set; }
            public VisualElement content { get; private set; }
            public Button removeButton { get; private set; }

            // Data

            private SerializedObject ownerObjectLocal;
            private string elementPropertyPath;
            private SuperList<T> parentList;
            private bool _selected = false;
            public bool selected
            {
                get => _selected;
                private set
                {
                    _selected = value;
                    // Visual feedback for selection: subtle highlight and border
                    style.backgroundColor = new StyleColor(_selected ? new Color(0.14f, 0.24f, 0.42f, 0.12f) : Color.clear);
                }
            }

            // Select this item and clear selection on siblings
            private void SelectSelf()
            {
                if (parentList?.items != null)
                {
                    foreach (var it in parentList.items)
                    {
                        if (it != null && it != this) it.SetSelected(false);
                    }
                }
                SetSelected(true);
            }

            public void SetSelected(bool value)
            {
                selected = value;
            }
        }


    }


    public class CachedElement<T> : object where T : VisualElement
    {
        public CachedElement(VisualElement root, string name = null, string ussClassName = null, bool buildNow = false)
        {
            Root = root;
            Name = name;
            USSClassName = ussClassName;
            if (buildNow) Build();
        }
        public CachedElement(VisualElement root, string name = null, string ussClassName = null, Action<T> resultEvent = null)
        {
            Root = root;
            Name = name;
            USSClassName = ussClassName;
            if (resultEvent != null && Valid(out T e)) resultEvent?.Invoke(e);
        }


        public VisualElement Root { get; private set; }
        public T E => value ?? Build();
        public T Element => value ?? Build();
        private T value;
        public string Name { get; private set; }
        public string USSClassName { get; private set; }

        public T Build()
        {
            value = Root.Q<T>(Name, USSClassName);
            return value;
        }
        public bool Valid(out T result)
        {
            result = E;
            return E != null;
        }

        public void GetAndDo(Action<T> result)
        {
            if (Valid(out T e)) result?.Invoke(e);
        }
    }




    /// <summary>
    /// Doesn't work for my purposes. CRAP.
    /// </summary>
#if UNITY_EDITOR
    public abstract class LimitedListDrawer : PropertyDrawer
    {
        protected ListView listView { get; private set; }
        protected SerializedProperty rootProperty { get; private set; }
        protected System.Collections.IList itemsSource { get; private set; }

        // Plan / Pseudocode (detailed):
        // 1. When creating the ListView, set a fixed item height so the ListView can
        //    layout items correctly and avoid overlapping. UIElements ListView uses
        //    virtualization and requires a fixed height per item.
        // 2. Choose the fixed height based on Unity editor single line height plus any
        //    vertical padding used in MakeListItem. This keeps rows aligned and prevents overlap.
        // 3. Also set a minimum/explicit height on the VisualElement created in MakeListItem
        //    so each row actually measures to at least that height when rendered.
        // 4. Give the ListView a flexible vertical layout (flexGrow = 1) so it lays out
        //    in the inspector as expected.
        // 5. If you need variable-height rows, switch away from ListView virtualization to a
        //    non-virtualized container (e.g., manually build children into a VisualElement or use IMGUI fallback),
        //    because UIElements ListView does not support variable row heights.
        //
        // Implementation details:
        // - After constructing ListView set 'fixedItemHeight' to EditorGUIUtility.singleLineHeight + padding.
        // - In MakeListItem set container.style.height and container.style.minHeight to the same value.
        // - Keep existing makeItem/bindItem wiring intact.
        // - Ensure ApplyAndRebuild keeps listView.itemsSource in sync.

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            rootProperty = property;

            // Resolve the runtime IList backing this serialized property, fallback to ArrayList if unresolved
            itemsSource = ResolveIList(rootProperty) ?? new ArrayList();

            // Create the ListView. Use the itemsSource and then assign makeItem/bindItem.
            listView = new ListView(itemsSource);

            // IMPORTANT: set a fixed item height so the ListView knows how to spacing rows.
            // Use a base of one editor line plus the padding used in MakeListItem (2 top + 2 bottom).
            float rowPadding = 4f; // corresponds to margin/padding used in MakeListItem
            float itemHeight = EditorGUIUtility.singleLineHeight + rowPadding;
            // Depending on Unity version the property name is 'fixedItemHeight' and exists on ListView.
            // Assign it so virtualization can compute positions and avoid overlapping rows.
            listView.fixedItemHeight = itemHeight;

            // Let the list expand to fill available vertical space in the inspector.
            listView.style.flexGrow = 1;

            listView.showFoldoutHeader = true;
            listView.headerTitle = property.displayName;
            listView.showAddRemoveFooter = true;
            listView.reorderMode = ListViewReorderMode.Animated;

            // wire up item creation & binding
            listView.makeItem = () => MakeListItem();
            listView.bindItem = (element, index) =>
            {
                // Ensure itemsSource is current (in case of change)
                if (itemsSource == null)
                    itemsSource = ResolveIList(rootProperty) ?? new ArrayList();

                BindListItem(element, index, itemsSource);
            };

            listView.onAdd = (b) => InternalOnAdd();
            listView.onRemove = (b) => InternalOnRemove();

            OnInitialize();
            return listView;
        }

        protected virtual void OnInitialize() { }

        // ----------- Overridable hooks -----------
        // Create a VisualElement instance for each list row
        protected virtual VisualElement MakeListItem()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.paddingLeft = 2;
            container.style.paddingRight = 2;
            // Use margins consistent with original code
            container.style.marginTop = 2;
            container.style.marginBottom = 2;

            // Ensure the element has an explicit/minimum height so ListView row measurement matches.
            float rowPadding = 4f; // top+bottom used above in CreatePropertyGUI
            float itemHeight = EditorGUIUtility.singleLineHeight + rowPadding;
            container.style.height = itemHeight;
            container.style.minHeight = itemHeight;

            // default placeholder
            container.Add(new Label("Item"));
            return container;
        }

        // Bind data into the provided element given the index and a live IList
        protected virtual void BindListItem(VisualElement element, int index, System.Collections.IList list)
        {
            element.Clear();
            if (list == null || index < 0 || index >= list.Count)
            {
                element.Add(new Label("Empty"));
                return;
            }

            var item = list[index];
            element.Add(new Label(item?.ToString() ?? "Null"));
        }

        // Called when Add button is pressed. Default adds a default instance for generic List<T> or null.
        protected virtual void OnAdd(System.Collections.IList list)
        {
            if (list == null) return;

            // Try to construct a default element for generic List<T>
            try
            {
                var t = list.GetType();
                if (t.IsGenericType)
                {
                    var genArgs = t.GetGenericArguments();
                    if (genArgs.Length == 1)
                    {
                        var elemType = genArgs[0];
                        object newElem = null;
                        try
                        {
                            // Try parameterless constructor
                            newElem = Activator.CreateInstance(elemType);
                        }
                        catch
                        {
                            newElem = null;
                        }
                        list.Add(newElem);
                        ApplyAndRebuild();
                        return;
                    }
                }
            }
            catch
            {
                // ignore and fallback
            }

            // Fallback: add null
            list.Add(null);
        }

        // Called when Remove button is pressed. Default removes the selected index.
        protected virtual void OnRemove(System.Collections.IList list, int index)
        {
            if (list == null) return;
            if (index < 0 || index >= list.Count) return;
            list.RemoveAt(index);
        }

        // ----------- Internal wiring -----------
        void InternalOnAdd()
        {
            var list = ResolveIList(rootProperty) ?? itemsSource;
            OnAdd(list);
            ApplyAndRebuild();
        }

        void InternalOnRemove()
        {
            var list = ResolveIList(rootProperty) ?? itemsSource;
            int sel = listView.selectedIndex;
            if (sel < 0) return;
            OnRemove(list, sel);
            ApplyAndRebuild();
        }

        void ApplyAndRebuild()
        {
            try
            {
                rootProperty?.serializedObject?.ApplyModifiedProperties();
            }
            catch { /* ignore */ }

            // refresh itemsSource reference
            itemsSource = ResolveIList(rootProperty) ?? itemsSource;
            if (listView != null)
            {
                listView.itemsSource = itemsSource;
                listView.Rebuild();
            }
        }

        // ----------- Helpers to resolve runtime IList backing the serialized property -----------
        private static System.Collections.IList ResolveIList(SerializedProperty property)
        {
            if (property == null) return null;
            var so = property.serializedObject;
            if (so == null) return null;
            var target = so.targetObject;
            if (target == null) return null;

            object currentObject = target;
            Type currentType = currentObject.GetType();

            string path = property.propertyPath;
            var tokens = path.Split('.');

            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];

                if (token == "Array") continue;

                if (token.StartsWith("data["))
                {
                    // The previous token resolved to a collection instance; return it if IList
                    if (currentObject is System.Collections.IList list) return list;
                    return null;
                }

                var field = GetFieldInfoRecursive(currentType, token);
                if (field == null) return null;

                currentObject = field.GetValue(currentObject);
                if (currentObject == null)
                {
                    // If this is the final token, it might be the list field but currently null.
                    // We do not attempt to create a new list here to avoid mutating data unexpectedly.
                    if (i == tokens.Length - 1)
                    {
                        // If the declared field type implements IList, we could return null and let caller fallback.
                        return null;
                    }
                    return null;
                }

                currentType = currentObject.GetType();
            }

            if (currentObject is System.Collections.IList finalList) return finalList;
            return null;
        }

        private static FieldInfo GetFieldInfoRecursive(Type type, string fieldName)
        {
            while (type != null)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var fi = type.GetField(fieldName, flags);
                if (fi != null) return fi;
                var backing = $"<{fieldName}>k__BackingField";
                fi = type.GetField(backing, flags);
                if (fi != null) return fi;
                type = type.BaseType;
            }
            return null;
        }
    }
#endif
}

