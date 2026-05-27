using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ListUtilities.Editor.Internal
{
    internal static class Xtensions
    {
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
        public static bool QCache<T>(this VisualElement V, out T result, string name = null, string className = null) where T : VisualElement
        {
            result = V.Q<T>(name, className) ?? null;
            return result != null;
        }
        public static void DelayedBuild(this VisualElement V, Action result) =>
            V.RegisterCallbackOnce<AttachToPanelEvent>(_ => V.schedule.Execute(result));

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
    internal static class Xtensions_VisualElements_StyleBuilders
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
    internal class ElementHighlighter
    {
        public ElementHighlighter(VisualElement source)
        {
            target = source;
            Init();
        }
        public ElementHighlighter(VisualElement source, Color? setMain = null, Color? setBack = null, Color? setBorder = null,
            float? raiseMain = null, float? raiseBack = null, float? raiseBorder = null)
        {
            target = source;
            sMain = setMain;
            sBack = setBack;
            sBorder = setBorder;
            rMain = raiseMain;
            rBack = raiseBack;
            rBorder = raiseBorder;
            Init();
        }
        public ElementHighlighter(VisualElement source, float? raiseMain = null, float? raiseBack = null, float? raiseBorder = null)
        {
            target = source;
            rMain = raiseMain;
            rBack = raiseBack;
            rBorder = raiseBorder;
            Init();
        }

        void Init()
        {
            target.schedule.Execute(Do);
            //target.RegisterCallbackOnce<AttachToPanelEvent>(ev => , TrickleDown.TrickleDown);
            void Do()
            {
                iMain = target.resolvedStyle.color;
                iBack = target.resolvedStyle.backgroundColor;
                iBorderTop = target.resolvedStyle.borderTopColor;
                iBorderBottom = target.resolvedStyle.borderBottomColor;
                iBorderLeft = target.resolvedStyle.borderLeftColor;
                iBorderRight = target.resolvedStyle.borderRightColor;
            }
        }

        VisualElement target;

        public Color iMain { get; private set; }
        public Color iBack { get; private set; }
        public Color iBorderTop { get; private set; }
        public Color iBorderBottom { get; private set; }
        public Color iBorderLeft { get; private set; }
        public Color iBorderRight { get; private set; }

        public Color? sMain;
        public float? rMain;

        public Color? sBack;
        public float? rBack;

        public Color? sBorder;
        public float? rBorder;


        public void Hover()
        {
            target.RegisterCallback<MouseOverEvent>(Hover, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseLeaveEvent>(UnHover, TrickleDown.TrickleDown);

            void Hover(MouseOverEvent E) => ApplyHighlight();
            void UnHover(MouseLeaveEvent E) => ResetHighlight();
        }
        public void Select()
        {
            target.RegisterCallback<FocusEvent>(ev => ApplyHighlight(), TrickleDown.TrickleDown);
            target.RegisterCallback<BlurEvent>(ev => ResetHighlight(), TrickleDown.TrickleDown);
            target.focusable = true;
        }
        public void Click()
        {
            target.RegisterCallback<MouseDownEvent>(Hover, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseUpEvent>(UnHover, TrickleDown.TrickleDown);

            void Hover(MouseDownEvent E)
            {
                if (E.target != target) return;
                ApplyHighlight();
            }
            void UnHover(MouseUpEvent E)
            {
                if (E.target != target) return;
                ResetHighlight();
            }
        }

        void ApplyHighlight()
        {
            if (sMain.HasValue) target.style.color = sMain.Value;
            else if (rMain.HasValue) target.style.color = RaiseColor(iMain, rMain.Value);

            if (sBack.HasValue) target.style.backgroundColor = sBack.Value;
            else if (rBack.HasValue) target.style.backgroundColor = RaiseColor(iBack, rBack.Value);

            if (sBorder.HasValue)
            {
                target.style.borderTopColor = sBorder.Value;
                target.style.borderBottomColor = sBorder.Value;
                target.style.borderLeftColor = sBorder.Value;
                target.style.borderRightColor = sBorder.Value;
            }
            else if (rBorder.HasValue)
            {
                target.style.borderTopColor = RaiseColor(iBorderTop, rBorder.Value);
                target.style.borderBottomColor = RaiseColor(iBorderBottom, rBorder.Value);
                target.style.borderLeftColor = RaiseColor(iBorderLeft, rBorder.Value);
                target.style.borderRightColor = RaiseColor(iBorderRight, rBorder.Value);
            }
        }
        void ResetHighlight()
        {
            target.style.color = iMain;
            target.style.backgroundColor = iBack;
            target.style.borderTopColor = iBorderTop;
            target.style.borderBottomColor = iBorderBottom;
            target.style.borderLeftColor = iBorderLeft;
            target.style.borderRightColor = iBorderRight;
        }

        Color RaiseColor(Color input, float r) => new(input.r + r, input.g + r, input.b + r);

        public static float ButtonHoverBackRaise { get; private set; } = .404f - .345f;
        public static Color ButtonClickedBack { get; private set; } = new(.275f, .376f, .486f);
        public static Color ButtonSelectedOutline { get; private set; } = new(.482f, .682f, .980f);
        public static Color ButtonBack { get; private set; } = .345f.Gray();
        public static Color ButtonBorder { get; private set; } = .188f.Gray();
        public static Color ButtonBorderBottom { get; private set; } = .141f.Gray();
        public static Color ButtonText { get; private set; } = .933f.Gray();
        public static Color Text { get; private set; } = .824f.Gray();
        public static Color TextSelected { get; private set; } = new(.506f, .706f, 1);
        public static Color FoldoutArrow { get; private set; } = .408f.Gray();
        public static Color FoldoutArrowSelected { get; private set; } = new(.282f, .439f, .835f);

        public static void ButtonDefault(VisualElement target)
        {
            new ElementHighlighter(target, null, ButtonHoverBackRaise).Hover();
            new ElementHighlighter(target, null, ButtonClickedBack).Click();
            new ElementHighlighter(target, null, null, ButtonSelectedOutline).Select();
        }
        public static void ButtonStyle(VisualElement target, float? hoverAmount = null, Color? clickColor = null, Color? selectOutline = null)
        {
            new ElementHighlighter(target, null, hoverAmount ?? ButtonHoverBackRaise).Hover();
            new ElementHighlighter(target, null, clickColor ?? ButtonClickedBack).Click();
            new ElementHighlighter(target, null, null, selectOutline ?? ButtonSelectedOutline).Select();
        }
        public static void ButtonStyle(VisualElement target, Color? hoverAmount = null, Color? clickColor = null, Color? selectOutline = null)
        {
            (hoverAmount.HasValue
            ? new ElementHighlighter(target, null, hoverAmount)
            : new ElementHighlighter(target, null, ButtonHoverBackRaise)
            ).Hover();
            new ElementHighlighter(target, null, clickColor ?? ButtonClickedBack).Click();
            new ElementHighlighter(target, null, null, selectOutline ?? ButtonSelectedOutline).Select();
        }
        public static void TextDefault(VisualElement target) => new ElementHighlighter(target, TextSelected).Select();
    }
}