using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using _Polymorph_Helpers;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[System.Serializable]
public abstract class Polymorph
{
#if UNITY_EDITOR

    public virtual bool OverrideBody(VisualElement.Hierarchy container, SerializedProperty property) => false;

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
            typeButton ??= new Button(TypeButtonClick)
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
            if (!this.Contains(typeButton)) this.Add(typeButton);
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
            }

            TryCacheFoldout();
            this.QCache(out contentContainer, "unity-content");

            //Handle other hasInstance specific pieces.
            if (this.QCache(out toggle, className: "unity-foldout__checkmark"))
            {
                toggle.style.marginRight = 1;
                if (CurrentType == null) toggle.value = false;
            }

            if (this.QCache(out toggleArrow, "unity-checkmark")) toggleArrow.visible = CurrentType != null;

            if (property.managedReferenceValue is not null and Polymorph O && bodyInvalid)
            {
                if (O.OverrideBody(contentContainer.hierarchy, property))
                    contentContainer.Bind(property.serializedObject);

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
        Button typeButton;
        Toggle toggle;
        Foldout foldout;
        Label label;
        new VisualElement contentContainer;
        VisualElement toggleArrow;


        //Data
        SerializedProperty property;
        Type BaseType;
        Type CurrentType;
        bool bodyInvalid = true;


        public Action<Type> OnTypeChanged;
        public bool drawnSuccessfully { get; private set; } = false;

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
            //this.DelayedBuild(() =>
            //{
            //    for (int i = 0; i < tabs.Count; i++)
            //    {
            //        tabView.Add(tabs[i]);
            //    }
            //});
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

#endif

    [System.Serializable]
    public class ListOf<T> : UnityEngine.Object, IList<T> where T : Polymorph
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

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ListOf<>), true)]
    public class ListOfDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.name = $"ListOfDrawer-{property.propertyPath}";


            // Backing array property (robust resolution)
            SerializedProperty itemsProp = null;

            itemsProp = property.FindPropertyRelative("items");
            itemsProp ??= property.FindPropertyRelative("Items");


            if (itemsProp == null)
            {
                // If still null, throw a clearer editor-time exception so the developer can inspect the property path.
                throw new Exception($"Polymorph.ListOfDrawer: Could not resolve 'items' SerializedProperty for '{property.propertyPath}'.");
            }

            // Resolve element (generic) type from FieldInfo where possible
            Type elementType = null;
            try
            {
                var fi = fieldInfo;
                if (fi != null)
                {
                    var ft = fi.FieldType;
                    if (ft.IsGenericType)
                    {
                        var args = ft.GetGenericArguments();
                        if (args != null && args.Length > 0) elementType = args[0];
                    }
                }
            }
            catch
            {
                elementType = null;
            }

            // Header bar
            VisualElement headerBar = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 2
                }
            };

            var titleLabel = new Label(property.displayName)
            {
                name = "listof-title",
                style =
                {
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            headerBar.Add(titleLabel);

            var counterLabel = new Label((itemsProp != null) ? itemsProp.arraySize.ToString() : "0")
            {
                name = "listof-counter",
                style =
                {
                    width = 40,
                    unityTextAlign = TextAnchor.MiddleRight
                }
            };
            headerBar.Add(counterLabel);


            // Collection container
            var collection = new VisualElement()
            {
                name = "listof-collection",
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingLeft = 2,
                    paddingRight = 2
                }
            };

            var addButton = new Button(
                () =>
            {
                // When clicked, show type chooser and add the chosen type (or null choice) to the list.
                Type baseType = elementType ?? typeof(Polymorph);
                Polymorph.ShowChooseTypeMenu(baseType, false, (chosen) =>
                {
#if UNITY_EDITOR
                    try
                    {
                        if (itemsProp == null) return;
                        itemsProp.serializedObject.Update();

                        // Increase array size
                        int newIndex = itemsProp.arraySize;
                        itemsProp.arraySize++;
                        itemsProp.serializedObject.ApplyModifiedProperties();

                        // Resolve the newly created element property
                        itemsProp.serializedObject.Update();
                        if (newIndex < itemsProp.arraySize)
                        {
                            var newElem = itemsProp.GetArrayElementAtIndex(newIndex);
                            if (newElem != null)
                            {
                                try
                                {
                                    if (chosen != null)
                                    {
                                        // Try to set as managed reference if supported
                                        if (newElem.propertyType == SerializedPropertyType.ManagedReference)
                                        {
                                            newElem.managedReferenceValue = Activator.CreateInstance(chosen);
                                        }
                                        else
                                        {
                                            // Fallback: attempt managed reference assignment anyway (defensive)
                                            try { newElem.managedReferenceValue = Activator.CreateInstance(chosen); }
                                            catch { }
                                        }
                                    }
                                    else
                                    {
                                        // Null choice -> leave default (or null)
                                        if (newElem.propertyType == SerializedPropertyType.ManagedReference)
                                            newElem.managedReferenceValue = null;
                                    }
                                }
                                catch { /* swallow instantiation errors */ }
                            }
                        }

                        itemsProp.serializedObject.ApplyModifiedProperties();
                    }
                    catch { /* swallow editor-time exceptions */ }

                    // Rebuild UI to reflect change
                    Rebuild();
#endif
                });
            })
            {
                text = "+",
                name = "listof-add",
                style =
                {
                    width = 22,
                    height = 18
                }
            };
            headerBar.Add(addButton);

            root.Add(headerBar);

            root.Add(collection);

            // Local helper: rebuild UI from serialized property
            void Rebuild()
            {
                collection.Clear();
                counterLabel.text = (itemsProp != null) ? itemsProp.arraySize.ToString() : "0";

#if UNITY_EDITOR
                if (itemsProp == null) return;
                itemsProp.serializedObject.Update();
                int size = itemsProp.arraySize;

                for (int i = 0; i < size; i++)
                {
                    SerializedProperty elemProp = itemsProp.GetArrayElementAtIndex(i);
                    VisualElement row = new()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center,
                            marginBottom = 2
                        }
                    };

                    // Drag glyph (visual only)
                    var glyph = new Label("≡")
                    {
                        name = "listof-grab",
                        style =
                        {
                            width = 18,
                            unityTextAlign = TextAnchor.MiddleCenter,
                            marginRight = 4
                        }
                    };
                    row.Add(glyph);

                    // Body: Use Polymorph.HeaderDrawer for polymorph editing if the element represents a Polymorph-like object.
                    VisualElement body = null;
                    try
                    {
                        // If the element property is compatible with Polymorph.HeaderDrawer, use it.
                        // HeaderDrawer expects a SerializedProperty pointing at a Polymorph-like managed reference.
                        body = new Polymorph.HeaderDrawer(elemProp);
                        body.style.flexGrow = 1;
                    }
                    catch
                    {
                        // Fallback: use a default PropertyField
                        try
                        {
                            var pf = new PropertyField(elemProp);
                            pf.style.flexGrow = 1;
                            body = pf;
                        }
                        catch
                        {
                            body = new Label("(unable to draw element)");
                        }
                    }

                    // Bind the body defensively
                    try { body.Bind(property.serializedObject); } catch { }

                    row.Add(body);

                    // Remove button
                    var removeBtn = new Button(() =>
                    {
                        try
                        {
                            if (itemsProp == null) return;
                            itemsProp.serializedObject.Update();

                            // Delete once; for object references Unity may leave a null placeholder
                            itemsProp.DeleteArrayElementAtIndex(i);

                            // If after deletion there is still an element at that index and it's an object reference & null, delete again.
                            if (i < itemsProp.arraySize)
                            {
                                var maybeElem = itemsProp.GetArrayElementAtIndex(i);
                                if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
                                {
                                    itemsProp.DeleteArrayElementAtIndex(i);
                                }
                            }

                            // Update counter and apply changes
                            if (counterLabel != null) counterLabel.text = itemsProp.arraySize.ToString();
                            itemsProp.serializedObject.ApplyModifiedProperties();
                        }
                        catch { /* swallow */ }

                        // Rebuild UI after removal
                        Rebuild();
                    })
                    {
                        text = "-",
                        name = "listof-remove",
                        style =
                        {
                            width = 20,
                            marginLeft = 6
                        }
                    };
                    removeBtn.RegisterCallback<ClickEvent>((evt) => evt.StopPropagation());
                    row.Add(removeBtn);

                    collection.Add(row);
                }

                // Ensure foldout-like visibility: nothing special here
#endif
            }

            // Initial build
            Rebuild();

            // Bind root for prefab/apply support (defensive)
            try { root.Bind(property.serializedObject); } catch { }

            return root;
        }
#endif
    }
}

namespace _Polymorph_Helpers
{
    static class XtensionsPolymorph
    {
        public static void DelayedBuild(this VisualElement V, Action result)
            => V.RegisterCallbackOnce<AttachToPanelEvent>(_ => { V.schedule.Execute(result); });


        public static bool QCache<T>(this VisualElement V, out T result, string name = null, string className = null) where T : VisualElement
        {
            result = V.Q<T>(name, className) ?? null;
            return result != null;
        }
    }
}