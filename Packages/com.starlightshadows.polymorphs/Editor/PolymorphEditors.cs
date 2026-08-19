using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SLS.EditorUtilities.Editor;
using SLS.ListUtilities;
using SLS.ListUtilities.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Polymorph;

public class PolymorphEditors
{

    #region Utilities

    public static void ShowChooseTypeMenu(Type baseType, bool showNullOption, Action<Type> result)
    {
        GenericMenu menu = new();

        
        Type[] types = GetSubtypes(baseType);
        if (types.Length != 0)
        {
            DoType(baseType);
            foreach (Type t in types)
            {
                if (t == baseType) continue;
                Add(t);
            }
        }
        else menu.AddItem(new GUIContent("Add"), false, () => { result?.Invoke(baseType); });
        void DoType(Type t)
        {
            if (t.IsAbstract) return;
            if (!t.ContainsGenericParameters) Add(t);
            else
            {
                PropertyInfo ValidTypesProperty = t.GetProperty("ValidTypes", BindingFlags.NonPublic | BindingFlags.Static);
                if (ValidTypesProperty == null) return;
                Type[] validTypes = ValidTypesProperty.GetValue(t, null) as Type[];
                for (int i = 0; i < validTypes.Length; i++)
                {
                    Type subtype = t;
                    subtype.GenericTypeArguments[0] = validTypes[i];
                    Add(subtype);
                }
            }
        }
        void Add(Type t) => menu.AddItem(new GUIContent(t.Name), false, () => { result?.Invoke(t); });

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

    public class ListDrawer : SuperList<ListDrawer, ListDrawer.ItemDrawer, Polymorph>
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
            Selection.Select(newID);
        }

        public class ItemDrawer : SuperListItem<ListDrawer, ItemDrawer, Polymorph>
        {
            public ItemDrawer(ListDrawer parentList, int Index) : base(parentList, Index) { }
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

    }
    public class DictionaryDrawer : SuperList<DictionaryDrawer, DictionaryDrawer.ItemDrawer, NameHashValueTrio<Polymorph>>
    {
        private readonly FieldInfo fieldInfo;
        private readonly Type targetBaseType;
        public ILookupTable LookupTable { get; private set; }
        public SerializedProperty NamesProperty { get; private set; }
        public SerializedProperty KeysProperty { get; private set; }
        public SerializedProperty ValuesProperty { get; private set; }

        public DictionaryDrawer(SerializedProperty rootProperty, FieldInfo fieldInfo, bool BindImmediately = true)
            : base(rootProperty, true)
        {
            this.fieldInfo = fieldInfo;
            // Try to obtain the live dictionary instance to support duplicate detection if it implements ILookupTable
            try
            {
                LookupTable = fieldInfo?.GetValue(rootProperty.serializedObject.targetObject) as ILookupTable;
            }
            catch { LookupTable = null; }

            targetBaseType = fieldInfo.FieldType.GenericTypeArguments[0];

            BuildBasicElements();
            enterDataMenu = new EnterPolyDataMenu(TypeChosen, targetBaseType).AddTo(collectionBackground);

            if (BindImmediately) BindProperty(rootProperty);
        }

        new public void BindProperty(SerializedProperty input)
        {
            property = input;
            NamesProperty = property.FindPropertyRelative("serializedNames");
            KeysProperty = property.FindPropertyRelative("serializedKeys");
            ValuesProperty = property.FindPropertyRelative("serializedValues");
            header.Bind(input);
            FinishBind();
        }

        public override int CurrentSize => ValuesProperty != null ? ValuesProperty.arraySize : 0;

        public override bool allowCounterEdit => false;


        EnterPolyDataMenu enterDataMenu;
        public class EnterPolyDataMenu : EnterDataMenu
        {
            public EnterPolyDataMenu(Action<string, Type> result, Type baseType, bool Override = false) : base(null, true)
            {
                if (Override) return;
                style.flexDirection = FlexDirection.Row;
                this.Display(false);

                TextField = new TextField("").AddTo(this);
                TextField.style.flexGrow = 1f;

                Types = Polymorph.GetSubtypes(baseType);
                string[] typeNames = Types.Select(t => t.Name).ToArray();
                TypeField = new DynamicEnumField(typeNames, -1, null).AddTo(this);
                TypeField.style.width = Length.Percent(40);
                TypeField.style.flexShrink = 0;

                Result = result;
                FinishButton = new Button(Complete).AddTo(this);
                FinishButton.style.width = 20;
                FinishButton.text = "+";
                FinishButton.style.backgroundColor = new Color(.5f, .75f, .5f);
            }

