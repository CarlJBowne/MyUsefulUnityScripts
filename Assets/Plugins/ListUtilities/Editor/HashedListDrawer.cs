using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ListUtilities.Editor.Internal;

namespace ListUtilities.Editor
{
    [CustomPropertyDrawer(typeof(SerializedHashedList<>))]
    internal class HashedListDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement Display;
            Type DrawerType = typeof(ListDrawer<>)
                .MakeGenericType(fieldInfo.FieldType.GenericTypeArguments);
            var literal = fieldInfo.GetValue(property.serializedObject.targetObject);
            // Pass the live literal (the actual dictionary instance) to the drawer so it
            // can recalculate occurrences and provide proper binding. Using property.boxedValue
            // here returned a boxed/copy and left Literal null which caused blank/uneditable fields.
            Display = Activator.CreateInstance(DrawerType, property, literal, true) as VisualElement;
            return Display;
        }

        public class ListDrawer<T> : SuperList<ListDrawer<T>, ItemDrawer<T>, T>
        {
            public ListDrawer(SerializedProperty rootProperty, IHashedListPreGeneric literal, bool BindImmediately = true)
                : base(rootProperty, true)
            {
                Literal = literal;

                BuildBasicElements();
                if (BindImmediately) BindProperty(rootProperty);
                NewItemInput = new("New Item: ");
                NewItemInput.isDelayed = true;
                NewItemInput.style.display = DisplayStyle.None;
                this.Add(NewItemInput);
            }
            new public void BindProperty(SerializedProperty input)
            {
                property = input;
                NamesProperty = property.FindPropertyRelative("SerializedNames");
                KeysProperty = property.FindPropertyRelative("SerializedKeys");
                ValuesProperty = property.FindPropertyRelative("SerializedValues");
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

            public IHashedListPreGeneric Literal { get; private set; }
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
                NewItemInput.style.display = DisplayStyle.Flex;
                NewItemInput.SetValueWithoutNotify("Insert Name Here");
                NewItemInput.Focus();
                NewItemInput.RegisterValueChangedCallback(PostItemNaming);
            }
            private TextField NewItemInput;
            void PostItemNaming(ChangeEvent<string> value)
            {
                NewItemInput.UnregisterValueChangedCallback(PostItemNaming);

                CreatePropertySlot(out int newID);

                NamesProperty.GetArrayElementAtIndex(newID).stringValue = value.newValue;
                KeysProperty.GetArrayElementAtIndex(newID).intValue = value.newValue.GetHashCode();
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
                Literal.RemoveDuplicates();
                property.serializedObject.Update();
                BuildItems();
                TryForceRefreshPrefabMarkers();
            }


            public void CallUpdateColors()
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (Literal == null) return;
                    List<bool> dupes = Literal.Duplicates();
                    if (i < dupes.Count) items[i].Invalid = dupes[i];
                }
            }


        }
        internal class ItemDrawer<T> : SuperListItem<ListDrawer<T>, ItemDrawer<T>, T>
        {
            public ItemDrawer(ListDrawer<T> parentList, int Index) : base(parentList, Index) { }

            protected override void BindProperty()
            {
                this.NameProp = parent.NamesProperty.GetArrayElementAtIndex(Index);
                this.KeyProp = parent.KeysProperty.GetArrayElementAtIndex(Index);
                this.ValueProp = parent.ValuesProperty.GetArrayElementAtIndex(Index);
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
