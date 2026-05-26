using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using static SLS.StateMachineH.AnimatorAction;

namespace SLS.StateMachineH.Editor
{
    [UnityEditor.CustomPropertyDrawer(typeof(AnimatorAction))]
    public class AnimatorActionDrawer : UnityEditor.PropertyDrawer
    {
        EnumField EnumField;
        TextField NameField;
        IntegerField IDField; //Invisible.
        Button CacheButton;
        IntegerField LayerField;
        FloatField Float1Field;
        FloatField Float2Field;
        IntegerField IntField;
        BaseBoolField BoolField;

        private bool Cached = false;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Foldout root = new();
            root.text = property.displayName;
            root.BindProperty(property);


            SerializedProperty EnumProp = property.FindPropertyRelative(nameof(AnimatorAction.type));
            EnumField = new((AnimatorAction.Type)EnumProp.enumValueIndex)
            {
                label = "Action Type"
            };
            EnumField.BindProperty(EnumProp);
            EnumField.RegisterValueChangedCallback(OnEnumChanged);
            root.Add(EnumField);

            VisualElement IDRow = new();
            IDRow.style.flexDirection = FlexDirection.Row;
            root.Add(IDRow);

            SerializedProperty NameProp = property.FindPropertyRelative(nameof(AnimatorAction.NameID));
            SerializedProperty CacheProp = property.FindPropertyRelative(nameof(AnimatorAction.cachedHash));

            NameField = new TextField("ID");
            NameField.BindProperty(NameProp);
            NameField.SetValueWithoutNotify(NameProp.stringValue);
            NameField.style.flexGrow = 1f;
            NameField.isDelayed = true;
            NameField.RegisterValueChangedCallback(NameChanged);
            IDRow.Add(NameField);

            IDField = new IntegerField("Hash");
            IDField.BindProperty(CacheProp);
            IDField.SetValueWithoutNotify(CacheProp.intValue);
            IDField.style.display = DisplayStyle.None;
            IDRow.Add(IDField);

            if (IDField.value != -1) Cached = true;

            CacheButton = new(CacheButtonPressed)
            {
                text = "C",
                style =
                {
                    width = 20,
                    right = 0
                }
            };
            IDRow.Add(CacheButton);

            SetButtonState(Cached);

            LayerField = new("Layer");
            LayerField.BindProperty(property.FindPropertyRelative(nameof(AnimatorAction.layer)));
            root.Add(LayerField);

            Float1Field = new("Float Value 1");
            Float1Field.BindProperty(property.FindPropertyRelative(nameof(AnimatorAction.floatValue1)));
            root.Add(Float1Field);

            Float2Field = new("Float Value 1");
            Float2Field.BindProperty(property.FindPropertyRelative(nameof(AnimatorAction.floatValue2)));
            root.Add(Float2Field);

            IntField = new("Int Value");
            Float2Field.BindProperty(property.FindPropertyRelative(nameof(AnimatorAction.intValue)));
            root.Add(IntField);

            BoolField = new Toggle("Bool Value");
            BoolField.BindProperty(property.FindPropertyRelative(nameof(AnimatorAction.boolValue)));
            root.Add(BoolField);

            EnumField.value = (AnimatorAction.Type)EnumProp.enumValueIndex; //Force an update, hopefully.

            return root;
        }

        void OnEnumChanged(ChangeEvent<System.Enum> ev)
        {
            AnimatorAction.Type NewValue = ev.newValue as AnimatorAction.Type? ?? AnimatorAction.Type.Play;
            switch (NewValue)
            {
                case AnimatorAction.Type.Play:
                    Parameter(Float1Field, null);
                    Parameter(Float2Field, null);
                    Parameter(IntField, null);
                    Parameter(BoolField, null);
                    break;
                case AnimatorAction.Type.PlayAtPoint:
                    Parameter(Float1Field, "Time Offset");
                    Parameter(Float2Field, null);
                    Parameter(IntField, null);
                    Parameter(BoolField, null);
                    break;
                case AnimatorAction.Type.PlaySynced:
                    Parameter(Float1Field, null);
                    Parameter(Float2Field, null);
                    Parameter(IntField, null);
                    Parameter(BoolField, null);
                    break;
                case AnimatorAction.Type.CrossFade:
                    Parameter(Float1Field, "Transition Duration");
                    Parameter(Float2Field, null);
                    Parameter(IntField, null);
                    Parameter(BoolField, null);
                    break;
                case AnimatorAction.Type.CrossFadeAtPoint:
                    Parameter(Float1Field, "Transition Duration");
                    Parameter(Float2Field, "Time Offset");
                    Parameter(IntField, null);
                    Parameter(BoolField, null);
                    break;
                case AnimatorAction.Type.CrossFadeSynced:
                    Parameter(Float1Field, "Transition Duration");
                    Parameter(Float2Field, null);
                    Parameter(IntField, null);
                    Parameter(BoolField, null);
                    break;
                case AnimatorAction.Type.SetTrigger:
                    Parameter(Float1Field, null);
                    Parameter(Float2Field, null);
                    Parameter(IntField, null);
                    Parameter(BoolField, "Value");
                    break;
                case AnimatorAction.Type.SetBool:
                    Parameter(Float1Field, null);
                    Parameter(Float2Field, null);
                    Parameter(IntField, null);
                    Parameter(BoolField, "Value");
                    break;
                case AnimatorAction.Type.SetFloat:
                    Parameter(Float1Field, "Value");
                    Parameter(Float2Field, null);
                    Parameter(IntField, null);
                    Parameter(BoolField, null);
                    break;
                case AnimatorAction.Type.SetInt:
                    Parameter(Float1Field, null);
                    Parameter(Float2Field, null);
                    Parameter(IntField, "Value");
                    Parameter(BoolField, null);
                    break;
                default:
                    Parameter(Float1Field, null);
                    Parameter(Float2Field, null);
                    Parameter(IntField, null);
                    Parameter(BoolField, null);
                    break;
            }
        }

        void NameChanged(ChangeEvent<string> ev)
        {
            if (ev.newValue == ev.previousValue) return;
            IDField.value = -1;
            Cached = false;
            SetButtonState(false);
        }

        void CacheButtonPressed()
        {
            if (Cached) return;
            IDField.value = Animator.StringToHash(NameField.value);
            Cached = true;
            SetButtonState(true);
        }

        void SetButtonState(bool value)
        {
            CacheButton.style.color = value ? Color.green : Color.gray6;
            CacheButton.style.backgroundColor = value ? Color.darkGreen : Color.gray4;
        }

        void Parameter<T>(BaseField<T> target, string display) where T : new()
        {
            target.style.display = display != null ? DisplayStyle.Flex : DisplayStyle.None;
            target.label = display;
        }
    }
}
