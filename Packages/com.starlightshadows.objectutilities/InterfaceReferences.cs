using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using SLS.ListUtilities.Editor;
using SLS.EditorUtilities;
#endif

namespace SLS.ObjectUtilities
{
    [Serializable]
    public class InterfaceObject<T, U> where U : UnityEngine.Object where T : class
    {
        [SerializeField] U serial;

        public InterfaceObject() { }
        public InterfaceObject(T initValue) => Value = initValue;
        public InterfaceObject(U initValue) => Unity = initValue;

        public U Unity
        {
            get
            {
                if (serial == null) return null;
                if (serial is not U) return null;
                if (serial is not T) return null;
                return serial;
            }
            set
            {
                if (value is not U) return;
                if (value is not T) return;
                serial = value;
            }
        }
        public T Value
        {
            get
            {
                if (serial == null) return null;
                if (serial is not U) return null;
                if (serial is not T) return null;
                return serial as T;
            }
            set
            {
                if (value is not U) return;
                if (value is not T) return;
                serial = value as U;
            }
        }

        public Type Type => typeof(T);
        public Type UnityType => typeof(U);

        public static implicit operator T(InterfaceObject<T, U> source) => source.Value;
        public static implicit operator U(InterfaceObject<T, U> source) => source.Unity;
    }
    [Serializable]
    public class InterfaceList<T, U> : IList<T> where U : UnityEngine.Object where T : class
    {
        [SerializeField] List<U> serial;

        public Type Type => typeof(T);
        public Type UnityType => typeof(U);

        public T this[int i]
        {
            get => serial[i] as T;
            set => serial[i] = value as U;
        }

        public int Count => serial.Count;
        public bool IsReadOnly => false;

        public void Add(T item) => serial.Add(item as U);
        public void Add(U item) => serial.Add(item);

        public void Clear() => serial.Clear();
        public bool Contains(T item) => serial.Contains(item as U);
        public bool Contains(U item) => serial.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => serial.CopyTo(array.Cast<U>().ToArray(), arrayIndex);
        public IEnumerator<T> GetEnumerator() => serial.Cast<T>().GetEnumerator();
        public int IndexOf(T item) => serial.IndexOf(item as U);
        public void Insert(int index, U item) => serial.IndexOf(item, index);
        public void Insert(int index, T item) => serial.IndexOf(item as U, index);
        public bool Remove(U item) => serial.Remove(item);
        public bool Remove(T item) => serial.Remove(item as U);
        public void RemoveAt(int index) => serial.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public List<T> Literal => serial.Cast<T>().ToList();
        public List<U> Unity => serial.Cast<U>().ToList();
    }

