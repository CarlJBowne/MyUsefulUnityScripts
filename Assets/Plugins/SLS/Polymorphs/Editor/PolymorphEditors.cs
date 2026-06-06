using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using static Polymorph;
using SLS.ListUtilities.Editor;

public class PolymorphEditors
{

    #region Utilities

    public static void ShowChooseTypeMenu(Type baseType, bool showNullOption, Action<Type> result)
    {
        GenericMenu menu = new();


        Type[] types = GetSubtypes(baseType);
        if (types.Length == 0)
        {
            menu.AddItem(new GUIContent("Add"), false, () => { result?.Invoke(baseType); });
        }
        else
        {
            foreach (Type t in types)
            {
                if (t == baseType) continue;
                menu.AddItem(new GUIContent(t.Name), false, () => { result?.Invoke(t); });
            }

        }

        if (showNullOption) menu.AddItem(new GUIContent("Nullify"), false, () => { result?.Invoke(null); });

        menu.ShowAsContext();
    }

    private static Type GetDeclaredFieldType(SerializedProperty property)
    {
        if (property == null) return null;

        // If Unity gives a managedReferenceFieldTypename, try to parse it first.
        if (!string.IsNullOrEmpty(property.managedReferenceFieldTypename))
        {
            // managedReferenceFieldTypename can contain tokens; try to resolve each token to a Type.
            var parts = property.managedReferenceFieldTypename.Split(' ');
            foreach (var part in parts)
            {
                var t = Type.GetType(part);
                if (t != null) return t;
            }
        }

        // Fall back to reflection over the target object and the propertyPath.
        object target = property.serializedObject.targetObject;
        if (target == null) return null;

        Type currentType = target.GetType();
        string path = property.propertyPath;
        string[] tokens = path.Split('.');

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];

            if (token == "Array")
            {
                // 'Array' is followed by 'data[x]' token; the element type will be handled when we hit data[...]
                continue;
            }

            if (token.StartsWith("data["))
            {
                // The previous field was a collection; get its element type.
                if (currentType.IsArray)
                {
                    currentType = currentType.GetElementType() ?? currentType;
                }
                else if (currentType.IsGenericType)
                {
                    var genDef = currentType.GetGenericTypeDefinition();
                    if (genDef == typeof(List<>) || currentType.GetInterfaces().Any(iFace => iFace.IsGenericType && iFace.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                    {
                        currentType = currentType.GetGenericArguments()[0];
                    }
                    else
                    {
                        // Unknown collection type; abort resolution.
                        return null;
                    }
                }
                else
                {
                    // Unknown collection shape; cannot resolve element type.
                    return null;
                }
                continue;
            }

            FieldInfo field = GetFieldInfoRecursive(currentType, token);
            if (field == null)
            {
                // Could not find the field; abort.
                return null;
            }

            currentType = field.FieldType;
        }

        // If the final resolved type is a collection, return its element type.
        if (currentType.IsArray) return currentType.GetElementType() ?? currentType;
        if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(List<>))
            return currentType.GetGenericArguments()[0];

