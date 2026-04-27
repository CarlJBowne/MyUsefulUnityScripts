using System;
using System.Numerics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Utilities.Xtensions.VisualElements
{
    public static class Xtensions_VisualElements_Core
    {
        public static void CreateAddAndStore<T>(this VisualElement target, out T result, Func<T> process) where T : VisualElement
        {
            result = process();
            target.Add(result);
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
        public static void Highlighter(this VisualElement V, float factor, bool clamp)
        {
            Color initialColor = V.style.color.value;
            Color initialColorBack = V.style.backgroundColor.value;
            Color initialColorBorder = V.style.borderTopColor.value;

            V.RegisterCallback<MouseOverEvent>(Hover);
            V.RegisterCallback<MouseLeaveEvent>(UnHover);

            Color H(Color IN)
            {
                IN.r += factor;
                IN.g += factor;
                IN.b += factor;
                if (clamp)
                {
                    if (IN.r < 0f) IN.r = 0f; if (IN.r > 1f) IN.r = 1f;
                    if (IN.g < 0f) IN.g = 0f; if (IN.g > 1f) IN.g = 1f;
                    if (IN.b < 0f) IN.b = 0f; if (IN.b > 1f) IN.b = 1f;
                }
                return IN;
            }
            void Hover(MouseOverEvent E)
            {
                V.style.color = H(initialColor);
                V.style.backgroundColor = H(initialColorBack);
                V.style.borderBottomColor = H(initialColorBorder);
                V.style.borderLeftColor = H(initialColorBorder);
                V.style.borderRightColor = H(initialColorBorder);
                V.style.borderTopColor = H(initialColorBorder);
            }
            void UnHover(MouseLeaveEvent E)
            {
                V.style.color = initialColor;
                V.style.backgroundColor = initialColorBack;
                V.style.borderBottomColor = initialColorBorder;
                V.style.borderLeftColor = initialColorBorder;
                V.style.borderRightColor = initialColorBorder;
                V.style.borderTopColor = initialColorBorder;
            }
        }
        public static void Highlighter(this VisualElement V, Func<Color, Color> Process)
        {
            Color initialColor = V.style.color.value;
            Color initialColorBack = V.style.backgroundColor.value;
            Color initialColorBorder = V.style.borderTopColor.value;

            V.RegisterCallback<MouseOverEvent>(Hover);
            V.RegisterCallback<MouseLeaveEvent>(UnHover);

            void Hover(MouseOverEvent E)
            {
                V.style.color = Process(initialColor);
                V.style.backgroundColor = Process(initialColorBack);
                V.style.borderBottomColor = Process(initialColorBorder);
                V.style.borderLeftColor = Process(initialColorBorder);
                V.style.borderRightColor = Process(initialColorBorder);
                V.style.borderTopColor = Process(initialColorBorder);
            }
            void UnHover(MouseLeaveEvent E)
            {
                V.style.color = initialColor;
                V.style.backgroundColor = initialColorBack;
                V.style.borderBottomColor = initialColorBorder;
                V.style.borderLeftColor = initialColorBorder;
                V.style.borderRightColor = initialColorBorder;
                V.style.borderTopColor = initialColorBorder;
            }

        }

        public static void RegisterHoverEvents(this VisualElement V, Action<bool> hovered)
        {
            V.RegisterCallback<MouseOverEvent>(Do);
            V.RegisterCallback<MouseLeaveEvent>(Do);
            void Do(EventBase E) => hovered?.Invoke(E is MouseOverEvent);
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








    }

    public static class Xtensions_VisualElements_CustomStyles
    {

    }
}

