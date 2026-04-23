using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Codice.CM.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace AYellowpaper.Editor
{
    [CustomPropertyDrawer(typeof(InterfaceList<,>), true)]
    [CustomPropertyDrawer(typeof(IComponentList<>), true)]
    [CustomPropertyDrawer(typeof(IScriptableObjectList<>), true)]
    public class InterfaceListDawer : PropertyDrawer
    {


        //ListView listView;
        //
        //public override VisualElement CreatePropertyGUI(SerializedProperty property)
        //{
        //    listView = new ListView()
        //    {
        //        name = $"InterfaceList_{property.name}",
        //        headerTitle = property.displayName,
        //        allowAdd = true,
        //        allowRemove = true,
        //        reorderable = true,
        //        reorderMode = ListViewReorderMode.Simple,
        //        showAddRemoveFooter = true,
        //        bindItem = BindItem,
        //        dataSource = property.FindPropertyRelative("list")
        //    };
        //
        //    void BindItem(VisualElement V, int i) => V.Add(new PropertyField(property.FindPropertyRelative("list").GetArrayElementAtIndex(i)));
        //
        //    return listView;
        //}

        protected SerializedProperty property;
        protected SerializedProperty serializedListProperty;
        protected INgb_InterfaceListNGB targetList;
        protected ReorderableList reorderableList;
        protected GUIContent label;
        protected InterfaceObjectArguments interfaceObjectArguments;

        protected virtual string NoElementsDisplay => "This List is empty. Click the + button to add a new item.";

        protected bool IsReorderableListValid =>
            reorderableList != null
            && reorderableList.list != null
            && reorderableList.drawElementCallback != null
            && reorderableList.elementHeightCallback != null;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initialize(property, label);

            if (!Expanded)
                return EditorGUIUtility.singleLineHeight;

            MakeReorderableList();
            return reorderableList.GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property, label);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            MakeReorderableList();
            if (!Expanded) ReorderableList.defaultBehaviours.DrawHeaderBackground(position);
            reorderableList.DoList(position);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            }
            EditorGUI.EndProperty();
        }

        protected void Initialize(SerializedProperty property, GUIContent label)
        {
            if (property != null)
                this.property = property;

            if (this.property != null && serializedListProperty == null)
                serializedListProperty = this.property.FindPropertyRelative("list");

            if (fieldInfo != null && this.property != null && targetList == null)
                targetList = fieldInfo.GetValue(this.property.serializedObject.targetObject) as INgb_InterfaceListNGB;

            if (fieldInfo != null)
            {
                Type[] genericArgs = fieldInfo.FieldType.GenericTypeArguments;
                Type t1 = genericArgs[0];
                Type t2 = genericArgs.Length > 1
                    ? genericArgs[1]
                    : fieldInfo.FieldType == typeof(IComponentList<>)
                        ? typeof(Component)
                        : typeof(ScriptableObject);
                ;
                interfaceObjectArguments = new(t2, t1);
            }

            MakeReorderableList();
        }

        protected void MakeReorderableList()
        {
            if (IsReorderableListValid) return;

            if (property == null || property.serializedObject == null || property.serializedObject.targetObject == null)
                return;

            if (serializedListProperty == null && property != null)
                serializedListProperty = property.FindPropertyRelative("list");

            if (serializedListProperty == null)
            {
                Debug.LogWarning("SerializedDictionaryDrawer: Could not find 'List' property.");
                return;
            }

            Undo.RecordObject(property.serializedObject.targetObject, "Modify SerializedDictionary");

            reorderableList = new ReorderableList(property.serializedObject, serializedListProperty);
            if (targetList != null) reorderableList.list = targetList.listAccess;



            reorderableList.drawHeaderCallback = HeaderDrawer;
            reorderableList.drawElementCallback = (position, index, isActive, isFocused) =>
            {
                if (!Expanded) return;
                ElementDrawer(serializedListProperty.GetArrayElementAtIndex(index), position, index);
            };
            reorderableList.elementHeightCallback = index =>
            {
                return Expanded ? ElementHeight(serializedListProperty, index) : 0;
            };
            reorderableList.onAddCallback = list => AddNewItem(serializedListProperty, list);
            reorderableList.onRemoveCallback = list => RemoveItem(serializedListProperty, list);
            reorderableList.drawNoneElementCallback = rect =>
            {
                if (Expanded)
                    EditorGUI.LabelField(rect, NoElementsDisplay);
            };

            Expanded = Expanded;

            property.serializedObject.ApplyModifiedProperties();
        }

        protected bool Expanded
        {
            get => property?.isExpanded ?? false;
            set
            {
                if (property == null || reorderableList == null) return;
                property.isExpanded = value;
                reorderableList.displayAdd = value;
                reorderableList.displayRemove = value;
                reorderableList.draggable = value;
                //reorderableList.drawElementBackgroundCallback = value ? DrawElementBackground : null;
                //reorderableList.footerHeight = value ? EditorGUIUtility.singleLineHeight : 0;
                reorderableList.showDefaultBackground = value;
            }
        }


        protected virtual void HeaderDrawer(Rect rect)
        {
            var newRect = new Rect(rect.x, rect.y, rect.width - 10, rect.height);
            Expanded = EditorGUI.Foldout(newRect, Expanded, property.displayName, true);

            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition)) HeaderContextMenu(new());
        }

        protected virtual void HeaderContextMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Clear"), false, () =>
            {
                serializedListProperty.ClearArray();
                serializedListProperty.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            });
            menu.ShowAsContext();
            Event.current.Use();
        }

        protected virtual void ElementDrawer(SerializedProperty item, Rect position, int id)
        {
            EditorGUI.BeginChangeCheck();

            float normalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 20;
            GUIContent label = new($"{id}: ");
            var prop = property.FindPropertyRelative("list").GetArrayElementAtIndex(id);
            InterfaceReferenceUtility.OnGUI(position, prop, label, interfaceObjectArguments);
            EditorGUIUtility.labelWidth = normalLabelWidth;


            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            }
        }

        protected virtual float ElementHeight(SerializedProperty serializedListProperty, int index) => EditorGUIUtility.singleLineHeight;


        protected virtual void AddNewItem(SerializedProperty serializedListProperty, ReorderableList list)
        {
            int place = serializedListProperty.arraySize > 0 ? serializedListProperty.arraySize - 1 : 0;
            serializedListProperty.InsertArrayElementAtIndex(place);
            serializedListProperty.serializedObject.ApplyModifiedProperties();
            MakeReorderableList();
        }

        protected virtual void RemoveItem(SerializedProperty serializedListProperty, ReorderableList list)
        {
            if (serializedListProperty.arraySize > 0)
            {
                serializedListProperty.DeleteArrayElementAtIndex(list.index);
                serializedListProperty.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            }
        }

        #region Others
        //public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        //{
        //    var prop = property.FindPropertyRelative(_fieldName);
        //    InterfaceReferenceUtility.OnGUI(position, prop, label, GetArguments(fieldInfo));
        //}
        //
        //public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        //{
        //    var prop = property.FindPropertyRelative(_fieldName);
        //    return InterfaceReferenceUtility.GetPropertyHeight(prop, label, GetArguments(fieldInfo));
        //}

        private static void GetObjectAndInterfaceType(Type fieldType, out Type objectType, out Type interfaceType)
        {
            if (TryGetTypesFromInterfaceReference(fieldType, out objectType, out interfaceType))
                return;

            TryGetTypesFromList(fieldType, out objectType, out interfaceType);
        }

        private static bool TryGetTypesFromInterfaceReference(Type fieldType, out Type objectType, out Type interfaceType)
        {
            var fieldBaseType = fieldType;
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(InterfaceReference<>))
                fieldBaseType = fieldType.BaseType;

            if (fieldBaseType.IsGenericType && fieldBaseType.GetGenericTypeDefinition() == typeof(InterfaceReference<,>))
            {
                var types = fieldBaseType.GetGenericArguments();
                interfaceType = types[0];
                objectType = types[1];
                return true;
            }

            objectType = null;
            interfaceType = null;
            return false;
        }

        private static bool TryGetTypesFromList(Type fieldType, out Type objectType, out Type interfaceType)
        {
            Type listType = fieldType.GetInterfaces().FirstOrDefault(x =>
              x.IsGenericType &&
              x.GetGenericTypeDefinition() == typeof(IList<>));

            return TryGetTypesFromInterfaceReference(listType.GetGenericArguments()[0], out objectType, out interfaceType);
        }

        private static InterfaceObjectArguments GetArguments(FieldInfo fieldInfo)
        {
            GetObjectAndInterfaceType(fieldInfo.FieldType, out var objectType, out var interfaceType);
            return new InterfaceObjectArguments(objectType, interfaceType);
        }
        #endregion
    }
}