    [Serializable]
    public class InterfaceMono<T> : InterfaceObject<T, MonoBehaviour> where T : class { }
    [Serializable]
    public class InterfaceAsset<T> : InterfaceObject<T, ScriptableObject> where T : class { }
    [Serializable]
    public class InterfaceMonoList<T> : InterfaceList<T, MonoBehaviour> where T : class { }
    [Serializable]
    public class InterfaceAssetList<T> : InterfaceList<T, ScriptableObject> where T : class { }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(InterfaceObject<,>), true)]
    public class RefEditor : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty valueProperty = property.FindPropertyRelative("serial");
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;
            string propertyPath = property.propertyPath;
            Type interfaceType = FindInterfaceObjectType(fieldInfo.FieldType, 0);
            Type unityType = FindInterfaceObjectType(fieldInfo.FieldType, 1);
            InterfaceObjectField field = new InterfaceObjectField(preferredLabel, unityType, interfaceType, selected =>
                        SetSerializedReference(targets, propertyPath, selected));

            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null || interfaceType.IsInstanceOfType(evt.newValue)) return;

                evt.StopImmediatePropagation();
                field.SetValueWithoutNotify(evt.previousValue);
            });
            field.BindProperty(valueProperty);
            return field;
        }

        public static Type FindInterfaceObjectType(Type fieldType, int genericArgumentIndex)
        {
            for (var type = fieldType; type != null; type = type.BaseType)
            {
                if (type.IsGenericType &&
                    (type.GetGenericTypeDefinition() == typeof(InterfaceObject<,>) || type.GetGenericTypeDefinition() == typeof(InterfaceList<,>)))
                    return type.GetGenericArguments()[genericArgumentIndex];
            }

            throw new InvalidOperationException($"{fieldType} is not an {typeof(InterfaceObject<,>)} field.");
        }

        public static void SetSerializedReference(UnityEngine.Object[] targets, string propertyPath, UnityEngine.Object selected)
        {
            foreach (var target in targets)
            {
                if (target == null) continue;

                var serializedObject = new SerializedObject(target);
                serializedObject.Update();
                var property = serializedObject.FindProperty(propertyPath);
                if (property == null)
                {
                    Debug.LogWarning($"Could not find serialized property '{propertyPath}' on {target}.", target);
                    continue;
                }

                property.objectReferenceValue = selected;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

    [CustomPropertyDrawer(typeof(InterfaceList<,>), true)]
    public class ListEditor : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Type interfaceType = RefEditor.FindInterfaceObjectType(fieldInfo.FieldType, 0);
            Type unityType = RefEditor.FindInterfaceObjectType(fieldInfo.FieldType, 1);

            return new SuperList(property, unityType, interfaceType, property.serializedObject.targetObjects);
        }



        public class SuperList : SuperList<SuperList, SuperListItem, UnityEngine.Object>
        {
            public SuperList(SerializedProperty rootProperty, Type unityType, Type interfaceType, UnityEngine.Object[] targets) : base(rootProperty, true)
            {
                BuildBasicElements();
                this.rootProperty = rootProperty;
                this.unityType = unityType;
                this.interfaceType = interfaceType;
                this.targets = targets;
                BindProperty(rootProperty.FindPropertyRelative("serial"), rootProperty.displayName);
            }

            protected override void AddButtonPressed()
            {
                base.AddButtonPressed();
                items[items.Count - 1].OpenPicker(worldBound);
            }

            SerializedProperty rootProperty;
            public Type unityType { get; private set; }
            public Type interfaceType { get; private set; }
            public UnityEngine.Object[] targets { get; private set; }

        }
        public class SuperListItem : SuperListItem<SuperList, SuperListItem, UnityEngine.Object>
        {
            public SuperListItem(SuperList parentList, int Index) : base(parentList, Index) { }

            InterfaceObjectField objectField;

            public override VisualElement Content()
            {
                objectField = new(Index.ToString(), parent.unityType, parent.interfaceType,
                    select => RefEditor.SetSerializedReference(parent.targets, property.propertyPath, select))
                {
                    style =
                    {
                        marginRight = 2,
                        flexShrink = 1
                    }
                };
                objectField.BindProperty(property);
                return objectField;
            }

            public void OpenPicker(Rect? overrideBound = null) => objectField.OpenPicker(overrideBound);
        }
    }

    internal class InterfaceObjectField : ObjectField
    {
        readonly Type unityType;
        readonly Type interfaceType;
        readonly Action<UnityEngine.Object> onSelected;

        public InterfaceObjectField(string label, Type unityType, Type interfaceType, Action<UnityEngine.Object> onSelected) : base(label)
        {
            this.unityType = unityType;
            this.interfaceType = interfaceType;
            this.onSelected = onSelected;
            this.objectType = unityType;
            this.allowSceneObjects = typeof(Component).IsAssignableFrom(unityType);
            this.labelElement.ShrinkToTextWidth(0);
            this.labelElement.style.minWidth = 0;

            VisualElement objectCircle = this.Q<VisualElement>(classes: "unity-object-field__selector");
            if (objectCircle != null)
            {
                objectCircle.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0) return;

                    evt.StopImmediatePropagation();
                    OpenPicker();
                }, TrickleDown.TrickleDown);
            }
            VisualElement objectWords = this.Q<VisualElement>(classes: "unity-object-field-display");
            if (objectWords != null)
            {
                Label intTypeLabel = new(interfaceType.Name)
                {
                    style =
                    {
                        paddingTop = 1,
                        paddingLeft = 0,
                        paddingRight = 0,
                        unityFontStyleAndWeight = FontStyle.Italic
                    }
                };
                objectWords.Add(intTypeLabel);
            }
        }

        public void OpenPicker(Rect? overrideBound = null) => UnityEditor.PopupWindow.Show(overrideBound ?? worldBound,
            new InterfaceObjectPicker(unityType, interfaceType, onSelected));
    }

    sealed class InterfaceObjectPicker : PopupWindowContent
    {
        readonly List<UnityEngine.Object> candidates;
        readonly Action<UnityEngine.Object> onSelected;
        ListView resultsList;
        TextField searchField;

        public InterfaceObjectPicker(Type unityType, Type interfaceType, Action<UnityEngine.Object> onSelected)
        {
            candidates = FindCandidates(unityType, interfaceType);
            this.onSelected = onSelected;
        }

        public override Vector2 GetWindowSize() => new(360, 420);

        public override VisualElement CreateGUI()
        {
            VisualElement root = new()
            {
                style =
                {
                    flexGrow = 1,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 4,
                    paddingBottom = 4
                }
            };

            searchField = new TextField { name = "interface-object-search" };
            searchField.style.marginBottom = 4;
            searchField.RegisterValueChangedCallback(_ => RefreshResults());
            root.Add(searchField);

            resultsList = new ListView
            {
                selectionType = SelectionType.None,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                fixedItemHeight = EditorGUIUtility.singleLineHeight + 4,
                makeItem = CreateResultRow,
                bindItem = BindResultRow
            };
            resultsList.style.flexGrow = 1;
            root.Add(resultsList);
            RefreshResults();
            return root;
        }

        VisualElement CreateResultRow()
        {
            VisualElement row = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = EditorGUIUtility.singleLineHeight + 4,
                    paddingLeft = 2,
                    paddingRight = 2
                }
            };
            row.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (row.userData is UnityEngine.Object candidate) Select(candidate);
                else if (Equals(row.userData, "None")) Select(null);
                evt.StopPropagation();
            });

            Image icon = new()
            {
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = EditorGUIUtility.singleLineHeight,
                    height = EditorGUIUtility.singleLineHeight,
                    flexShrink = 0,
                    marginRight = 4
                }
            };
            row.Add(icon);

            Label label = new()
            {
                name = "label",
                style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft }
            };
            row.Add(label);
            return row;
        }

        void BindResultRow(VisualElement element, int index)
        {
            var row = (VisualElement)element;
            var icon = row.Q<Image>();
            var label = row.Q<Label>("label");
            if (index == 0)
            {
                row.userData = "None";
                icon.image = null;
                label.text = "None";
                return;
            }

            var candidate = (UnityEngine.Object)resultsList.itemsSource[index];
            row.userData = candidate;
            icon.image = AssetPreview.GetMiniThumbnail(candidate);
            label.text = candidate.name;
            label.tooltip = $"{candidate.name} ({candidate.GetType().Name})";
        }

        void RefreshResults()
        {
            if (resultsList == null) return;

            var visibleCandidates = candidates
                .Where(candidate => MatchesSearch(candidate, searchField.value))
                .Cast<object>()
                .ToList();
            visibleCandidates.Insert(0, "None");
            resultsList.itemsSource = visibleCandidates;
            resultsList.Rebuild();
        }

        static List<UnityEngine.Object> FindCandidates(Type unityType, Type interfaceType)
        {
            var results = new List<UnityEngine.Object>();
            if (typeof(Component).IsAssignableFrom(unityType))
            {
                foreach (var candidate in Resources.FindObjectsOfTypeAll(unityType))
                {
                    if (candidate is not Component component || !component.gameObject.scene.IsValid() ||
                        !component.gameObject.scene.isLoaded || !Matches(candidate, unityType, interfaceType))
                        continue;

                    results.Add(candidate);
                }
            }
            else if (typeof(ScriptableObject).IsAssignableFrom(unityType))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (var candidate in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (Matches(candidate, unityType, interfaceType)) results.Add(candidate);
                    }
                }
            }

            return results
                .Distinct()
                .OrderBy(candidate => candidate.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        static bool Matches(UnityEngine.Object candidate, Type unityType, Type interfaceType) =>
            candidate != null && unityType.IsInstanceOfType(candidate) && interfaceType.IsInstanceOfType(candidate);

        static bool MatchesSearch(UnityEngine.Object candidate, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;

            var assetPath = AssetDatabase.GetAssetPath(candidate);
            return candidate.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   candidate.GetType().Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   assetPath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void Select(UnityEngine.Object candidate)
        {
            onSelected(candidate);
            editorWindow.Close();
        }
    }  

#endif
}