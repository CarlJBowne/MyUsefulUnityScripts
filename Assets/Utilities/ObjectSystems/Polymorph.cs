using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using Utilities.Xtensions;
using Utilities.Xtensions.VisualElements;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[System.Serializable]
public abstract class Polymorph
{
    [System.Serializable]
    public class ListOf<T> : IList<T> where T : Polymorph
    {
        [SerializeField, SerializeReference]
        public List<T> items = new();

        // IList<T> implementation - delegate to the inner list.
        #region IList implementation
        public T this[int index]
        {
            get => items[index];
            set => items[index] = value;
        }

        public int Count => items.Count;
        public bool IsReadOnly => ((ICollection<T>)items).IsReadOnly;

        public void Add(T item) => items.Add(item);
        public void Clear() => items.Clear();
        public bool Contains(T item) => items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        public int IndexOf(T item) => items.IndexOf(item);
        public void Insert(int index, T item) => items.Insert(index, item);
        public bool Remove(T item) => items.Remove(item);
        public void RemoveAt(int index) => items.RemoveAt(index);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)items).GetEnumerator();
        #endregion
    }

    [System.Serializable]
    public class UniqueList<T> : IList<T>, ISerializationCallbackReceiver where T : Polymorph
    {
        [SerializeField, SerializeReference]
        public List<T> items = new();
        // IList<T> implementation with uniqueness enforcement.
        #region IList implementation
        public T this[int index]
        {
            get => items[index];
            set
            {
                if (value != null)
                {
                    // Ensure no other slot contains the same runtime type.
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (i == index) continue;
                        var existing = items[i];
                        if (existing != null && existing.GetType() == value.GetType() && !ReferenceEquals(existing, value))
                            throw new InvalidOperationException($"Cannot add duplicate item of type '{value.GetType().Name}' to UniqueList.");
                    }
                }
                items[index] = value;
            }
        }
        public int Count => items.Count;
        public bool IsReadOnly => ((ICollection<T>)items).IsReadOnly;
        public void Add(T item)
        {
            if (item != null)
            {
                if (items.Any(e => e != null && e.GetType() == item.GetType() && !ReferenceEquals(e, item)))
                    throw new InvalidOperationException($"Cannot add duplicate item of type '{item.GetType().Name}' to UniqueList.");
            }
            items.Add(item);
        }
        public void Clear() => items.Clear();
        public bool Contains(T item) => items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        public int IndexOf(T item) => items.IndexOf(item);

        public void Insert(int index, T item)
        {
            if (item != null)
            {
                if (items.Any(e => e != null && e.GetType() == item.GetType() && !ReferenceEquals(e, item)))
                    throw new InvalidOperationException($"Cannot insert duplicate item of type '{item.GetType().Name}' to UniqueList.");
            }
            items.Insert(index, item);
        }

        public bool Remove(T item) => items.Remove(item);
        public void RemoveAt(int index) => items.RemoveAt(index);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)items).GetEnumerator();
        #endregion

        [SerializeField] private List<Type> typeKey = new();

        // Additional dictionary-like and utility methods:

        /// <summary>
        /// Gets the value associated with the specified type.
        /// </summary>
        /// <param name="I">The type whose associated value to get.</param>
        /// <returns>The value associated with the specified type.</returns>
        public T this[Type I] => typeKey.Contains(I) ? items[typeKey.IndexOf(I)] : null;

        /// <summary>
        /// Returns the first stored element whose runtime Type equals the provided Type, or null if none.
        /// </summary>
        public T GetByType(Type type)
        {
            if (type == null) return null;
            return items.FirstOrDefault(e => e != null && e.GetType() == type);
        }

        /// <summary>
        /// Tries to get an element by runtime Type.
        /// </summary>
        public bool TryGetByType(Type type, out T value)
        {
            value = GetByType(type);
            return value != null;
        }

        /// <summary>
        /// Typed convenience getter. Returns the stored instance of U (or null).
        /// </summary>
        public U Get<U>() where U : T
        {
            var found = items.FirstOrDefault(e => e is U);
            return (U)found;
        }

        /// <summary>
        /// Typed try-get convenience.
        /// </summary>
        public bool TryGet<U>(out U value) where U : T
        {
            var found = items.FirstOrDefault(e => e is U);
            value = (U)found;
            return found != null;
        }

        /// <summary>
        /// Returns whether any element of the given runtime Type exists in the list.
        /// </summary>
        public bool ContainsType(Type type)
        {
            if (type == null) return false;
            return items.Any(e => e != null && e.GetType() == type);
        }

        /// <summary>
        /// Returns index of the element whose runtime Type equals the provided Type, or -1.
        /// </summary>
        public int IndexOfType(Type type)
        {
            if (type == null) return -1;
            for (int i = 0; i < items.Count; i++)
            {
                var e = items[i];
                if (e != null && e.GetType() == type) return i;
            }
            return -1;
        }

        /// <summary>
        /// Replace the existing element of the given runtime Type with 'item' or add it if missing.
        /// If 'item' is non-null its runtime type must match 'type'.
        /// </summary>
        public void SetByType(Type type, T item)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (item != null && item.GetType() != type) throw new ArgumentException("Item type does not match provided type.", nameof(item));

            int idx = IndexOfType(type);
            if (idx >= 0) items[idx] = item;
            else Add(item);
        }

        public void OnBeforeSerialize()
        {
            typeKey.Clear();
            for (int i = 0; i < items.Count; i++) 
                if (items[i] != null) 
                    typeKey.Add(items[i].GetType());
        }
        public void OnAfterDeserialize() { }
    }

