using UnityEngine;
using UnityEngine.UIElements;

namespace SLS.EditorUtilities.Editor
{
    public class ElementHighlight
    {
        public ElementHighlight(VisualElement source)
        {
            target = source;
            Init();
        }
        public ElementHighlight(VisualElement source, Color? setMain = null, Color? setBack = null, Color? setBorder = null,
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
        public ElementHighlight(VisualElement source, float? raiseMain = null, float? raiseBack = null, float? raiseBorder = null)
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
            new ElementHighlight(target, null, ButtonHoverBackRaise).Hover();
            new ElementHighlight(target, null, ButtonClickedBack).Click();
            new ElementHighlight(target, null, null, ButtonSelectedOutline).Select();
        }
        public static void ButtonStyle(VisualElement target, float? hoverAmount = null, Color? clickColor = null, Color? selectOutline = null)
        {
            new ElementHighlight(target, null, hoverAmount ?? ButtonHoverBackRaise).Hover();
            new ElementHighlight(target, null, clickColor ?? ButtonClickedBack).Click();
            new ElementHighlight(target, null, null, selectOutline ?? ButtonSelectedOutline).Select();
        }
        public static void ButtonStyle(VisualElement target, Color? hoverColor = null, Color? clickColor = null, Color? selectOutline = null)
        {
            (hoverColor.HasValue
            ? new ElementHighlight(target, null, hoverColor)
            : new ElementHighlight(target, null, ButtonHoverBackRaise)
            ).Hover();
            new ElementHighlight(target, null, clickColor ?? ButtonClickedBack).Click();
            new ElementHighlight(target, null, null, selectOutline ?? ButtonSelectedOutline).Select();
        }
        public static void TextDefault(VisualElement target) => new ElementHighlight(target, TextSelected).Select();
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
}