        return currentType;
    }

    private static FieldInfo GetFieldInfoRecursive(Type type, string fieldName)
    {
        while (type != null)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fi = type.GetField(fieldName, flags);
            if (fi != null) return fi;

            // Unity sometimes stores auto-property fields as backing fields with this pattern.
            string backing = $"<{fieldName}>k__BackingField";
            fi = type.GetField(backing, flags);
            if (fi != null) return fi;

            type = type.BaseType;
        }
        return null;
    }


    #endregion

    #region Core Drawers

    public class HeaderDrawer : Foldout
    {
        public HeaderDrawer(SerializedProperty property, Action onSetCallback = null)
        {
            this.property = property;
            bindingPath = property.propertyPath;
            BaseType = GetDeclaredFieldType(property) ?? typeof(Polymorph);
            CurrentType = this.property?.managedReferenceValue?.GetType();
            name = $"HeaderDrawer-{BaseType.Name}-{this.property.name}";
            OnSetCallback = onSetCallback;


            ChangeButton ??= new Button(ChangeButtonClicked)
            {
                name = "Type Chooser",
                text = "*",
                style =
                {
                    alignSelf = Align.FlexEnd,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    position = Position.Relative,
                    right = 0,
                    top = 0,
                    width = 20,
                    height = 16,
                    fontSize = 18,
                    marginRight = 0,
                    marginBottom = 0,
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 5,
                }
            };
            void ChangeButtonClicked() => ShowChooseTypeMenu(BaseType, CurrentType != null, UpdateType);

            Foldout.text = CorrectLabel;
            Foldout.bindingPath = property.propertyPath;
            Foldout.BindProperty(property);
            Foldout.style.flexGrow = 1f;


            this.DelayedBuild(Update);
        }

        #region Pieces

        public Foldout Foldout => this;
        public Toggle FoldoutToggle { get; protected set; }
        public VisualElement Arrow { get; protected set; }
        public Label Label
        { get; protected set; }
        public VisualElement ContentContainer { get; protected set; }
        public Button ChangeButton { get; protected set; }

        void BuildElements()
        {
            if (Foldout == null) return;
            if (FoldoutToggle != null && Label != null && ChangeButton != null && ContentContainer != null) return;

            FoldoutToggle ??= Foldout.Q<Toggle>(null, Foldout.toggleUssClassName);

            Label ??= FoldoutToggle.Q<Label>(null, "unity-label").AddTo(null, l =>
            {
                l.name = "HeaderDrawer--CustomLabel";
                l.text = CorrectLabel;
                l.style.flexGrow = 1;
                l.style.unityTextAlign = TextAnchor.MiddleLeft;
                l.RegisterCallback<PointerUpEvent>(evt =>
                {
                    // is it a right click?
                    if (evt.button == 1)
                    {
                        // copy the event and send it to the hidden label
                        using PointerUpEvent labelEvent = PointerUpEvent.GetPooled(evt);
                        labelEvent.target = Foldout;
                        Foldout.panel.visualTree.SendEvent(labelEvent);
                    }
                });
                Arrow = FoldoutToggle.Q(null, "unity-foldout__checkmark");
                l.Add(ChangeButton);
            });

            ContentContainer ??= Foldout.Q(null, Foldout.contentUssClassName);
            ContentContainer.style.marginLeft = 10;
        }


        #endregion

        #region Data

        public SerializedProperty property { get; protected set; }
        public Type BaseType { get; protected set; }
        public Type CurrentType { get; protected set; }
        bool bodyInvalid = true;
        public Action<Type> OnTypeChanged;
        public bool drawnSuccessfully { get; private set; } = false;
        Action OnSetCallback;
        string CorrectLabel => CurrentType != null ? $"{property.displayName} ({CurrentType.Name})" : property.displayName;

        void Update()
        {
            BuildElements();

            Arrow.style.visibility = property.managedReferenceValue != null ? Visibility.Visible : Visibility.Hidden;
            Arrow.SetEnabled(property.managedReferenceValue != null);
            if (property.managedReferenceValue == null) expanded = false;

            if (ContentContainer == null) return;
            if (property.managedReferenceValue is not null and Polymorph O && bodyInvalid)
            {
                ContentContainer.Clear();
                O.OverrideBody(ContentContainer, property);
            }
            else
            {
                if (property.managedReferenceValue is null) ContentContainer.Clear();
            }
        }

        #endregion

        void UpdateType(Type t) => UpdateType(t, false);
        void UpdateType(Type t, bool forceRebuild = false)
        {
            if (property == null || (t == CurrentType && !forceRebuild)) return;

            CurrentType = t;
            SetValueWithoutNotify(t != null ? Activator.CreateInstance(t) as Polymorph : null);

            FoldoutToggle.value = t != null;

            property.serializedObject.ApplyModifiedProperties();

            bodyInvalid = true;
            Update();

            OnTypeChanged?.Invoke(property?.managedReferenceValue?.GetType());
            OnSetCallback?.Invoke();
        }

        public bool expanded
        {
            get => base.value;
            set => base.value = value;
        }
        new public Polymorph value
        {
            get => property.managedReferenceValue as Polymorph;
            set
            {
                Polymorph oldVal = property.managedReferenceValue as Polymorph;
                try { property.serializedObject.Update(); } catch { }
                property.managedReferenceValue = value;
                try { property.serializedObject.ApplyModifiedProperties(); } catch { }
                Update();
                using ChangeEvent<Polymorph> evt = ChangeEvent<Polymorph>.GetPooled(oldVal, value);
                evt.target = this;
                SendEvent(evt);
            }
        }
        public void SetValueWithoutNotify(Polymorph newValue)
        {
            if (property != null)
            {
                try { property.serializedObject.Update(); } catch { }
                property.managedReferenceValue = newValue;
                try { property.serializedObject.ApplyModifiedProperties(); } catch { }
            }
            Update();
        }
    }
    public class TabbedDrawer : VisualElement
    {
        public TabbedDrawer() : base()
        {
            name = "TabbedDrawer";
            tabView = new TabView();
            this.Add(tabView);
            tabView.Q<VisualElement>("unity-tab-view__header-container").style.flexGrow = 1;
            tabs = new();
        }

        TabView tabView;
        List<Tab> tabs;

        public void Add(string displayName, SerializedProperty prop)
        {
            Tab newTab = new(displayName, prop);
            tabView.Add(newTab);
        }

        public class Tab : UnityEngine.UIElements.Tab
        {
            public Tab(string title, SerializedProperty property) : base(title)
            {
                displayName = title;
                this.property = property;
                tabHeader.style.paddingLeft = 5;
                tabHeader.style.paddingRight = 5;
                tabHeader.style.flexGrow = 1f;
                tabHeader.style.justifyContent = Justify.Center;

                //contentContainer.Add(new Label($"Content for {displayName}")); //(Debug thing.)

                bodyDrawer = new HeaderDrawer(property);
                contentContainer.Add(bodyDrawer);

                UpdateLiteralObject(property.managedReferenceValue?.GetType());
                bodyDrawer.OnTypeChanged += UpdateLiteralObject;
            }

            public string displayName { get; private set; }
            public SerializedProperty property { get; private set; }
            public HeaderDrawer bodyDrawer { get; private set; }


            private void UpdateLiteralObject(Type T) => tabHeader.style.color = T != null ? Color.white : Color.gray;
        }
    }

    public class ListDrawer : SuperList<ListDrawer, ListItemDrawer, Polymorph>
    {
        protected SerializedProperty rootProperty;
        protected FieldInfo fieldInfo;
        public Type baseType;

        public ListDrawer(SerializedProperty rootProperty, FieldInfo fieldInfo, bool BindImmediately = true) : base(rootProperty)
        {
            this.fieldInfo = fieldInfo;
            try
            {
                if (fieldInfo != null && fieldInfo.FieldType.IsGenericType)
                {
                    Type[] args = fieldInfo.FieldType.GetGenericArguments();
                    if (args != null && args.Length > 0) baseType = args[0];
                }
            }
            catch { baseType = null; }
            ShowTypeChooser = () => { ShowChooseTypeMenu(baseType, false, TypeChosen); };

            BuildBasicElements();
            if (BindImmediately) BindProperty(rootProperty);
        }

        new public void BindProperty(SerializedProperty input)
        {
            rootProperty = input;
            property = input.FindPropertyRelative("items");
            header.Bind(input);
            FinishBind();
        }
        public override bool allowCounterEdit => false;
        public override bool Expanded
        {
            get => rootProperty.isExpanded;
            set
            {
                rootProperty.isExpanded = value;
                header.UpdateExpanded(value);
            }
        }

        protected override void AddButtonPressed() => ShowTypeChooser();


        public Action ShowTypeChooser;

        public virtual void TypeChosen(Type chosen)
        {
            CreatePropertySlot(out int newID);
            SetOrCreateItemValue(newID, Activator.CreateInstance(chosen));
            CreateItemElement(newID);
            Select(items[newID]);
        }

    }
    public class ListItemDrawer : SuperListItem<ListDrawer, ListItemDrawer, Polymorph>
    {
        public ListItemDrawer(ListDrawer parentList, int Index) : base(parentList, Index) { }
        public override VisualElement Content()
        {
            HeaderDrawer result = new(property);
            result.ChangeButton.SetEnabled(false);
            result.ChangeButton.style.display = DisplayStyle.None;
            result.style.marginLeft = 14;
            result.style.marginRight = 3;
            return result;
        }
        protected override void PostContent()
        {
            Label = (content as HeaderDrawer).Label;
            ContextMenuTarget = (content as HeaderDrawer).FoldoutToggle;
        }
    }


    #endregion

    #region Property Drawers

    [CustomPropertyDrawer(typeof(Polymorph), true)]
    public class DirectDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
            => new HeaderDrawer(property);
    }

    [CustomPropertyDrawer(typeof(Single<>), true)]
    public class SingleDrawer : PropertyDrawer
    {
        SerializedProperty property;
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            this.property = property;
            return new HeaderDrawer(property.FindPropertyRelative("value"), OnSet);
        }

        void OnSet()
        {
        }
    }

    [CustomPropertyDrawer(typeof(ListOf<>), true)]
    public class ListOfDrawer : PropertyDrawer
    {

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            ListDrawer list = new(property, fieldInfo);
            return list;
        }


        public class Item : VisualElement
        {
            private readonly Action<Item, int> moveCallback;

            public Item(SerializedProperty itemProperty, Action<Item> RemoveCall, Action<Item, int> MoveCall)
            {
                this.itemProperty = itemProperty;
                moveCallback = MoveCall;

                name = "PolyListRow";
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.Center;
                style.marginTop = 2;

                glyph = new("≡")
                {
                    name = "listof-grab", style =
                    {
                        width = 18,
                        marginRight = 16,
                        marginLeft = 4,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                };
                Add(glyph);

                // Register wheel event on the glyph to trigger reorder.
                glyph.RegisterCallback<WheelEvent>((evt) =>
                {
                    // evt.delta.y > 0 => scroll up; move up one slot
                    // evt.delta.y < 0 => scroll down; move down one slot
                    float dy = evt.delta.y;
                    int delta = 0;
                    if (dy > 0f) delta = 1;
                    else if (dy < 0f) delta = -1;

                    if (delta != 0)
                    {
                        try
                        {
                            moveCallback?.Invoke(this, delta);
                        }
                        catch { /* defensive: swallow */ }
                        evt.StopPropagation();
                    }
                });

                body = new(itemProperty);
                body.style.flexGrow = 1;
                Add(body);
                body.ChangeButton.style.visibility = Visibility.Hidden;

                var removeBtn = new Button(() => RemoveCall(this))
                {
                    text = "-",
                    name = "listof-remove",
                    style =
                    {
                        width = 20,
                        marginLeft = 6,
                        backgroundColor = Color.clear,
                        borderBottomColor = Color.clear,
                        borderLeftColor = Color.clear,
                        borderRightColor = Color.clear,
                        borderTopColor = Color.clear
                    }
                };
                removeBtn.RegisterCallback<ClickEvent>((evt) => evt.StopPropagation());
                Add(removeBtn);
                removeBtn.RegisterHoverEvents(value => removeBtn.style.color = value ? new(1, .2f, .2f) : Color.white);

                // expose removebutton property for parity with existing class API
                removebutton = removeBtn;
            }

            public SerializedProperty itemProperty { get; private set; }
            public Label glyph { get; private set; }
            public Button removebutton { get; private set; }
            public HeaderDrawer body { get; private set; }
        }

    }

    [CustomPropertyDrawer(typeof(UniqueList<>), true)]
    public class UniqueListDrawer : ListOfDrawer
    {
        public UniqueListDrawer() : base() { }
        ListDrawer list;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            list = new(property, fieldInfo);
            list.ShowTypeChooser = ShowTypeChooser;
            return list;
        }

        void ShowTypeChooser()
        {
            GenericMenu menu = new();

            List<Type> types = GetSubtypes(list.baseType).ToList();

            for (int i = 0; i < list.CurrentSize; i++)
            {
                SerializedProperty elem = list.property.GetArrayElementAtIndex(i);
                if (elem != null && elem.managedReferenceValue != null) types.Remove(elem.managedReferenceValue.GetType());
            }

            if (types.Count != 0)
            {
                foreach (Type t in types)
                {
                    if (t == list.baseType) continue;
                    menu.AddItem(new GUIContent(t.Name), false, () => { list.TypeChosen(t); });
                }

                menu.ShowAsContext();
            }
        }

    }
    #endregion
}
