using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SLS.ListUtilities;
using SLS.ListUtilities.Editor.Internal;

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
                if (BindImmediately) BindProperty(rootProperty);
                //NewItemInput = new(PostItemNaming);
                //this.Add(NewItemInput);
            }
            new public void BindProperty(SerializedProperty input)
            {
                property = input;
                KeysProperty = property.FindPropertyRelative("serializedKeys");
                ValuesProperty = property.FindPropertyRelative("serializedValues");
                header.Bind(input);
                FinishBind();
            }

            public override int CurrentSize
            {
                get => ValuesProperty != null ? ValuesProperty.arraySize : 0;
                set
                {
                    bool isBigger = value > ValuesProperty.arraySize;
                    KeysProperty.arraySize = value;
                    ValuesProperty.arraySize = value;
                    header.UpdateExpanded(isBigger);
                }
            }
            public override bool allowCounterEdit => false;

            public ILookupTable LookupTable { get; private set; }
            public SerializedProperty KeysProperty { get; private set; }
            public SerializedProperty ValuesProperty { get; private set; }

            public override void BuildItems()
            {
                base.BuildItems();
                CallUpdateColors();
            }

            protected override void AddButtonPressed()
            {
                CreatePropertySlot(out int newID);
                //SetOrCreateItemValue(newID);
                CreateItemElement(newID);
                Select(items[newID]);
            }
            /*
            #region Add Systems
            protected override void AddButtonPressed() => NewItemInput.Show();
            private InsertKeyPopup<TK> NewItemInput;
            void PostItemNaming(TK value)
            {
                CreatePropertySlot(out int newID);

                KeysProperty.GetArrayElementAtIndex(newID).intValue = value;
                SerializedProperty valProp = ValuesProperty.GetArrayElementAtIndex(newID);
                switch (valProp.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        valProp.intValue = 0;
                        break;
                    case SerializedPropertyType.Boolean:
                        valProp.boolValue = false;
                        break;
                    case SerializedPropertyType.Float:
                        valProp.floatValue = 0f;
                        break;
                    case SerializedPropertyType.String:
                        valProp.stringValue = string.Empty;
                        break;
                    case SerializedPropertyType.Enum:
                        valProp.intValue = 0;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        valProp.objectReferenceValue = null;
                        break;
                    case SerializedPropertyType.ManagedReference:
                        try { valProp.managedReferenceValue = Activator.CreateInstance(typeof(TV)); }
                        catch { valProp.managedReferenceValue = null; }
                        break;
                    default:
                        // Try managed reference as fallback
                        try { valProp.managedReferenceValue = Activator.CreateInstance(typeof(TV)); } catch { }
                        break;
                }

                property.serializedObject.ApplyModifiedProperties();

                CreateItemElement(newID);
                Select(items[newID]);
                NewItemInput.style.display = DisplayStyle.None;
            }
            #endregion
            */
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
                CurrentSize = 0;
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
                KeyProp = parent.KeysProperty.GetArrayElementAtIndex(Index);
                ValueProp = parent.ValuesProperty.GetArrayElementAtIndex(Index);
                FinishBind();
            }

            public SerializedProperty NameProp
            { get; protected set; }
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

                if (KeyField != null) KeyField.Unbind();
                KeyField =
                    typeof(TK) == typeof(string) ? new TextField().AddTo(content, k =>
                    {
                        k.label = "";
                        k.style.maxHeight = EditorGUIUtility.singleLineHeight;
                        k.style.top = 0;
                        k.SetValueWithoutNotify(KeyProp.stringValue);
                        k.BindProperty(KeyProp);
                        k.isDelayed = true;
                    })
                    : typeof(TK) == typeof(int) ? new IntegerField().AddTo(content, k =>
                    {
                        k.label = "";
                        k.style.maxHeight = EditorGUIUtility.singleLineHeight;
                        k.style.top = 0;
                        k.SetValueWithoutNotify(KeyProp.intValue);
                        k.BindProperty(KeyProp);
                        k.isDelayed = true;
                    })
                    : typeof(TK) == typeof(float) ? new FloatField().AddTo(content, k =>
                    {
                        k.label = "";
                        k.style.maxHeight = EditorGUIUtility.singleLineHeight;
                        k.style.top = 0;
                        k.SetValueWithoutNotify(KeyProp.floatValue);
                        k.BindProperty(KeyProp);
                        k.isDelayed = true;
                    })
                    : typeof(TK) == typeof(double) ? new DoubleField().AddTo(content, k =>
                    {
                        k.label = "";
                        k.style.maxHeight = EditorGUIUtility.singleLineHeight;
                        k.style.top = 0;
                        k.SetValueWithoutNotify(KeyProp.doubleValue);
                        k.BindProperty(KeyProp);
                        k.isDelayed = true;
                    })
                    : new PropertyField(KeyProp, "").AddTo(content, k =>
                    {
                        k.RegisterCallback<ContextualMenuPopulateEvent>(ContextMenu, TrickleDown.TrickleDown);
                    });

                KeyField.style.flexBasis = new Length(30, LengthUnit.Percent);


                ValueField?.Unbind();
                ValueField = new PropertyField(ValueProp, "").AddTo(content, v =>
                {
                    v.style.flexBasis = new Length(70, LengthUnit.Percent);
                    v.style.marginRight = 2;
                    v.style.flexGrow = 1f;
                });
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
                if (BindImmediately) BindProperty(rootProperty);
                NewItemInput = new(PostItemNaming);
                Add(NewItemInput);
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

            public override int CurrentSize
            {
                get => ValuesProperty.arraySize;
                set
                {
                    bool isBigger = value > NamesProperty.arraySize;
                    NamesProperty.arraySize = value;
                    KeysProperty.arraySize = value;
                    ValuesProperty.arraySize = value;
                    header.UpdateExpanded(isBigger);
                }
            }
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
            protected override void AddButtonPressed()
            {
                NewItemInput.Show();
            }
            private InsertKeyPopup<string> NewItemInput;
            void PostItemNaming(string value)
            {
                CreatePropertySlot(out int newID);

                NamesProperty.GetArrayElementAtIndex(newID).stringValue = value;
                KeysProperty.GetArrayElementAtIndex(newID).intValue = value.GetHashCode();
                SerializedProperty valProp = ValuesProperty.GetArrayElementAtIndex(newID);
                switch (valProp.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        valProp.intValue = 0;
                        break;
                    case SerializedPropertyType.Boolean:
                        valProp.boolValue = false;
                        break;
                    case SerializedPropertyType.Float:
                        valProp.floatValue = 0f;
                        break;
                    case SerializedPropertyType.String:
                        valProp.stringValue = string.Empty;
                        break;
                    case SerializedPropertyType.Enum:
                        valProp.intValue = 0;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        valProp.objectReferenceValue = null;
                        break;
                    case SerializedPropertyType.ManagedReference:
                        try { valProp.managedReferenceValue = Activator.CreateInstance(typeof(T)); }
                        catch { valProp.managedReferenceValue = null; }
                        break;
                    default:
                        // Try managed reference as fallback
                        try { valProp.managedReferenceValue = Activator.CreateInstance(typeof(T)); } catch { }
                        break;
                }

                property.serializedObject.ApplyModifiedProperties();

                CreateItemElement(newID);
                Select(items[newID]);
                NewItemInput.style.display = DisplayStyle.None;
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
                if (prevNamesCount < NamesProperty.arraySize)
                {
                    SerializedProperty maybeElem = NamesProperty.GetArrayElementAtIndex(index);
                    if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
                        NamesProperty.DeleteArrayElementAtIndex(index);
                }
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
                    foreach (ItemDrawer<T> el in items) collectionBackground.Remove(el);
                    items.Clear();
                }
                CurrentSize = 0;
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
                NameProp = parent.NamesProperty.GetArrayElementAtIndex(Index);
                KeyProp = parent.KeysProperty.GetArrayElementAtIndex(Index);
                ValueProp = parent.ValuesProperty.GetArrayElementAtIndex(Index);
                FinishBind();
            }

            public SerializedProperty NameProp
            { get; protected set; }
            public TextField NameField { get; protected set; }
            public SerializedProperty KeyProp { get; protected set; }
            public IntegerField KeyField { get; protected set; }
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

                NameField?.Unbind();
                NameField = new TextField().AddTo(content, k =>
                {
                    k.style.flexBasis = new Length(30, LengthUnit.Percent);
                    k.label = "";
                    k.style.maxHeight = EditorGUIUtility.singleLineHeight;
                    k.style.top = 0;
                    k.SetValueWithoutNotify(NameProp.stringValue);
                    k.BindProperty(NameProp);
                    k.isDelayed = true;
                });
                KeyField?.Unbind();
                KeyField = new IntegerField().AddTo(content, k =>
                {
                    k.style.display = DisplayStyle.None;
                    k.label = "";
                    k.style.maxHeight = EditorGUIUtility.singleLineHeight;
                    k.style.top = 0;
                    k.SetValueWithoutNotify(KeyProp.intValue);
                    k.BindProperty(KeyProp);
                    k.isDelayed = true;
                });
                ValueField?.Unbind();
                ValueField = new PropertyField(ValueProp, "").AddTo(content, v =>
                {
                    v.style.flexBasis = new Length(70, LengthUnit.Percent);
                    v.style.marginRight = 2;
                    v.style.flexGrow = 1f;
                });
                return content;
            }

            protected override void PostContent()
            {
                NameField.SetValueWithoutNotify(NameProp.stringValue);
                KeyField.SetValueWithoutNotify(KeyProp.intValue);

                ValueField.BindProperty(ValueProp);

                ContextMenuTarget = NameField;
                NameField.RegisterValueChangedCallback(ev =>
                {
                    KeyField.value = NameField.value.GetHashCode();
                    parent.CallUpdateColors();
                });
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
}
