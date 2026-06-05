using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.UIElements;
using UnityEngine;

namespace SLS.EditorUtilities.Editor
{
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
        public FoldoutArrow(Action<bool> clickEvent = null, bool initialValue = false) : base()
        {
            this.clickEvent = clickEvent;

            clicked += () => { Expanded = !isExpanded; };

            style.color = new StyleColor(Color.gray4);
            style.width = 18;
            style.height = 16;
            style.unityTextAlign = TextAnchor.MiddleCenter;

            style.backgroundColor = new StyleColor(Color.clear);
            style.Border(0, color: Color.clear).Radius(0).Padding(0).Margins(0);

            SetValueWithoutNotify(initialValue);

            this.style.color = DefaultColor;
            new ElementHighlight(this, SelectedColor).Select();
        }

        public bool Expanded
        {
            get => isExpanded;
            set
            {
                isExpanded = value;
                base.text = value ? "▼" : "▶";
                clickEvent?.Invoke(isExpanded);
            }
        }
        public bool Expandable
        {
            get => isExpandable;
            set
            {
                this.SetEnabled(value);
                this.style.visibility = value ? Visibility.Visible : Visibility.Hidden;
                if (!value) Expanded = false;
            }

        }
        private bool isExpanded = true;
        private bool isExpandable = true;
        private Action<bool> clickEvent;
        new private VisualElement text = null;

        public static Color DefaultColor { get; private set; } = .408f.Gray();
        public static Color SelectedColor { get; private set; } = new(.282f, .439f, .835f);

        public void SetValueWithoutNotify(bool value)
        {
            isExpanded = value;
            base.text = value ? "▼" : "▶";
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
}

