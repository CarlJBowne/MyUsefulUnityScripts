using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SLS.ListUtilities;
using SLS.EditorUtilities.Editor;

namespace SLS.ListUtilities.Editor
{
    [CustomPropertyDrawer(typeof(DictionaryS<,>), true)]
    [CustomPropertyDrawer(typeof(DictionarySReference<,>), true)]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement Display;
            Type DrawerType = typeof(ListDrawer<,>)
                .MakeGenericType(fieldInfo.FieldType.GenericTypeArguments);
            ILookupTable literal = fieldInfo.GetValue(property.serializedObject.targetObject) as ILookupTable;
            // Pass the live literal (the actual dictionary instance) to the drawer so it
            // can recalculate occurrences and provide proper binding. Using property.boxedValue
            // here returned a boxed/copy and left Literal null which caused blank/uneditable fields.
            Display = Activator.CreateInstance(DrawerType, property, literal, true) as VisualElement;
            return Display;
        }

        public class ListDrawer<TK, TV> : SuperList<ListDrawer<TK, TV>, ItemDrawer<TK, TV>, KeyValuePair<TK, TV>>
        {
            public ListDrawer(SerializedProperty rootProperty, ILookupTable literal, bool BindImmediately = true)
                : base(rootProperty, true)
            {
                LookupTable = literal;

                BuildBasicElements();
                NewItemInput = new(PostItemNaming);
                collectionBackground.Add(NewItemInput);

                if (BindImmediately) BindProperty(rootProperty);
            }
            new public void BindProperty(SerializedProperty input)
            {
                property = input;
                KeysProperty = property.FindPropertyRelative("serializedKeys");
                ValuesProperty = property.FindPropertyRelative("serializedValues");
                header.Bind(input);
                FinishBind();
            }

            public override int CurrentSize => ValuesProperty != null ? ValuesProperty.arraySize : 0;
            public override bool allowCounterEdit => false;

            public ILookupTable LookupTable { get; private set; }
            public SerializedProperty KeysProperty { get; private set; }
            public SerializedProperty ValuesProperty { get; private set; }

            public override void BuildItems()
            {
                base.BuildItems();
                CallUpdateColors();
            }

            protected override void AddButtonPressed() => NewItemInput.Show();
            InsertKeyPopup<TK> NewItemInput;
            public void PostItemNaming(TK value)
            {
                CreatePropertySlot(out int newID);

                SerializedProperty keyProp = KeysProperty.GetArrayElementAtIndex(newID);
                keyProp.SetGenericValue(value);

                SerializedProperty valProp = ValuesProperty.GetArrayElementAtIndex(newID);
                valProp.Reset();

                property.serializedObject.ApplyModifiedProperties();

                CreateItemElement(newID);
                Selection.Select(newID);
                NewItemInput.style.display = DisplayStyle.None;
                header.UpdateCounter(true);
                CallUpdateColors();
            }

            public override void CreatePropertySlot(out int newID)
            {
                if (KeysProperty == null || ValuesProperty == null) throw new ArgumentNullException();
                newID = Selection.NewItemID;
                KeysProperty.InsertArrayElementAtIndex(newID);
                ValuesProperty.InsertArrayElementAtIndex(newID);
            }

            public override void DeletePropertySlotAt(int index)
            {
                int prevKeysCount = KeysProperty.arraySize;
                int prevValuesCount = ValuesProperty.arraySize;

                KeysProperty.DeleteArrayElementAtIndex(index);
                ValuesProperty.DeleteArrayElementAtIndex(index);

                // If the array still has an element at this index and it's an object reference that is null,
                // delete it again to fully remove the slot.
                if (prevKeysCount < KeysProperty.arraySize)
                {
                    SerializedProperty maybeElem = KeysProperty.GetArrayElementAtIndex(index);
                    if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
                        KeysProperty.DeleteArrayElementAtIndex(index);
                }
                if (prevValuesCount < ValuesProperty.arraySize)
                {
                    SerializedProperty maybeElem = ValuesProperty.GetArrayElementAtIndex(index);
                    if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
                        ValuesProperty.DeleteArrayElementAtIndex(index);
                }

                header.UpdateCounter(false);
                header.UpdateExpanded(false);
                property.serializedObject.ApplyModifiedProperties();
            }

            protected override void EstablishContextMenu(ContextualMenuPopulateEvent evt)
            {
                base.EstablishContextMenu(evt);
                var list = evt.menu.MenuItems();
                list.Insert(1, new DropdownMenuAction("Remove Duplicates", RemoveDuplicatesContextMenu, DropDownMenuStatus));
            }
            protected override void ClearContextMenu(DropdownMenuAction C)
            {
                if (items != null)
                {
                    foreach (ItemDrawer<TK, TV> el in items) collectionBackground.Remove(el);
                    items.Clear();
                }

                KeysProperty.arraySize = 0;
                ValuesProperty.arraySize = 0;
                header.UpdateExpanded(false);

                property.serializedObject.ApplyModifiedProperties();
                BuildItems();
            }
            void RemoveDuplicatesContextMenu(DropdownMenuAction D)
            {
                LookupTable.RemoveDuplicates();
                property.serializedObject.Update();
                BuildItems();
                TryForceRefreshPrefabMarkers();
            }


            public void CallUpdateColors()
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (LookupTable == null) return;
                    List<bool> dupes = LookupTable.Duplicates();
                    if (i < dupes.Count) items[i].Invalid = dupes[i];
                }
            }


        }
        public class ItemDrawer<TK, TV> : SuperListItem<ListDrawer<TK, TV>, ItemDrawer<TK, TV>, KeyValuePair<TK, TV>>
        {
            public ItemDrawer(ListDrawer<TK, TV> parentList, int Index) : base(parentList, Index) { }

            protected override void BindProperty()
            {
                this.KeyProp = parent.KeysProperty.GetArrayElementAtIndex(Index);
                this.ValueProp = parent.ValuesProperty.GetArrayElementAtIndex(Index);
                FinishBind();
            }

            public SerializedProperty KeyProp { get; protected set; }
            public VisualElement KeyField { get; protected set; }
            public SerializedProperty ValueProp { get; protected set; }
            public PropertyField ValueField { get; protected set; }

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

                // Create the value field first so we can inspect whether it draws as a foldout
                ValueField?.Unbind();
                ValueField = new PropertyField(ValueProp, "").AddTo(content, v =>
                {
                    v.style.flexBasis = new Length(70, LengthUnit.Percent);
                    v.style.marginRight = 2;
                    v.style.flexGrow = 1f;
                });

                // Prepare KeyField but do not add it to the main content until we know layout
                if (KeyField != null) KeyField.Unbind();
                VisualElement createdKeyField;
                if (typeof(TK) == typeof(string)) createdKeyField = new TextField();
                else if (typeof(TK) == typeof(int)) createdKeyField = new IntegerField();
                else if (typeof(TK) == typeof(float)) createdKeyField = new FloatField();
                else if (typeof(TK) == typeof(double)) createdKeyField = new DoubleField();
                else createdKeyField = new PropertyField(KeyProp, "");

                // Configure KeyField common properties
                if (createdKeyField is TextField t)
                {
                    t.label = "";
                    t.style.maxHeight = EditorGUIUtility.singleLineHeight;
                    t.style.top = 0;
                    t.SetValueWithoutNotify(KeyProp.stringValue);
                    t.BindProperty(KeyProp);
                    t.isDelayed = true;
                }
                else if (createdKeyField is IntegerField i)
                {
                    i.label = "";
                    i.style.maxHeight = EditorGUIUtility.singleLineHeight;
                    i.style.top = 0;
                    i.SetValueWithoutNotify(KeyProp.intValue);
                    i.BindProperty(KeyProp);
                    i.isDelayed = true;
                }
                else if (createdKeyField is FloatField f)
                {
                    f.label = "";
                    f.style.maxHeight = EditorGUIUtility.singleLineHeight;
                    f.style.top = 0;
                    f.SetValueWithoutNotify(KeyProp.floatValue);
                    f.BindProperty(KeyProp);
                    f.isDelayed = true;
                }
                else if (createdKeyField is DoubleField d)
                {
                    d.label = "";
                    d.style.maxHeight = EditorGUIUtility.singleLineHeight;
                    d.style.top = 0;
                    d.SetValueWithoutNotify(KeyProp.doubleValue);
                    d.BindProperty(KeyProp);
                    d.isDelayed = true;
                }
                else if (createdKeyField is PropertyField pf)
                {
                    pf.RegisterCallback<ContextualMenuPopulateEvent>(ContextMenu, TrickleDown.TrickleDown);
                }

                createdKeyField.style.flexBasis = new Length(30, LengthUnit.Percent);
                KeyField = createdKeyField;

                // If the value field contains a Foldout, place the key field into the foldout header next to the label
                VisualElement top = ValueField?.Q<PropertyField>() as VisualElement
                    ?? ValueField?.Q<Foldout>() as VisualElement ?? null;
                if (top != null)
                {
                    top.DelayedBuild(() =>
                    {
                        // Make foldout take the full width of the item
                        top.style.flexBasis = new Length(100, LengthUnit.Percent);
                        top.style.flexGrow = 1f;
                        top.style.marginLeft = 8;

                        // Try to find the toggle/label container and insert the key field there
                        var toggle = top.Q<Toggle>(null, Foldout.toggleUssClassName);
                        var label = toggle?.Q<Label>(null, "unity-label");
                        var insertParent = label?.parent ?? (VisualElement)toggle ?? top;

                        // Add key field to the header area
                        insertParent.Add(KeyField);

                        // Adjust layout so label stays left and key field to the right
                        if (label != null)
                        {
                            label.text = label.text.Replace("Element ", "");
                            label.style.flexGrow = 0;
                            label.ShrinkToTextWidth();
                        }
                        KeyField.style.flexGrow = 1f;
                        KeyField.style.alignSelf = Align.FlexEnd;
                    });
                }
                else
                {
                    // Default layout: key on left, value on right
                    KeyField.style.flexBasis = new Length(30, LengthUnit.Percent);
                    content.Add(KeyField);
                    // ValueField already added
                }

                return content;
            }

            protected override void PostContent()
            {
                if (KeyField is TextField T)
                    T.RegisterValueChangedCallback(ev => parent.CallUpdateColors());
                else if (KeyField is PropertyField P)
                    P.RegisterValueChangeCallback(ev => parent.CallUpdateColors());
                else if (KeyField is IntegerField I)
                    I.RegisterValueChangedCallback(ev => parent.CallUpdateColors());
                else if (KeyField is FloatField F)
                    F.RegisterValueChangedCallback(ev => parent.CallUpdateColors());
                else if (KeyField is DoubleField D)
                    D.RegisterValueChangedCallback(ev => parent.CallUpdateColors());

                ValueField.BindProperty(ValueProp);

                ContextMenuTarget = KeyField;
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
    [CustomPropertyDrawer(typeof(HashedListS<>), true)]
    [CustomPropertyDrawer(typeof(HashedListSReference<>), true)]
    public class HashedListDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement Display;
            Type DrawerType = typeof(ListDrawer<>)
                .MakeGenericType(fieldInfo.FieldType.GenericTypeArguments);
            ILookupTable literal = fieldInfo.GetValue(property.serializedObject.targetObject) as ILookupTable;
            // Pass the live literal (the actual dictionary instance) to the drawer so it
            // can recalculate occurrences and provide proper binding. Using property.boxedValue
            // here returned a boxed/copy and left Literal null which caused blank/uneditable fields.
            Display = Activator.CreateInstance(DrawerType, property, literal, true) as VisualElement;
            return Display;
        }

        public class ListDrawer<T> : SuperList<ListDrawer<T>, ItemDrawer<T>, T>
        {
            public ListDrawer(SerializedProperty rootProperty, ILookupTable literal, bool BindImmediately = true)
                : base(rootProperty, true)
            {
                LookupTable = literal;

                BuildBasicElements();
                NewItemInput = new(PostItemNaming);
                collectionBackground.Add(NewItemInput);

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

            public override int CurrentSize => ValuesProperty != null ? ValuesProperty.arraySize
                    : KeysProperty != null ? KeysProperty.arraySize
                    : NamesProperty != null ? NamesProperty.arraySize
                    : 0;

            public override bool allowCounterEdit => false;

            public ILookupTable LookupTable { get; private set; }
            public SerializedProperty NamesProperty { get; private set; }
            public SerializedProperty KeysProperty { get; private set; }
            public SerializedProperty ValuesProperty { get; private set; }

            public override void BuildItems()
            {
                base.BuildItems();
                CallUpdateColors();
            }

            #region Add Systems
            protected override void AddButtonPressed() => NewItemInput.Show();
            private InsertKeyPopup<string> NewItemInput;
            void PostItemNaming(string value)
            {
                if (string.IsNullOrEmpty(value)) return;
                CreatePropertySlot(out int newID);

                NamesProperty.GetArrayElementAtIndex(newID).stringValue = value;
                KeysProperty.GetArrayElementAtIndex(newID).intValue = value.Hash();
                SerializedProperty valProp = ValuesProperty.GetArrayElementAtIndex(newID);
                valProp.Reset();

                property.serializedObject.ApplyModifiedProperties();

                CreateItemElement(newID);
                Selection.Select(newID);
                NewItemInput.style.display = DisplayStyle.None;
                header.UpdateCounter(true);
            }
            public override void CreatePropertySlot(out int newID)
            {
                if (KeysProperty == null || ValuesProperty == null) throw new ArgumentNullException();
                newID = Selection.NewItemID;
                NamesProperty.InsertArrayElementAtIndex(newID);
                KeysProperty.InsertArrayElementAtIndex(newID);
                ValuesProperty.InsertArrayElementAtIndex(newID);
            }
            #endregion
            public override void DeletePropertySlotAt(int index)
            {
                int prevNamesCount = NamesProperty.arraySize;
                int prevKeysCount = KeysProperty.arraySize;
                int prevValuesCount = ValuesProperty.arraySize;

                NamesProperty.DeleteArrayElementAtIndex(index);
                KeysProperty.DeleteArrayElementAtIndex(index);
                ValuesProperty.DeleteArrayElementAtIndex(index);

                // If the array still has an element at this index and it's an object reference that is null,
                // delete it again to fully remove the slot.
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
                header.UpdateCounter(false);
                property.serializedObject.ApplyModifiedProperties();
            }

            protected override void EstablishContextMenu(ContextualMenuPopulateEvent evt)
            {
                base.EstablishContextMenu(evt);
                var list = evt.menu.MenuItems();
                list.Insert(1, new DropdownMenuAction("Remove Duplicates", RemoveDuplicatesContextMenu, DropDownMenuStatus));
            }
            protected override void ClearContextMenu(DropdownMenuAction C)
            {
                if (items != null)
                {
                    foreach (ItemDrawer<T> el in items) collectionBackground.Remove(el);
                    items.Clear();
                }
                if (NamesProperty is null || KeysProperty is null || ValuesProperty is null) return;

                NamesProperty.arraySize = 0;
                KeysProperty.arraySize = 0;
                ValuesProperty.arraySize = 0;
                header.UpdateExpanded(false);

                property.serializedObject.ApplyModifiedProperties();
                BuildItems();
            }
            void RemoveDuplicatesContextMenu(DropdownMenuAction D)
            {
                LookupTable.RemoveDuplicates();
                property.serializedObject.Update();
                BuildItems();
                TryForceRefreshPrefabMarkers();
            }


            public void CallUpdateColors()
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (LookupTable == null) return;
                    List<bool> dupes = LookupTable.Duplicates();
                    if (i < dupes.Count) items[i].Invalid = dupes[i];
                }
            }


        }
        public class ItemDrawer<T> : SuperListItem<ListDrawer<T>, ItemDrawer<T>, T>
        {
            public ItemDrawer(ListDrawer<T> parentList, int Index) : base(parentList, Index) { }

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
            public PropertyField ValueField { get; protected set; }

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

                // Create the value field first so we can inspect whether it draws as a foldout
                ValueField?.Unbind();
                ValueField = new PropertyField(ValueProp, "").AddTo(content, v =>
                {
                    v.style.flexBasis = new Length(70, LengthUnit.Percent);
                    v.style.marginRight = 2;
                    v.style.flexGrow = 1f;

                    // Prepare KeyField but do not add it to the main content until we know layout
                    NameField?.Unbind();
                    NameField = new TextField("")
                    {
                        label = "",
                        style =
                    {
                        maxHeight = EditorGUIUtility.singleLineHeight,
                        top = 0,
                        flexBasis = new Length(30, LengthUnit.Percent),
                    },
                        isDelayed = true
                    };
                    NameField.SetValueWithoutNotify(NameProp.stringValue);
                    NameField.BindProperty(NameProp);


                    // If the value field contains a Foldout, place the key field into the foldout header next to the label
                    VisualElement top = v?.Q<PropertyField>() as VisualElement
                        ?? v?.Q<Foldout>() as VisualElement ?? null;
                    if (top != null)
                    {
                        top.DelayedBuild(() =>
                        {
                            // Make foldout take the full width of the item
                            top.style.flexBasis = new Length(100, LengthUnit.Percent);
                            top.style.flexGrow = 1f;
                            top.style.marginLeft = 8;

                            // Try to find the toggle/label container and insert the key field there
                            var toggle = top.Q<Toggle>(null, Foldout.toggleUssClassName);
                            var label = toggle?.Q<Label>(null, "unity-label");
                            var insertParent = label?.parent ?? (VisualElement)toggle ?? top;

                            // Add key field to the header area
                            insertParent.Add(NameField);

                            // Adjust layout so label stays left and key field to the right
                            if (label != null)
                            {
                                label.text = label.text.Replace("Element ", "");
                                label.style.flexGrow = 0;
                                label.ShrinkToTextWidth();
                            }
                            NameField.style.flexGrow = 1f;
                            NameField.style.alignSelf = Align.FlexEnd;
                        });
                    }
                    else
                    {
                        // Default layout: key on left, value on right
                        NameField.style.flexBasis = new Length(30, LengthUnit.Percent);
                        content.Add(NameField);
                        // ValueField already added
                    }

                });

                return content;
            }

            protected override void PostContent()
            {
                NameField.tooltip = $"Key: {KeyProp.intValue}";
                NameField.RegisterValueChangedCallback(ev =>
                {
                    KeyProp.intValue = ev.newValue.Hash();
                    NameField.tooltip = $"Key: {KeyProp.intValue}";
                    parent.CallUpdateColors();
                });

                ValueField.BindProperty(ValueProp);

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

    public class InsertKeyPopup<T> : EnterDataMenu
    {
        public InsertKeyPopup(Action<T> postAction) : base(null, true)
        {
            this.postAction = postAction;
            style.flexDirection = FlexDirection.Row;
            this.Display(false);

            Func<dynamic> get = null;

            // Build the appropriate field for type T and wire change callbacks to invoke the post action
            Field = typeof(T) == typeof(string) ? new TextField().AddTo(this, f =>
            {
                TextField = f;
                f.label = "Insert Key:";
                f.isDelayed = true;
                f.style.flexGrow = 1f;
                PrepAction = () => f.SetValueWithoutNotify(""); //Set to default on Show
                get = () => f.text;

            }) : typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)) ? new ObjectField().AddTo(this, f =>
            {
                f.label = "Insert Key:";
                f.style.flexGrow = 1f;
                PrepAction = () => f.SetValueWithoutNotify(null); //Set to default on Show
                get = () => f.value;

            }) : typeof(T) == typeof(int) ? new IntegerField().AddTo(this, f =>
            {
                f.label = "Insert Key:";
                f.isDelayed = true;
                f.style.flexGrow = 1f;
                PrepAction = () => f.SetValueWithoutNotify(0);
                get = () => f.value;

            }) : typeof(T) == typeof(double) ? new FloatField().AddTo(this, f =>
            {
                f.label = "Insert Key:";
                f.isDelayed = true;
                f.style.flexGrow = 1f;
                PrepAction = () => f.SetValueWithoutNotify(1f);
                get = () => f.value;

            }) : typeof(T) == typeof(Color) ? new ColorField().AddTo(this, f =>
            {
                f.label = "Insert Key:";
                f.style.flexGrow = 1f;
                PrepAction = () => f.SetValueWithoutNotify(Color.white);
                get = () => f.value;

            }) : typeof(T).IsEnum ? new EnumField(default(T) as System.Enum).AddTo(this, f =>
            {
                f.label = "Insert Key:";
                f.style.flexGrow = 1f;
                PrepAction = () => f.SetValueWithoutNotify(default(T) as System.Enum);
                get = () => f.value;

            }) : new PopupField<T>().AddTo(this, f =>
            {
                f.label = "Insert Key:";
                f.style.flexGrow = 1f;
                T def = f.value;
                PrepAction = () => f.SetValueWithoutNotify(def);
                get = () => f.value;
            });

            // Provide a finish button similar to EnterDataMenu
            FinishButton = new Button(Invoke).AddTo(this);
            FinishButton.style.width = 20;
            FinishButton.text = "+";
            FinishButton.style.backgroundColor = new Color(.5f, .75f, .5f);

            // helper to invoke the generic post action with some resilience to type mismatches
            void Invoke()
            {
                if (this.postAction == null) return;
                try
                {
                    this.postAction(get.Invoke());
                }
                catch
                {
                    try
                    {
                        var converted = Convert.ChangeType(get.Invoke(), typeof(T));
                        this.postAction((T)converted);
                    }
                    catch { }
                }
                this.Display(false);
                Blur();
            }
        }

        VisualElement Field;
        Action<T> postAction;
        Action PrepAction;

        public override void Show()
        {
            PrepAction?.Invoke();
            if (!parent.IsDisplay()) parent.Display(true);
            this.Display(!this.IsDisplay());
            Field?.Focus();
        }
    }

}
