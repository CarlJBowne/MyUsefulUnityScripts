using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SLS.EditorUtilities.Editor
{
    public static class Xtensions_VisualElements_Core
    {
        public static void CreateAddAndStore<T>(this VisualElement target, out T result, Func<T> process) where T : VisualElement
        {
            result = process();
            target.Add(result);
        }
        public static void CreateAddAndStore<T>(this VisualElement target, out T result, T input) where T : VisualElement
        {
            result = input;
            target.Add(result);
        }
        public static T AddTo<T>(this T input, VisualElement target, Action<T> PostMake = null) where T : VisualElement
        {
            if (input == null) return null;
            target?.Add(input);
            PostMake?.Invoke(input);
            return input;
        }
        public static T AddTo<T>(this T input, VisualElement.Hierarchy target, Action<T> PostMake = null) where T : VisualElement
        {
            if (input == null) return null;
            target.Add(input);
            PostMake?.Invoke(input);
            return input;
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

        public static T DelayedBuild<T>(this T V, Action result) where T : VisualElement
        {
            V.RegisterCallbackOnce<AttachToPanelEvent>(_ => V.schedule.Execute(result));
            return V;
        }

        public static void ShrinkToTextWidth(this Label l)
        {
            void DO()
            {
                float width = EditorStyles.label.CalcSize(new(l.text)).x + 2;
                if (width < l.style.width.value.value)
                    l.style.width = width;
            }
            l.RegisterValueChangedCallback(_ => DO());
        }

        public static bool QCache<T>(this VisualElement V, out T result, string name = null, string className = null) where T : VisualElement
        {
            result = V.Q<T>(name, className) ?? null;
            return result != null;
        }

        public static void RegisterHoverEvents(this VisualElement V, Action<bool> hovered)
        {
            V.RegisterCallback<MouseOverEvent>(Do);
            V.RegisterCallback<MouseLeaveEvent>(Do);
            void Do(EventBase E) => hovered?.Invoke(E is MouseOverEvent);
        }

        public static void MoveCallback<E>(this VisualElement From, VisualElement To, TrickleDown trickleDown = TrickleDown.NoTrickleDown, Func<bool> Conditional = null)
            where E : EventBase<E>, new()
        {
            From.RegisterCallback<E>(evt =>
            {
                if (Conditional != null && !Conditional()) return;
                evt.Dispose();
                E newEvt = EventBase<E>.GetPooled();
                newEvt.target = To;
                To.panel.visualTree.SendEvent(evt);
            }, trickleDown);
        }
    }

    public static class Xtensions_VisualElements_StyleBuilders
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

        public static IStyle Display(this IStyle v, bool value)
        {
            v.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            return v;
        }
        public static void Display(this VisualElement v, bool value) => v.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        public static bool IsDisplay(this VisualElement v) => v.style.display == DisplayStyle.Flex;






    }

    public static class Xtensions_VisualElements_CustomStyles
    {

    }

    public static class Xtensions_Editor_General
    {
        public static string BackingField(this string input) => $"<{input}>k__BackingField";

        public static void Reset(this SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: prop.intValue = 0; break;
                case SerializedPropertyType.Float: prop.floatValue = 0f; break;
                case SerializedPropertyType.Boolean: prop.boolValue = false; break;
                case SerializedPropertyType.String: prop.stringValue = string.Empty; break;
                case SerializedPropertyType.Vector2: prop.vector2Value = new(); break;
                case SerializedPropertyType.Vector3: prop.vector3Value = new(); break;
                case SerializedPropertyType.Vector2Int: prop.vector2IntValue = new(); break;
                case SerializedPropertyType.Vector3Int: prop.vector3IntValue = new(); break;
                case SerializedPropertyType.Color: prop.colorValue = Color.clear; break;
                case SerializedPropertyType.Bounds: prop.boundsValue = new Bounds(); break;
                case SerializedPropertyType.BoundsInt: prop.boundsIntValue = new BoundsInt(); break;
                case SerializedPropertyType.Enum: prop.enumValueIndex = 0; break;
                case SerializedPropertyType.Rect: prop.rectValue = new(); break;
                case SerializedPropertyType.RectInt: prop.rectIntValue = new(); break;
                case SerializedPropertyType.AnimationCurve: prop.animationCurveValue = new(); break;
                case SerializedPropertyType.Gradient: prop.gradientValue = new(); break;
                case SerializedPropertyType.LayerMask: prop.intValue = 0; break;
                case SerializedPropertyType.Quaternion: prop.quaternionValue = new(); break;
                case SerializedPropertyType.Vector4: prop.vector4Value = new(); break;

                // General Types
                case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = null; break;
                case SerializedPropertyType.ExposedReference: prop.exposedReferenceValue = null; break;
                case SerializedPropertyType.ManagedReference:
                    System.Type type = Type.GetType(prop.managedReferenceFieldTypename); // Requires reflection fallback
                    if (type != null && type.IsValueType)
                        prop.boxedValue = System.Activator.CreateInstance(type);
                    break;
                // Generic handles custom structs and serialization hierarchies safely
                case SerializedPropertyType.Generic:
                    if (prop.isArray)
                    {
                        prop.ClearArray();
                    }
                    else
                    {
                        // If it's a valid struct/class wrapper, boxedValue can handle it 
                        // provided you reflect the true default constructor type.
                        System.Type attemptType = Type.GetType(prop.type); // Requires reflection fallback
                        if (attemptType != null && attemptType.IsValueType)
                            prop.boxedValue = System.Activator.CreateInstance(attemptType);
                    }
                    break;
                default: break;
            }
        }
        /// <summary>
        /// Attempts to safely set the value of a SerializedProperty using a generic input.
        /// </summary>
        public static void SetGenericValue<T>(this SerializedProperty prop, T value)
        {
            if (prop == null) throw new ArgumentNullException(nameof(prop));

            // 1. Direct boxed assignment for custom Structs, Classes, and Managed References
            try
            {
                // boxedValue handles custom serializable structs and objects beautifully
                prop.boxedValue = value;
                return;
            }
            catch (Exception)
            {
                try
                {
                    switch (value)
                    {
                        case int i: prop.intValue = i; break;
                        case float f: prop.floatValue = f; break;
                        case bool b: prop.boolValue = b; break;
                        case string s: prop.stringValue = s ?? string.Empty; break;
                        case Vector2 v2: prop.vector2Value = v2; break;
                        case Vector3 v3: prop.vector3Value = v3; break;
                        case Vector2Int v2I: prop.vector2IntValue = v2I; break;
                        case Vector3Int v3I: prop.vector3IntValue = v3I; break;
                        case Vector4 v4: prop.vector4Value = v4; break;
                        case Quaternion q: prop.quaternionValue = q; break;
                        case Color c: prop.colorValue = c; break;
                        case Gradient g: prop.gradientValue = g; break;
                        case AnimationCurve ac: prop.animationCurveValue = ac; break;
                        case Bounds b: prop.boundsValue = b; break;
                        case BoundsInt bi: prop.boundsIntValue = bi; break;
                        case Rect r: prop.rectValue = r; break;
                        case RectInt ri: prop.rectIntValue = ri; break;
                        case UnityEngine.Object obj: prop.objectReferenceValue = obj; break;
                        case LayerMask lm: prop.intValue = lm.value; break;

                        default:
                            // 3. Last resort edge cases (e.g., passing 'null' to reference properties)
                            if (value == null)
                            {
                                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                                {
                                    prop.objectReferenceValue = null;
                                }
                            }
                            else
                            {
                                Debug.LogError($"[SetGenericValue] Cannot apply value of type '{typeof(T)}' to SerializedProperty '{prop.propertyPath}' (Type: {prop.propertyType})");
                            }
                            break;
                    }
                }
                catch (Exception) { }
            }
        }
    }

}

//#if UNITY_STANDALONE_WIN
//    public static class User32
//    {
//        [DllImport("user32.dll")]
//        public static extern long GetCursorPos(ref POINT point);
//
//        [DllImport("user32.dll")]
//        public static extern long SetCursorPos(int x, int y);
//
//        [StructLayout(LayoutKind.Sequential)]
//        public struct POINT
//        {
//            public int x;
//            public int y;
//        }
//    }
//#endif