using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Utilities;
using Utilities.Xtensions.VisualElements;
using static Codice.CM.WorkspaceServer.WorkspaceTreeDataStore;
using Generics = System.Collections.Generic;


namespace Utilities
{
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : Generics.Dictionary<TKey, TValue>, ISerializationCallbackReceiver, ISerializedDictionaryNonGeneric
    {
        [SerializeField] internal Generics.List<KeyValuePair> serializedList;
        [NonSerialized] private Generics.Dictionary<TKey, Generics.List<int>> occurences;

        public SerializedDictionary() : base()
        {
            serializedList = new();
            occurences = new();
        }

        public System.Collections.IList listAccess => serializedList;

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
                RemoveDuplicates();
#else
            serializedList.Clear();  
            foreach (var kvp in this)  
                serializedList.Add(new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));  
#endif
        }

        public void OnAfterDeserialize()
        {
            base.Clear();
            foreach (var kvp in serializedList)
            {
#if UNITY_EDITOR
                if (!(kvp.Key == null || (kvp.Key is UnityEngine.Object unityObject && unityObject == null)) && !ContainsKey(kvp.Key))
                    base.Add(kvp.Key, kvp.Value);
#else
                Add(kvp.Key, kvp.Value);  
#endif
            }

#if UNITY_EDITOR
            RecalculateOccurences();
#else
            serializedList.Clear();  
#endif
        }

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                base[key] = value;
                bool anyEntryWasFound = false;
                for (int i = 0; i < serializedList.Count; i++)
                {
                    KeyValuePair kvp = serializedList[i];
                    if (!Generics.EqualityComparer<TKey>.Default.Equals(key, kvp.Key)) continue;
                    anyEntryWasFound = true;
                    kvp = new(kvp.Key, value);
                    serializedList[i] = kvp;
                }

                if (!anyEntryWasFound)
                    serializedList.Add(new(key, value));
            }
        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            serializedList.Add(new(key, value));
        }

        public void AddNew()
        {
            base.Add(default, default);
            serializedList.Add(new(default, default));
        }

        public new void Clear()
        {
            base.Clear();
            serializedList.Clear();
        }

        public new bool Remove(TKey key)
        {
            if (TryGetValue(key, out var value))
            {
                base.Remove(key);
                serializedList.RemoveAll(kvp => Generics.EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
                RecalculateOccurences();
                return true;
            }

            return false;
        }

        public new bool TryAdd(TKey key, TValue value)
        {
            if (base.TryAdd(key, value))
            {
                serializedList.Add(new(key, value));
                RecalculateOccurences();
                return true;
            }

            return false;
        }

        public bool[] RecalculateOccurences()
        {
            occurences.Clear();
            for (int i = 0; i < serializedList.Count; i++)
            {
                if (!occurences.ContainsKey(serializedList[i].Key)) occurences.Add(serializedList[i].Key, new(i));
                else occurences[serializedList[i].Key].Add(i);
            }
            return DuplicateValues;
        }

        public void RemoveDuplicates()
        {
            Generics.HashSet<TKey> firstInstances = new();
            for (int i = 0; i < serializedList.Count; i++)
            {
                if (firstInstances.Contains(serializedList[i].Key))
                {
                    serializedList.RemoveAt(i);
                    i--;
                }
                else firstInstances.Add(serializedList[i].Key);
            }
        }

        public bool[] DuplicateValues
        {
            get
            {
                Generics.List<bool> result = new();
                for (int i = 0; i < serializedList.Count; i++) result.Add(false);
                foreach (var item in occurences)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                        result[item.Value[i]] = true;
                }
                return result.ToArray();
            }
        }

        public object this[object key] { get => this[(TKey)key]; set => this[(TKey)key] = (TValue)value; }
        public void Add(object key, object value) => Add((TKey)key, (TValue)value);
        public bool Remove(object key) => Remove((TKey)key);
        public bool TryAdd(object key, object value) => TryAdd((TKey)key, (TValue)value);


        [Serializable]
        public struct KeyValuePair
        {
            public TKey Key;
            public TValue Value;

            public KeyValuePair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }
        [Serializable]
        public class LookupTable : System.Collections.Generic.List<KeyValuePair>, ILookupTableNonGeneric
        {
            public System.Collections.Generic.Dictionary<TKey, System.Collections.Generic.List<int>> occurences = new();

            // Add a method to expose the internal list as an array for SerializedProperty recognition  
            public KeyValuePair[] AsArray => this.ToArray();

            public void RecalculateOccurences()
            {
                occurences.Clear();
                for (int i = 0; i < Count; i++)
                {
                    if (!occurences.ContainsKey(this[i].Key)) occurences.Add(this[i].Key, new(i));
                    else occurences[this[i].Key].Add(i);
                }
            }

            public void RemoveDuplicates()
            {
                Generics.HashSet<TKey> firstInstances = new();
                for (int i = 0; i < Count; i++)
                {
                    if (firstInstances.Contains(this[i].Key))
                    {
                        this.RemoveAt(i);
                        i--;
                    }
                    else firstInstances.Add(this[i].Key);
                }
            }

            public bool[] DuplicateValues
            {
                get
                {
                    Generics.List<bool> result = new(Count);
                    foreach (var item in occurences)
                    {
                        for (int i = 1; i < item.Value.Count; i++)
                            result[item.Value[i]] = true;
                    }
                    return result.ToArray();
                }
            }
        }
    }

    [Serializable]
    public class SerializedReferenceDictionary<TKey, TValue> : Generics.Dictionary<TKey, TValue>, ISerializationCallbackReceiver, ISerializedDictionaryNonGeneric
    {
        [SerializeField] internal Generics.List<KeyValuePair> serializedList;
        [NonSerialized] private Generics.Dictionary<TKey, Generics.List<int>> occurences;

        public SerializedReferenceDictionary() : base()
        {
            serializedList = new();
            occurences = new();
        }

        public System.Collections.IList listAccess => serializedList;

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
                RemoveDuplicates();
#else
            serializedList.Clear();  
            foreach (var kvp in this)  
                serializedList.Add(new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));  
