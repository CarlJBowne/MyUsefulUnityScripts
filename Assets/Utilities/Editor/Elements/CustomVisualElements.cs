using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.UIElements;
using UnityEngine;

namespace Utilities.Xtensions.VisualElements
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
            new ElementHighlighter(this, SelectedColor).Select();
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