            public DynamicEnumField TypeField { get; protected set; }
            public Type[] Types { get; protected set; }
            new public Action<string, Type> Result { get; protected set; }

            protected override void Complete()
            {
                Result?.Invoke(TextField.text, Types[TypeField.SelectedIndex]);
                TextField.SetValueWithoutNotify("");
                TypeField.SelectedIndex = -1;
                this.Display(false);
            }
        }

        protected override void AddButtonPressed()
        {
            // Show type chooser so caller can pick the concrete Polymorph type to create
            enterDataMenu.Show();
            collectionBackground.style.display = DisplayStyle.Flex;
        }
        public override void CreatePropertySlot(out int newID)
        {
            if (ValuesProperty == null || KeysProperty == null || NamesProperty == null) throw new ArgumentNullException();
            newID = Selection.NewItemID;
            ValuesProperty.InsertArrayElementAtIndex(newID);
            NamesProperty.InsertArrayElementAtIndex(newID);
            KeysProperty.InsertArrayElementAtIndex(newID);
        }

        public virtual void TypeChosen(string newName, Type chosen)
        {
            if (chosen == null) return;
            CreatePropertySlot(out int newID);

            // ensure a stable name and corresponding hash key for the new slot
            SerializedProperty nameProp = NamesProperty.GetArrayElementAtIndex(newID);
            nameProp.stringValue = newName;

            SerializedProperty keyProp = KeysProperty.GetArrayElementAtIndex(newID);
            keyProp.intValue = newName.Hash();

            SerializedProperty valProp = ValuesProperty.GetArrayElementAtIndex(newID);
            try { valProp.managedReferenceValue = Activator.CreateInstance(chosen); } catch { valProp.managedReferenceValue = null; }

            property.serializedObject.ApplyModifiedProperties();

            CreateItemElement(newID);
            Selection.Select(newID);
        }



        public override void BuildItems()
        {
            base.BuildItems();
            CallUpdateColors();
        }

        public override void DeletePropertySlotAt(int index)
        {
            int prevNamesCount = NamesProperty.arraySize;
            int prevKeysCount = KeysProperty.arraySize;
            int prevValuesCount = ValuesProperty.arraySize;

            NamesProperty.DeleteArrayElementAtIndex(index);
            KeysProperty.DeleteArrayElementAtIndex(index);
            ValuesProperty.DeleteArrayElementAtIndex(index);

            // Handle the Unity quirk where deleting an object reference leaves a null element
            if (prevNamesCount == NamesProperty.arraySize)
            {
                SerializedProperty maybeElem = NamesProperty.GetArrayElementAtIndex(index);
                if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
                    NamesProperty.DeleteArrayElementAtIndex(index);
            }
            if (prevKeysCount == KeysProperty.arraySize)
            {
                SerializedProperty maybeElem = KeysProperty.GetArrayElementAtIndex(index);
                if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
                    KeysProperty.DeleteArrayElementAtIndex(index);
            }
            if (prevValuesCount == ValuesProperty.arraySize)
            {
                SerializedProperty maybeElem = ValuesProperty.GetArrayElementAtIndex(index);
                if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
                    ValuesProperty.DeleteArrayElementAtIndex(index);
            }

            header.UpdateExpanded(false);
            property.serializedObject.ApplyModifiedProperties();
        }

        protected override void EstablishContextMenu(ContextualMenuPopulateEvent evt)
        {
            base.EstablishContextMenu(evt);
            if (LookupTable != null)
            {
                var list = evt.menu.MenuItems();
                list.Insert(1, new DropdownMenuAction("Remove Duplicates", RemoveDuplicatesContextMenu, DropDownMenuStatus));
            }
        }

        void RemoveDuplicatesContextMenu(DropdownMenuAction D)
        {
            LookupTable?.RemoveDuplicates();
            property.serializedObject.Update();
            BuildItems();
            TryForceRefreshPrefabMarkers();
        }

        public void CallUpdateColors()
        {
            if (LookupTable == null || items == null) return;
            List<bool> dupes = LookupTable.Duplicates();
            for (int i = 0; i < items.Count; i++)
            {
                if (i < dupes.Count) items[i].Invalid = dupes[i];
            }
        }