#if UNITY_EDITOR

    public virtual bool OverrideBody(VisualElement container, SerializedProperty property) => false;

    public static Type[] GetSubtypes(Type baseType)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t =>
                !t.IsAbstract &&
                // For interfaces, include implementers; for classes, include strict subclasses only.
                t.IsSubclassOf(baseType) && (t.IsPublic || t.IsNestedPublic || t.IsNestedFamORAssem || t.IsNestedFamily)
            )
            .ToArray();
    }

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

    [CustomPropertyDrawer(typeof(Polymorph), true)]
    public class Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => new HeaderDrawer(property);
    }

    //Note: Consider making a "No Choosing Header" option, but that's more or less useless so maybe ignore this.


    public class HeaderDrawer : VisualElement
    {
        public HeaderDrawer(SerializedProperty p) : base()
        {
            property = p;
            BaseType = GetDeclaredFieldType() ?? typeof(Polymorph);
            CurrentType = property?.managedReferenceValue?.GetType();
            name = $"HeaderDrawer-{BaseType.Name}-{property.name}";

            propertyField ??= new PropertyField(p)
            {
                name = $"HeaderDrawer-PropertyField__{p.name}"
            };
            if (!this.Contains(propertyField)) this.Add(propertyField);

            changeButton ??= new Button(TypeButtonClick)
            {
                name = "Type Chooser",
                text = "*",
                style =
                        {
                            alignSelf = Align.FlexEnd,
                            flexDirection = FlexDirection.Row,
                            position = Position.Absolute,
                            width = 16,
                            height = 16,
                            fontSize = 18,
                            flexGrow = 1,
                            paddingTop = 3,
                            paddingBottom = 0,
                            paddingLeft = 0,
                            paddingRight = 0,
                            right = -1,
                            top = 0
                        }
            };
            if (!this.Contains(changeButton)) this.Add(changeButton);


            if (TryCacheFoldout()) foldout.value = true;

            // Schedule Delayed building of the Layout.
            this.DelayedBuild(Update);
        }

        void Update()
        {
            // Update label and toggle UI. Create the TypeButton once and only add it to the labelElement if not already present.
            if (this.QCache(out label, className: "unity-label"))
            {
                label.text = CorrectLabel;

                label.style.right = 0;
                label.style.flexGrow = 1;
                label.style.height = EditorGUIUtility.singleLineHeight;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
            }

            TryCacheFoldout();
            this.QCache(out contentContainer, "unity-content");

            //Handle other hasInstance specific pieces.
            if (this.QCache(out toggle, className: "unity-foldout__checkmark"))
            {
                toggle.style.marginRight = 1;
                toggle.style.marginBottom = 0;
                toggle.style.marginTop = 0;
                if (CurrentType == null) toggle.value = false;
            }

            if (this.QCache(out toggleArrow, "unity-checkmark")) toggleArrow.visible = CurrentType != null;

            if (property.managedReferenceValue is not null and Polymorph O && bodyInvalid)
            {
                if (contentContainer == null) return;
                if (O.OverrideBody(contentContainer, property))
                    contentContainer.Bind(property.serializedObject);

                HeaderDrawer dupe;
                if (propertyField.QCache(out dupe) && dupe.parent == propertyField)
                {
                    PropertyField oldPropField = propertyField;
                    propertyField = dupe.propertyField;
                    this.Remove(oldPropField);
                    this.Add(propertyField);
                    Remove(changeButton);
                    Add(changeButton);
                    Update();
                }

                bodyInvalid = false;
            }

        }

        void UpdateType(Type t) => UpdateType(t, false);
        void UpdateType(Type t, bool forceRebuild = false)
        {
            if (property == null || (t == CurrentType && !forceRebuild)) return;

            bool wasPreviouslyNull = CurrentType == null && t != null;
            if (CurrentType != t)
            {
                if (t != null) property.managedReferenceValue = Activator.CreateInstance(t);
                else property.managedReferenceValue = null;
            }

            CurrentType = t;
            //bodyInvalidated = true;

            // Re-bind the hidden anchor (the only bound element) to ensure prefab behavior remains correct.
            //try { overrideAnchor?.Bind(property.serializedObject); } catch { /* defensive */ }

            if (foldout != null || TryCacheFoldout()) foldout.value = true;

            // Apply the modification so the SerializedProperty reflects the new instance/type.
            property.serializedObject.ApplyModifiedProperties();

            bodyInvalid = true;

            // Rebuild the visible parts of the HeaderDrawer.
            if (!wasPreviouslyNull) Update();
            else propertyField.DelayedBuild(Update);

            if (foldout != null || TryCacheFoldout()) foldout.value = true;

            // Notify listeners of the type change.
            OnTypeChanged?.Invoke(property?.managedReferenceValue?.GetType());
        }

        //Pieces
        PropertyField propertyField;
        Button changeButton;
        Toggle toggle;
        Foldout foldout;
        Label label;
        new VisualElement contentContainer;
        VisualElement toggleArrow;


        //Data
        public SerializedProperty property { get; protected set; }
        public Type BaseType { get; protected set; }
        public Type CurrentType { get; protected set; }
        bool bodyInvalid = true;
        public Action<Type> OnTypeChanged;
        public bool drawnSuccessfully { get; private set; } = false;

        #region PartGetters

        public Button ChangeButton => changeButton;

        #endregion


        //VisualElement bodyDrawer;
        //bool bodyInvalidated = true;

        // Hidden bound anchor used to preserve prefab Apply/Revert behavior even when value is null.
        //private PropertyField overrideAnchor;
        private string NAME => name;

        Type GetDeclaredFieldType()
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

        static FieldInfo GetFieldInfoRecursive(Type type, string fieldName)
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

        string CorrectLabel => CurrentType != null ? $"{property.displayName} ({CurrentType.Name})" : property.displayName;

        void TypeButtonClick() => ShowChooseTypeMenu(BaseType, CurrentType != null, UpdateType);

        bool TryCacheFoldout() => this.QCache(out foldout, className: "unity-foldout");

        //PropertyField OverrideAnchor()
        //{
        //    // Ensure anchor still exists and is bound (insulates against inspector re-creation).
        //    if (overrideAnchor == null && property != null)
        //    {
        //        overrideAnchor = new PropertyField(property);
        //        overrideAnchor.name = "headerDrawer_overrideAnchor";
        //        overrideAnchor.style.display = DisplayStyle.None;
        //        this.hierarchy.Add(overrideAnchor);
        //        try { overrideAnchor.Bind(property.serializedObject); } catch { /* ignore */ }
        //    }
        //    return overrideAnchor;
        //}
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

                bodyDrawer = new Polymorph.HeaderDrawer(property);
                contentContainer.Add(bodyDrawer);

                UpdateLiteralObject(property.managedReferenceValue?.GetType());
                bodyDrawer.OnTypeChanged += UpdateLiteralObject;
            }

            public string displayName { get; private set; }
            public SerializedProperty property { get; private set; }
            public Polymorph.HeaderDrawer bodyDrawer { get; private set; }


            private void UpdateLiteralObject(Type T) => tabHeader.style.color = T != null ? Color.white : Color.gray;
        }
    }

    [CustomPropertyDrawer(typeof(ListOf<>), true)]
    public class ListOfDrawer : PropertyDrawer
    {
        // Stored visual pieces to resemble VisualElementsHelpers.SuperList structure
        protected SerializedProperty rootProperty;
        protected SerializedProperty listProperty;
        protected Type baseType;
        protected VisualElement root;
        protected VisualElement headerBar;
        protected Label titleLabel;
        protected Label counterLabel;
        protected Button addButton;
        protected VisualElement collection;
        protected List<Item> itemElements = new();

        // Foldout pieces
        private Label foldoutArrow;
        private bool foldoutState = true;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            rootProperty = property;

            // Root
            root = new VisualElement();
            root.name = $"ListOfDrawer-{property.propertyPath}";

            // Backing array property (robust resolution)
            listProperty = property.FindPropertyRelative("items")
                ?? throw new Exception($"Polymorph.ListOfDrawer: Could not resolve 'items' SerializedProperty for '{property.propertyPath}'.");

            // Resolve element (generic) type from FieldInfo where possible
            try
            {
                var fi = fieldInfo;
                if (fi != null)
                {
                    var ft = fi.FieldType;
                    if (ft.IsGenericType)
                    {
                        var args = ft.GetGenericArguments();
                        if (args != null && args.Length > 0) baseType = args[0];
                    }
                }
            }
            catch { baseType = null; }

            // Establish visual elements and styling
            EstablishVisualElements();

            // Add to root
            root.Add(headerBar);
            root.Add(collection);

            // Initial build
            BuildItems();

            // Bind root for prefab/apply support (defensive)
            try { root.Bind(property.serializedObject); } catch { }

            return root;
        }

        // Helper: creates and styles headerBar, titleLabel, counterLabel, addButton, collection
        private void EstablishVisualElements()
        {
            // Header bar
            headerBar = new()
            {
                name = "listof-headerbar",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 20,
                    backgroundColor = .2078432f.Gray(),
                    borderRightColor = .1411765f.Gray(),
                    borderLeftColor = .1411765f.Gray(),
                    borderTopColor = .1411765f.Gray(),
                    borderBottomColor = .1411765f.Gray(),
                    borderRightWidth = 1,
                    borderLeftWidth = 1,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    paddingLeft = 4,
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6
                }
            };

            // Foldout arrow - left side
            foldoutArrow = new("▾")
            {
                name = "listof-foldout",
                style =
                {
                    width = 18,
                    fontSize = 25,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginRight = 6,
                    color = .75f.Gray()
                }
            };
            foldoutArrow.RegisterCallback<ClickEvent>((evt) =>
            {
                // Toggle persistent expanded state if possible, otherwise toggle internal state
                if (rootProperty != null)
                {
                    rootProperty.isExpanded = !rootProperty.isExpanded;
                    try { rootProperty.serializedObject.ApplyModifiedProperties(); } catch { }
                }
                else
                {
                    foldoutState = !foldoutState;
                }
                UpdateFoldoutVisuals();
                evt.StopPropagation();
            });
            foldoutArrow.RegisterHoverEvents(v => foldoutArrow.style.color = v ? Color.white : .75f.Gray());
            headerBar.Add(foldoutArrow);

            // Title label
            titleLabel = new(rootProperty.displayName)
            {
                name = "listof-title",
                style =
                {
                    flexGrow = 1,
                    fontSize = 12,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    color = .82f.Gray(),
                }
            };
            headerBar.Add(titleLabel);

            // Counter label
            counterLabel = new((listProperty != null) ? listProperty.arraySize.ToString() : "0")
            {
                name = "listof-counter",
                style =
                {
                    width = 36,
                    unityTextAlign = TextAnchor.MiddleRight,
                    color = .85f.Gray(),
                    marginRight = 6
                }
            };
            headerBar.Add(counterLabel);

            // Add button
            addButton = new(ShowTypeChooser)
            {
                text = "+",
                name = "listof-add",
                style =
                {
                    width = 24,
                    height = 18,
                    backgroundColor = Color.clear,
                    borderBottomColor = Color.clear, borderTopColor = Color.clear,
                    borderLeftColor = Color.clear, borderRightColor = Color.clear,
                    fontSize = 14,
                    unityTextAlign = TextAnchor.LowerCenter,
                    borderRightWidth = 0, borderBottomWidth = 0, borderLeftWidth = 0, borderTopWidth = 0,
                    borderTopRightRadius = 6,
                    marginBottom = 0, marginLeft = 0, marginRight = 0, marginTop = 0,
                    paddingBottom = 0, paddingLeft = 0, paddingRight = 0, paddingTop = 0
                },
            };
            headerBar.Add(addButton);
            addButton.RegisterHoverEvents(value => addButton.style.color = value ? Color.cyan : Color.white);

            // Collection container
            collection = new()
            {
                name = "listof-collection",
                style =
                {
                    backgroundColor = .254902f.Gray(),
                    borderBottomColor = .1411765f.Gray(), borderRightColor = .1411765f.Gray(),
                    borderLeftColor = .1411765f.Gray(),borderTopColor = .1411765f.Gray(),
                    borderLeftWidth = 1, borderRightWidth = 1, borderBottomWidth = 1, borderTopWidth = 0,
                    borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                    flexDirection = FlexDirection.Column
                }
            };

            // Initialize foldout visual state
            UpdateFoldoutVisuals();
        }

        private void UpdateFoldoutVisuals()
        {
            bool expanded = foldoutState;
            if (rootProperty != null) expanded = rootProperty.isExpanded;

            collection.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            if (foldoutArrow != null) foldoutArrow.text = expanded ? "▾" : "▸";
        }

        protected virtual void ShowTypeChooser() => Polymorph.ShowChooseTypeMenu(baseType, false, TypeChosen);

        protected virtual void TypeChosen(Type chosen)
        {
            rootProperty.isExpanded = true;
#if UNITY_EDITOR
            try
            {
                if (listProperty == null) return;
                listProperty.serializedObject.Update();

                // Increase array size
                int newIndex = listProperty.arraySize;
                listProperty.arraySize++;
                listProperty.serializedObject.ApplyModifiedProperties();

                // Resolve the newly created element property
                listProperty.serializedObject.Update();
                if (newIndex < listProperty.arraySize)
                {
                    var newElem = listProperty.GetArrayElementAtIndex(newIndex);
                    if (newElem != null)
                    {
                        try
                        {
                            if (chosen != null)
                            {
                                if (newElem.propertyType == SerializedPropertyType.ManagedReference) newElem.managedReferenceValue = Activator.CreateInstance(chosen);
                                else try { newElem.managedReferenceValue = Activator.CreateInstance(chosen); } catch { }
                            }
                            else
                            {
                                if (newElem.propertyType == SerializedPropertyType.ManagedReference)
                                    newElem.managedReferenceValue = null;
                            }
                        }
                        catch { /* swallow instantiation errors */ }
                    }
                }

                listProperty.serializedObject.ApplyModifiedProperties();
                BuildItems();
            }
            catch { /* swallow editor-time exceptions */ }
#endif

        }

        // New: move item by delta (-1 up, +1 down) when wheel used on glyph
        void MoveItem(Item item, int delta)
        {
            if (listProperty == null) return;
            listProperty.serializedObject.Update();

            int i = itemElements.IndexOf(item);
            if (i < 0) return;

            int arraySize = listProperty.arraySize;
            if (arraySize <= 1) return;

            int newIndex = Mathf.Clamp(i + delta, 0, arraySize - 1);
            if (newIndex == i) return;

            try
            {
                listProperty.MoveArrayElement(i, newIndex);
                listProperty.serializedObject.ApplyModifiedProperties();
            }
            catch
            {
                // Swallow any editor-time exceptions and continue defensively.
                try { listProperty.serializedObject.ApplyModifiedProperties(); } catch { }
            }

            // Rebuild visuals to reflect new ordering.
            BuildItems();
        }


        void BuildItems()
        {
            itemElements.Clear();
            collection.Clear();
            counterLabel.text = (listProperty != null) ? listProperty.arraySize.ToString() : "0";

            if (listProperty == null) return;
            listProperty.serializedObject.Update();
            int size = listProperty.arraySize;

            for (int i = 0; i < size; i++)
            {
                Item item = new(listProperty.GetArrayElementAtIndex(i), RemoveItem, MoveItem);
                itemElements.Add(item);
                collection.Add(item);
                item.body.Bind(rootProperty.serializedObject);
            }

            // Ensure foldout visuals reflect current state after building items.
            UpdateFoldoutVisuals();
        }

        void RemoveItem(Item item)
        {
            if (listProperty == null) return;
            listProperty.serializedObject.Update();

            int i = itemElements.IndexOf(item);

            // Delete once; for object references Unity may leave a null placeholder
            listProperty.DeleteArrayElementAtIndex(i);

            // If after deletion there is still an element at that index and it's an object reference & null, delete again.
            if (i < listProperty.arraySize)
            {
                var maybeElem = listProperty.GetArrayElementAtIndex(i);
                if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
                {
                    listProperty.DeleteArrayElementAtIndex(i);
                }
            }

            // Update counter and apply changes
            if (counterLabel != null) counterLabel.text = listProperty.arraySize.ToString();
            listProperty.serializedObject.ApplyModifiedProperties();

            itemElements[i].parent.Remove(itemElements[i]);
            itemElements.RemoveAt(i);
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
            public Polymorph.HeaderDrawer body { get; private set; }
        }

    }

    [CustomPropertyDrawer(typeof(UniqueList<>), true)]
    public class UniqueListDrawer : ListOfDrawer
    {
        public UniqueListDrawer() : base() { }

        protected override void ShowTypeChooser()
        {
            GenericMenu menu = new();

            List<Type> types = GetSubtypes(baseType).ToList();

            for (int i = 0; i < listProperty.arraySize; i++)
            {
                var elem = listProperty.GetArrayElementAtIndex(i);
                if (elem != null && elem.managedReferenceValue != null) types.Remove(elem.managedReferenceValue.GetType());
            }

            if (types.Count != 0)
            {
                foreach (Type t in types)
                {
                    if (t == baseType) continue;
                    menu.AddItem(new GUIContent(t.Name), false, () => { TypeChosen(t); });
                }

                menu.ShowAsContext();
            }
        }

        protected override void TypeChosen(Type chosen)
        {
            // Prevent adding duplicate types to the list.
            if (chosen != null)
            {
                for (int i = 0; i < listProperty.arraySize; i++)
                {
                    var elem = listProperty.GetArrayElementAtIndex(i);
                    if (elem != null && elem.managedReferenceValue != null && elem.managedReferenceValue.GetType() == chosen)
                    {
                        EditorUtility.DisplayDialog("Duplicate Type", $"An instance of type '{chosen.Name}' already exists in the list. UniqueList cannot contain duplicates.", "OK");
                        return;
                    }
                }
            }
            // If no duplicates, proceed with normal addition.
            base.TypeChosen(chosen);
        }
#endif
    }
}