#endif
        }

        public void OnAfterDeserialize()
        {
            base.Clear();
            foreach (var kvp in serializedList)
            {
#if UNITY_EDITOR
                if (!(kvp.Key == null || (kvp.Key is UnityEngine.Object unityObject && unityObject == null)) && !ContainsKey(kvp.Key))
                    base.Add(kvp.Key, kvp.Value);
#else
                Add(kvp.Key, kvp.Value);  
#endif
            }

#if UNITY_EDITOR
            RecalculateOccurences();
#else
            serializedList.Clear();  
#endif
        }

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                base[key] = value;
                bool anyEntryWasFound = false;
                for (int i = 0; i < serializedList.Count; i++)
                {
                    KeyValuePair kvp = serializedList[i];
                    if (!Generics.EqualityComparer<TKey>.Default.Equals(key, kvp.Key)) continue;
                    anyEntryWasFound = true;
                    kvp = new(kvp.Key, value);
                    serializedList[i] = kvp;
                }

                if (!anyEntryWasFound)
                    serializedList.Add(new(key, value));
            }
        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            serializedList.Add(new(key, value));
        }

        public void AddNew()
        {
            base.Add(default, default);
            serializedList.Add(new(default, default));
        }

        public new void Clear()
        {
            base.Clear();
            serializedList.Clear();
        }

        public new bool Remove(TKey key)
        {
            if (TryGetValue(key, out var value))
            {
                base.Remove(key);
                serializedList.RemoveAll(kvp => Generics.EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
                RecalculateOccurences();
                return true;
            }

            return false;
        }

        public new bool TryAdd(TKey key, TValue value)
        {
            if (base.TryAdd(key, value))
            {
                serializedList.Add(new(key, value));
                RecalculateOccurences();
                return true;
            }

            return false;
        }

        public bool[] RecalculateOccurences()
        {
            occurences.Clear();
            for (int i = 0; i < serializedList.Count; i++)
            {
                if (!occurences.ContainsKey(serializedList[i].Key)) occurences.Add(serializedList[i].Key, new(i));
                else occurences[serializedList[i].Key].Add(i);
            }
            return DuplicateValues;
        }

        public void RemoveDuplicates()
        {
            Generics.HashSet<TKey> firstInstances = new();
            for (int i = 0; i < serializedList.Count; i++)
            {
                if (firstInstances.Contains(serializedList[i].Key))
                {
                    serializedList.RemoveAt(i);
                    i--;
                }
                else firstInstances.Add(serializedList[i].Key);
            }
        }

        public bool[] DuplicateValues
        {
            get
            {
                Generics.List<bool> result = new();
                for (int i = 0; i < serializedList.Count; i++) result.Add(false);
                foreach (var item in occurences)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                        result[item.Value[i]] = true;
                }
                return result.ToArray();
            }
        }

        public object this[object key] { get => this[(TKey)key]; set => this[(TKey)key] = (TValue)value; }
        public void Add(object key, object value) => Add((TKey)key, (TValue)value);
        public bool Remove(object key) => Remove((TKey)key);
        public bool TryAdd(object key, object value) => TryAdd((TKey)key, (TValue)value);


        [Serializable]
        public struct KeyValuePair
        {
            public TKey Key;
            [SerializeReference] public TValue Value;

            public KeyValuePair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }
        [Serializable]
        public class LookupTable : System.Collections.Generic.List<KeyValuePair>, ILookupTableNonGeneric
        {
            public System.Collections.Generic.Dictionary<TKey, System.Collections.Generic.List<int>> occurences = new();

            // Add a method to expose the internal list as an array for SerializedProperty recognition  
            public KeyValuePair[] AsArray => this.ToArray();

            public void RecalculateOccurences()
            {
                occurences.Clear();
                for (int i = 0; i < Count; i++)
                {
                    if (!occurences.ContainsKey(this[i].Key)) occurences.Add(this[i].Key, new(i));
                    else occurences[this[i].Key].Add(i);
                }
            }

            public void RemoveDuplicates()
            {
                Generics.HashSet<TKey> firstInstances = new();
                for (int i = 0; i < Count; i++)
                {
                    if (firstInstances.Contains(this[i].Key))
                    {
                        this.RemoveAt(i);
                        i--;
                    }
                    else firstInstances.Add(this[i].Key);
                }
            }

            public bool[] DuplicateValues
            {
                get
                {
                    Generics.List<bool> result = new(Count);
                    foreach (var item in occurences)
                    {
                        for (int i = 1; i < item.Value.Count; i++)
                            result[item.Value[i]] = true;
                    }
                    return result.ToArray();
                }
            }
        }
    }

    public interface ISerializedDictionaryNonGeneric
    {
        public System.Collections.IList listAccess { get; }

        public void OnBeforeSerialize();
        public void OnAfterDeserialize();
        public object this[object key] { get; set; }
        public void Add(object key, object value);
        public void Clear();
        public bool Remove(object key);
        public bool TryAdd(object key, object value);

        public bool[] RecalculateOccurences();
        public bool[] DuplicateValues { get; }
        public void RemoveDuplicates();
    }
    public interface ILookupTableNonGeneric
    {
        public bool[] DuplicateValues { get; }
        public void RecalculateOccurences();
        public void RemoveDuplicates();
    }
}