        public class ItemDrawer : SuperListItem<DictionaryDrawer, ItemDrawer, NameHashValueTrio<Polymorph>>
        {
            public ItemDrawer(DictionaryDrawer parentList, int Index) : base(parentList, Index) { }

            protected override void BindProperty()
            {
                this.NameProp = parent.NamesProperty.GetArrayElementAtIndex(Index);
                this.KeyProp = parent.KeysProperty.GetArrayElementAtIndex(Index);
                this.ValueProp = parent.ValuesProperty.GetArrayElementAtIndex(Index);
                FinishBind();
            }

            public SerializedProperty NameProp { get; protected set; }
            public TextField NameField { get; protected set; }
            public SerializedProperty KeyProp { get; protected set; }
            public SerializedProperty ValueProp { get; protected set; }
            public HeaderDrawer ValueHeader { get; protected set; }

            public override VisualElement Content()
            {
                UpdateBackground();

                content = new VisualElement()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        flexGrow = 1f
                    }
                };

                ValueHeader = new HeaderDrawer(ValueProp)
                {
                    style =
                    {
                        flexBasis = new Length(70, LengthUnit.Percent),
                        marginRight = 2,
                        marginLeft = 12,
                        flexGrow = 1f
                    }
                }.AddTo(content).DelayedBuild(() =>
                {
                    ValueHeader.ChangeButton.parent.Remove(ValueHeader.ChangeButton);

                    // Name field (visible)
                    NameField?.Unbind();
                    NameField = new TextField().AddTo(ValueHeader.Label.parent, k =>
                    {
                        ValueHeader.Label.text = ValueHeader.Label.text.Split(' ')[2];
                        k.style.flexBasis = Length.Percent(50);
                        k.label = "";
                        k.style.height = EditorGUIUtility.singleLineHeight - 2;
                        k.style.flexGrow = 1;
                        k.SendToBack();
                        ValueHeader.Arrow.SendToBack();
                        ValueHeader.Arrow.style.marginRight = 0;
                        ValueHeader.Label.style.marginLeft = 4;
                        ValueHeader.Label.style.flexGrow = 0;
                        ValueHeader.Label.ShrinkToTextWidth();


                        k.SetValueWithoutNotify(NameProp.stringValue);

                        k.SetValueWithoutNotify(NameProp.stringValue);
                        k.BindProperty(NameProp);
                        k.isDelayed = true;
                        // When name changes, update the hash key and propagate duplicate checks
                        k.DelayedBuild(() => k.RegisterValueChangedCallback(ev =>
                        {
                            NameProp.stringValue = ev.newValue;
                            KeyProp.intValue = ev.newValue.Hash();
                            NameProp.serializedObject.ApplyModifiedProperties();
                            parent.CallUpdateColors();
                        }));


                    });
                });

                return content;
            }

            protected override void PostContent()
            {
                // Context menu target and value binding handled by HeaderDrawer
                ContextMenuTarget = NameField;
            }

            protected override void ContextMenu(ContextualMenuPopulateEvent evt)
            {
                var list = evt.menu.MenuItems();
                bool deleteFound = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is not DropdownMenuAction iAction) continue;

                    if (iAction.name.StartsWith("Apply to Prefab")) list[i] = new DropdownMenuAction(iAction.name, T => ApplyOrRevertContextMenu(iAction), DropDownMenuStatus);
                    if (iAction.name.StartsWith("Revert")) list[i] = new DropdownMenuAction(iAction.name, T => ApplyOrRevertContextMenu(iAction), DropDownMenuStatus);

                    if (iAction.name == "Duplicate Array Element")
                    {
                        list.RemoveAt(i);
                        i--;
                    }
                    if (iAction.name == "Delete Array Element")
                    {
                        list[i] = new DropdownMenuAction("Delete", DeleteContextMenu, DropDownMenuStatus);
                        deleteFound = true;
                    }
                }
                if (!deleteFound)
                    list.Add(new DropdownMenuAction("Delete", DeleteContextMenu, DropDownMenuStatus));
            }
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
    }

    [CustomPropertyDrawer(typeof(UniqueList<>), true)]
    public class UniqueListDrawer : ListOfDrawer
    {
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

    [CustomPropertyDrawer(typeof(Dictionary<>), true)]
    public class DictionaryOfDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Make a concrete generic drawer type for the dictionary value type
            var display = new DictionaryDrawer(property, fieldInfo, true) as VisualElement;
            return display;
        }

    }
    #endregion
}
