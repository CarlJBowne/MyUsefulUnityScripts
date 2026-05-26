using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SLS.StateMachineH.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SLS.StateMachineH.Editor
{
    [CustomPropertyDrawer(typeof(ISerializedDictionaryNonGeneric), true)]
    internal class SerializedDictionaryDrawer : PropertyDrawer
    {

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement Display;

            Type DrawerType = typeof(SerializedDictionaryDrawer<,>)
                .MakeGenericType(fieldInfo.FieldType.GenericTypeArguments);
            var literal = fieldInfo.GetValue(property.serializedObject.targetObject)
                as ISerializedDictionaryNonGeneric;

            // Pass the live literal (the actual dictionary instance) to the drawer so it
            // can recalculate occurrences and provide proper binding. Using property.boxedValue
            // here returned a boxed/copy and left Literal null which caused blank/uneditable fields.
            Display = Activator.CreateInstance(DrawerType, property, literal) as VisualElement;

            return Display;
        }


    }

    internal class SerializedDictionaryDrawer<TK, TV> :
    SuperList<SerializedDictionaryDrawer<TK, TV>, SerializedDictionaryItem<TK, TV>, SerializedDictionary<TK, TV>.KeyValuePair>
    {
        public SerializedDictionaryDrawer(SerializedProperty listProperty, ISerializedDictionaryNonGeneric literal) : base(listProperty)
        {
            Literal = literal;
            BuildItems();
            //UpdateItems();
        }

        public override string nameSource => RootProperty.displayName;

        public override void InitializeProperty(SerializedProperty input)
        {
            RootProperty = input;
            property = input.FindPropertyRelative("serializedList");
        }

        public override void BuildItems()
        {
            base.BuildItems();
            CallUpdateColors();
        }

        public SerializedProperty RootProperty { get; protected set; }
        public ISerializedDictionaryNonGeneric Literal { get; protected set; }

        protected override void EstablishContextMenu(ContextualMenuPopulateEvent evt)
        {
            base.EstablishContextMenu(evt);
            var list = evt.menu.MenuItems();
            list.Insert(1, new DropdownMenuAction("Remove Duplicates", RemoveDuplicatesContextMenu, DropDownMenuStatus));
        }
        void RemoveDuplicatesContextMenu(DropdownMenuAction D)
        {
            Literal.RemoveDuplicates();
            RootProperty.serializedObject.Update();
            BuildItems();
            TryForceRefreshPrefabMarkers();
        }

        public void CallUpdateColors()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (Literal == null) return;
                bool[] dupes = Literal.RecalculateOccurences();
                if (i < dupes.Length) items[i].Invalid = dupes[i];
            }
        }

    }
    internal class SerializedDictionaryItem<TK, TV> : SuperListItem<SerializedDictionaryDrawer<TK, TV>, SerializedDictionaryItem<TK, TV>, SerializedDictionary<TK, TV>.KeyValuePair>
    {
        public SerializedDictionaryItem(SerializedDictionaryDrawer<TK, TV> parentList, SerializedProperty thisProperty) : base(parentList, thisProperty)
        { }

        public SerializedProperty KeyProp { get; protected set; }
        public VisualElement KeyField { get; protected set; }
        public SerializedProperty ValueProp { get; protected set; }
        public VisualElement ValueField { get; protected set; }

        protected override void InitializeProperty(SerializedProperty newprop)
        {
            property = newprop ?? parentList.property.GetArrayElementAtIndex(Index);
            KeyProp = property.FindPropertyRelative("Key");
            ValueProp = property.FindPropertyRelative("Value");
        }
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

            KeyField?.Unbind();
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
            ContextMenuTarget = KeyField;
            if (KeyField is TextField T)
                T.RegisterValueChangedCallback(ev => parentList.CallUpdateColors());
            else if (KeyField is PropertyField P)
                P.RegisterValueChangeCallback(ev => parentList.CallUpdateColors());
            else if (KeyField is IntegerField I)
                I.RegisterValueChangedCallback(ev => parentList.CallUpdateColors());
            else if (KeyField is FloatField F)
                F.RegisterValueChangedCallback(ev => parentList.CallUpdateColors());
            else if (KeyField is DoubleField D)
                D.RegisterValueChangedCallback(ev => parentList.CallUpdateColors());
        }

        public override void DuplicateContextMenu(DropdownMenuAction C)
            => parentList.DuplicatePropertySlotAt(Index - 1); //I couldn't tell you why this needs to be done.

    }

}