namespace Utilities.Editor
{
    [CustomPropertyDrawer(typeof(ISerializedDictionaryNonGeneric), true)]
    public class SerializedDictionaryDrawer : PropertyDrawer
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

        /*
        protected SerializedProperty property;
        protected SerializedProperty serializedListProperty;
        protected ISerializedDictionaryNonGeneric targetDictionary;
        protected ReorderableList reorderableList;

        protected readonly Color redWarning = new Color(1.5f, 1, 1);
        protected virtual string NoElementsDisplay => "This dictionary is empty. Click the + button to add a new item.";

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
                serializedListProperty = this.property.FindPropertyRelative("serializedList");

            if (fieldInfo != null && this.property != null && targetDictionary == null)
                targetDictionary = fieldInfo.GetValue(this.property.serializedObject.targetObject) as ISerializedDictionaryNonGeneric;

            MakeReorderableList();
        }

        protected void MakeReorderableList()
        {
            if (IsReorderableListValid) return;

            if (property == null || property.serializedObject == null || property.serializedObject.targetObject == null)
                return;

            if (serializedListProperty == null && property != null)
                serializedListProperty = property.FindPropertyRelative("serializedList");

            if (serializedListProperty == null)
            {
                Debug.LogWarning("SerializedDictionaryDrawer: Could not find 'serializedList' property.");
                return;
            }

            Undo.RecordObject(property.serializedObject.targetObject, "Modify SerializedDictionary");

            reorderableList = new ReorderableList(property.serializedObject, serializedListProperty);
            if (targetDictionary != null) reorderableList.list = targetDictionary.listAccess;



            reorderableList.drawHeaderCallback = HeaderDrawer;
            reorderableList.drawElementCallback = (position, index, isActive, isFocused) =>
            {
                if (!Expanded) return;
                KeyValuePairDrawer(serializedListProperty.GetArrayElementAtIndex(index), position, index, IsDuplicate(index));
            };
            reorderableList.elementHeightCallback = index =>
            {
                return Expanded ? KeyValuePairHeight(serializedListProperty, index) : 0;
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
                targetDictionary?.RecalculateOccurences();
                serializedListProperty.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            });
            menu.AddItem(new GUIContent("Remove Duplicates"), false, () =>
            {
                targetDictionary?.RemoveDuplicates();
                targetDictionary?.RecalculateOccurences();
                serializedListProperty.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            });
            menu.ShowAsContext();
            Event.current.Use();
        }

        protected virtual void KeyValuePairDrawer(SerializedProperty item, Rect position, int id, bool isDupe)
        {
            var keyProperty = item.FindPropertyRelative("Key");
            var valueProperty = item.FindPropertyRelative("Value");

            if (keyProperty == null || valueProperty == null) return;

            float keyHeight = EditorGUI.GetPropertyHeight(keyProperty, true);
            float valueHeight = EditorGUI.GetPropertyHeight(valueProperty, true);
            float elementHeight = Mathf.Max(keyHeight, valueHeight);

            Rect keyRect = new Rect(position.x, position.y, position.width * 0.3f, elementHeight);
            Rect valueRect = new Rect(position.x + position.width * 0.3f, position.y, position.width * 0.7f, elementHeight);

            var prevColor = GUI.color;
            if (isDupe) GUI.color = redWarning;

            try
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);
            }
            finally { GUI.color = prevColor; }
            EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            }
        }

        protected virtual float KeyValuePairHeight(SerializedProperty serializedListProperty, int index)
        {
            var element = serializedListProperty.GetArrayElementAtIndex(index);
            var keyProperty = element.FindPropertyRelative("Key");
            var valueProperty = element.FindPropertyRelative("Value");
            return Mathf.Max(
                EditorGUI.GetPropertyHeight(keyProperty, true),
                EditorGUI.GetPropertyHeight(valueProperty, true),
                EditorGUIUtility.singleLineHeight
            );
        }


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

        protected bool IsDuplicate(int id)
        {
            bool[] duplicates = targetDictionary?.DuplicateValues;
            return duplicates != null && duplicates.Length > id && duplicates[id];
        }

        */
    }

    public class SerializedDictionaryDrawer<TK, TV> :
    SuperList<SerializedDictionaryDrawer<TK, TV>, SerializedDictionaryItem<TK, TV>, SerializedDictionary<TK, TV>.KeyValuePair>
    {
        public SerializedDictionaryDrawer(SerializedProperty listProperty, ISerializedDictionaryNonGeneric literal) : base(listProperty)
        {
            Literal = literal;
            BuildItems();
            //UpdateItems();
        }

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
    public class SerializedDictionaryItem<TK, TV> : SuperListItem<SerializedDictionaryDrawer<TK, TV>, SerializedDictionaryItem<TK, TV>, SerializedDictionary<TK, TV>.KeyValuePair>
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

            if (KeyField != null) KeyField.Unbind();
            KeyField =
                typeof(TK) == typeof(string) ? new TextField().AddTo(content, k =>
                {
                    k.label = "";
                    k.SetValueWithoutNotify(KeyProp.stringValue);
                    k.BindProperty(KeyProp);
                    k.isDelayed = true;
                })
                : typeof(TK) == typeof(int) ? new IntegerField().AddTo(content, k =>
                {
                    k.label = "";
                    k.SetValueWithoutNotify(KeyProp.intValue);
                    k.BindProperty(KeyProp);
                    k.isDelayed = true;
                })
                : typeof(TK) == typeof(float) ? new FloatField().AddTo(content, k =>
                {
                    k.label = "";
                    k.SetValueWithoutNotify(KeyProp.floatValue);
                    k.BindProperty(KeyProp);
                    k.isDelayed = true;
                })
                : typeof(TK) == typeof(double) ? new DoubleField().AddTo(content, k =>
                {
                    k.label = "";
                    k.SetValueWithoutNotify(KeyProp.doubleValue);
                    k.BindProperty(KeyProp);
                    k.isDelayed = true;
                })
                : new PropertyField(KeyProp, "").AddTo(content, k =>
                {
                    k.RegisterCallback<ContextualMenuPopulateEvent>(ContextMenu, TrickleDown.TrickleDown);
                });

            KeyField.style.flexBasis = new Length(30, LengthUnit.Percent);

            if (ValueField != null) ValueField.Unbind();
            ValueField = new PropertyField(ValueProp, "").AddTo(content, v =>
            {
                v.style.flexBasis = new Length(70, LengthUnit.Percent);
                v.style.marginRight = 4;
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

    }

}