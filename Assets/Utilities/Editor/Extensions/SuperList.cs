using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Utilities.Xtensions.VisualElements
{
    /// <summary>
    /// A <see cref="VisualElement"/> that displays and manages a completely-customizable List.<br/>
    /// When derived from, should be paired with a derived form of <see cref="SuperListItem{LIST, ITEM, VALUE}"/>
    /// </summary>
    /// <typeparam name="LIST">Concrete SuperList type.</typeparam>
    /// <typeparam name="ITEM">Concrete SuperListItem type.</typeparam>
    /// <typeparam name="VALUE">Underlying value type stored in the serialized array.</typeparam>
    public class SuperList<LIST, ITEM, VALUE> : VisualElement
        where LIST : SuperList<LIST, ITEM, VALUE>
        where ITEM : SuperListItem<LIST, ITEM, VALUE>
    {
        /// <summary>
        /// Creates a new SuperList bound to a SerializedProperty representing an array/list.
        /// </summary>
        /// <param name="listProperty">The serialized array property to represent.</param>
        /// <param name="HeaderOverride">Optional factory to provide a custom header instance.</param>
        public SuperList(SerializedProperty listProperty)
        {
            property = GetMainProperty(listProperty);

            header = HeaderDefinition;

            collectionBackground = new VisualElement().AddTo(this, b =>
            {
                b.name = "superlist-collection";
                b.style
                    .Colors(null, .254902f.Gray(), .1411765f.Gray())
                    .Border(1, top: 0)
                    .Radius(0, bottom: 4)
                    .Flex(FlexDirection.Column);
            });
            collectionBackground.style.display = listProperty.isExpanded ? DisplayStyle.Flex : DisplayStyle.None;

            BuildItems();
            Undo.undoRedoPerformed += BuildItems;

            this.Bind(listProperty.serializedObject);

            // Register polling to detect external changes (Reset, script changes, etc.)
            UpdateRegister = true;
            // Ensure we unregister when the element is removed from the panel
            this.RegisterCallback<DetachFromPanelEvent>((evt) => { UpdateRegister = false; });
        }

        /// <summary>
        /// The header VisualElement for this list (foldout, counter and add/remove actions).
        /// </summary>
        public Header header { get; private set; }

        /// <summary>
        /// Root container that holds the item elements for this list.
        /// </summary>
        public VisualElement collectionBackground { get; private set; }

        /// <summary>
        /// The overridable immediate method run to acquire the main property this list will use for everything.
        /// </summary>
        public virtual SerializedProperty GetMainProperty(SerializedProperty input) => input;

        /// <summary>
        /// The overridable immediate method run to create the <see cref="Header"/>. <br/>
        /// Override to disable Add/Remove buttons or disable editing the counter.
        /// </summary>
        public virtual Header HeaderDefinition => new Header(this as LIST).AddTo(this);

        #region Data
        /// <summary>
        /// The serialized property (array) that this list represents.
        /// </summary>
        public SerializedProperty property { get; private set; }
        /// <summary>
        /// The visual item holders currently displayed by the list. The index/order matches the serialized array.
        /// </summary>
        public List<ITEM> items { get; private set; } = new();
        /// <summary>
        /// Currently selected item in the list, or null when nothing is selected.
        /// </summary>
        public ITEM selectedItem { get; private set; }
        #endregion

        #region Virtuals

        /// <summary>
        /// (Re)builds the visual representation of the list from the current serialized array state.
        /// This will clear existing visuals and recreate item elements to match property.arraySize.
        /// </summary>
        public virtual void BuildItems()
        {
            if (property == null) return;

            Select(null);

            // Clear existing visuals
            collectionBackground?.Clear();
            items.Clear();

            for (int i = 0; i < CurrentSize; i++)
                CreateItemElement(i);
            UpdateCounterAndFoldout();

            // Ensure item visuals reflect the current serialized data
            UpdateItems();
        }

        /// <summary>
        /// Updates each visible item to match the current serialized data. This calls ITEM.Update for each item.
        /// </summary>
        public virtual void UpdateItems()
        {
            for (int i = 0; i < items.Count; i++)
                items[i].Update(property.GetArrayElementAtIndex(i));
        }

        #region Add Systems

        /// <summary>
        /// Handler invoked when the header add button is pressed. Adds a new array slot and creates an item visual.<br/>
        /// Override this for unique functionality when pressing the + button.
        /// </summary>
        protected virtual void AddButtonPressed()
        {
            CreatePropertySlot(out int newID);
            SetOrCreateItemValue(newID);
            CreateItemElement(newID);
            Select(items[newID]);
        }

        /// <summary>
        /// Increases the underlying serialized array size by one and returns the newly created index. <br/>
        /// (Note: Will not work if the primary property is not actually an array. Add unique functionality to replace this.)
        /// </summary>
        /// <param name="newID">Outputs the index of the newly allocated slot.</param>
        public virtual void CreatePropertySlot(out int newID)
        {
            if (property == null) throw new InvalidOperationException("Property is null");

            //property.serializedObject.Update();

            CurrentSize++;

            property.serializedObject.ApplyModifiedProperties();

            UpdateCounterAndFoldout();

            newID = property.arraySize - 1;
        }

        /// <summary>
        /// Sets a sensible default (or the provided input) into the serialized element at the specified index.
        /// Handles primitive, enum, object reference and managed reference property types.
        /// </summary>
        /// <param name="ID">Index in the serialized array to set.</param>
        /// <param name="input">Optional explicit value to assign. If null a default is created depending on property type.</param>
        public virtual void SetOrCreateItemValue(int ID, object input = null)
        {
            if (property == null) throw new InvalidOperationException("Property is null");
            //property.serializedObject.Update();
            SerializedProperty targetProperty = property.GetArrayElementAtIndex(ID) ?? throw new ArgumentOutOfRangeException(nameof(ID));

            // If input is null, provide a sensible default depending on the property type.
            if (input == null)
            {
                switch (targetProperty.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        targetProperty.intValue = 0;
                        break;
                    case SerializedPropertyType.Boolean:
                        targetProperty.boolValue = false;
                        break;
                    case SerializedPropertyType.Float:
                        targetProperty.floatValue = 0f;
                        break;
                    case SerializedPropertyType.String:
                        targetProperty.stringValue = string.Empty;
                        break;
                    case SerializedPropertyType.Enum:
                        targetProperty.intValue = 0;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        targetProperty.objectReferenceValue = null;
                        break;
                    case SerializedPropertyType.ManagedReference:
                        try { targetProperty.managedReferenceValue = Activator.CreateInstance(typeof(VALUE)); }
                        catch { targetProperty.managedReferenceValue = null; }
                        break;
                    default:
                        // Try managed reference as fallback
                        try { targetProperty.managedReferenceValue = Activator.CreateInstance(typeof(VALUE)); } catch { }
                        break;
                }
            }
            else
            {
                // Convert input to the appropriate underlying serialized value.
                try
                {
                    switch (targetProperty.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            targetProperty.intValue = Convert.ToInt32(input);
                            break;
                        case SerializedPropertyType.Boolean:
                            targetProperty.boolValue = Convert.ToBoolean(input);
                            break;
                        case SerializedPropertyType.Float:
                            targetProperty.floatValue = Convert.ToSingle(input);
                            break;
                        case SerializedPropertyType.String:
                            targetProperty.stringValue = Convert.ToString(input);
                            break;
                        case SerializedPropertyType.Enum:
                            // Enums are stored as intValue
                            targetProperty.intValue = Convert.ToInt32(input);
                            break;
                        case SerializedPropertyType.ObjectReference:
                            targetProperty.objectReferenceValue = input as UnityEngine.Object;
                            break;
                        case SerializedPropertyType.ManagedReference:
                            targetProperty.managedReferenceValue = input;
                            break;
                        default:
                            // best-effort fallback
                            try { targetProperty.managedReferenceValue = input; } catch { }
                            break;
                    }
                }
                catch
                {
                    // If conversion fails, attempt a safe fallback default
                    switch (targetProperty.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            targetProperty.intValue = 0;
                            break;
                        case SerializedPropertyType.Boolean:
                            targetProperty.boolValue = false;
                            break;
                        case SerializedPropertyType.Float:
                            targetProperty.floatValue = 0f;
                            break;
                        case SerializedPropertyType.String:
                            targetProperty.stringValue = string.Empty;
                            break;
                        default:
                            // leave as-is for object/managed references
                            break;
                    }
                }
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Creates and registers an ITEM visual for the element at the given index.
        /// The ITEM type must provide a constructor accepting (LIST parent, SerializedProperty property).
        /// </summary>
        /// <param name="ID">Index of the array element to create a visual for.</param>
        public virtual void CreateItemElement(int ID)
        {
            if (property == null) throw new InvalidOperationException("Property is null");
            // Grab a fresh serialized property for this slot
            SerializedProperty elemProp = property.GetArrayElementAtIndex(ID) ?? throw new ArgumentOutOfRangeException(nameof(ID));

            ITEM holder = Activator.CreateInstance(typeof(ITEM), this as LIST, elemProp) as ITEM;

            items.Add(holder);
            collectionBackground.Add(holder);

            // Bind the newly created element to the owner object so it displays immediately and reacts to changes.
            try { holder.Bind(property.serializedObject); } catch { }
        }

        #endregion

        #region Remove Systems

        /// <summary>
        /// Handler invoked when the header remove button is pressed. Removes the selected item (or last) from the array and UI. <br/>
        /// Override this for unique functionality when pressing the - button.
        /// </summary>
        protected virtual void RemoveButtonPressed()
        {
            if (property == null) return;
            if (CurrentSize == 0) return;

            ITEM selected = selectedItem ?? items[^1];
            int id = items.IndexOf(selected);

            Select(null);
            DeletePropertySlotAt(id);
            RemoveItemElement(selected);
            UpdateItems();
        }

        /// <summary>
        /// Deletes the serialized array element at the provided index. Handles Unity's two-step deletion for object references.
        /// (Note: Will not work if the primary property is not actually an array. Add unique functionality to replace this.)
        /// </summary>
        /// <param name="index">Index of the element to delete.</param>
        public virtual void DeletePropertySlotAt(int index)
        {
            if (property == null) return;
            property.serializedObject.Update();

            // Delete once. For object reference slots Unity may leave a null placeholder and require a second delete call.
            property.DeleteArrayElementAtIndex(index);

            // If the array still has an element at this index and it's an object reference that is null,
            // delete it again to fully remove the slot.
            if (index < property.arraySize)
            {
                SerializedProperty maybeElem = property.GetArrayElementAtIndex(index);
                if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
                {
                    property.DeleteArrayElementAtIndex(index);
                }
            }

            // Keep UI counter accurate
            CurrentSize = property.arraySize;
            property.serializedObject.ApplyModifiedProperties();

            UpdateCounterAndFoldout();
        }

        /// <summary>
        /// Removes the ITEM visual from the list UI and internal collection.
        /// </summary>
        /// <param name="I">The item instance to remove.</param>
        public virtual void RemoveItemElement(ITEM I)
        {
            if (items == null || I == null) return;

            items.Remove(I);
            collectionBackground?.Remove(I);
        }

        #endregion

        /// <summary>
        /// Gets or sets the array size (property.arraySize). Setting adjusts the underlying serialized array size.
        /// </summary>
        public virtual int CurrentSize
        {
            get => property.arraySize;
            set
            {
                if (value > property.arraySize) header.FoldoutArrow.Expanded = true;
                //property.serializedObject.Update();
                property.arraySize = value;
                // Do not ApplyModifiedProperties here — callers should apply as needed, but keep UI in sync
            }
        }

        /// <summary>
        /// Overridable source from which to get whether the list is expanded or not.
        /// </summary>
        public virtual bool expandedSource
        {
            get => property.isExpanded;
            set => property.isExpanded = value;
        }

        /// <summary>
        /// Called when the header counter value is changed manually by the user. Resizes the list to the requested value and rebuilds visuals.
        /// </summary>
        /// <param name="newValue">The new requested size value.</param>
        public virtual void OnCounterTouched(int newValue)
        {
            CurrentSize = newValue;
            BuildItems();
        }

        /// <summary>
        /// Updates the header counter control and the foldout 'expandable' state to reflect the current array size.
        /// </summary>
        public virtual void UpdateCounterAndFoldout()
        {
            if (header != null && property != null)
            {
                try { header.Counter.SetValueWithoutNotify(property.arraySize); } catch { }
                try { header.FoldoutArrow.Expandable = property.arraySize > 0; } catch { }
            }
        }

        /// <summary>
        /// Moves the specified item in the underlying serialized array by delta positions (negative moves up).
        /// </summary>
        /// <param name="item">Item instance to move.</param>
        /// <param name="delta">Relative movement (-1, +1 etc.).</param>
        public void MoveItem(ITEM item, int delta)
        {
            if (property == null) return;
            //property.serializedObject.Update();

            int i = items.IndexOf(item);
            if (i < 0) return;

            int arraySize = CurrentSize;
            if (arraySize <= 1) return;

            int newIndex = Mathf.Clamp(i + delta, 0, arraySize - 1);
            if (newIndex == i) return;

            try
            {
                property.MoveArrayElement(i, newIndex);
                property.serializedObject.ApplyModifiedProperties();
            }
            catch
            {

            }

            // Rebuild visuals to reflect new ordering.
            BuildItems();
        }

        #region Editor Registration (Not sure why I felt the need to add this.)
        /// <summary>
        /// When set to true the list registers to EditorApplication.update to poll for external changes (undo, reset).
        /// </summary>
        public bool UpdateRegister
        {
            get => _updateRegistered;
            set
            {
                if (value == _updateRegistered) return;
                _updateRegistered = value;
                if (value) EditorApplication.update += EditorUpdate;
                else EditorApplication.update -= EditorUpdate;
            }
        }
        bool _updateRegistered = false;

        /// <summary>
        /// Editor polling callback used to detect external modifications to the serialized array and rebuild/refresh visuals.
        /// Override to change polling behaviour.
        /// </summary>
        protected virtual void EditorUpdate()
        {
            //if (property == null) return;
            //try
            //{
            //    property.serializedObject.Update();
            //    int size = property.arraySize;
            //    items ??= new List<ItemHolder>();
            //    if (size != items.Count)
            //    {
            //        // External change detected (Reset, undo, etc.) -> rebuild UI to match serialized data
            //        BuildItems();
            //    }
            //    else
            //    {
            //        // Keep UI synced: counter/foldout and update each item in-place
            //        UpdateCounterAndFoldout();
            //        UpdateItems();
            //    }
            //}
            //catch
            //{
            //    // swallow exceptions to avoid EditorApplication update throwing
            //}

        }
        #endregion

        #region Context Menu

        /// <summary>
        /// Establishes context menu entries for the list header. Override to add custom context items.
        /// </summary>
        /// <param name="menu">The GenericMenu instance to populate.</param>
        protected virtual void EstablishContextMenu(GenericMenu menu) => menu.AddItem(new("Clear"), false, ClearCalled);

        /// <summary>
        /// Clears the underlying serialized array and removes all visuals from the UI.
        /// </summary>
        protected virtual void ClearCalled()
        {
            if (property != null)
            {
                property.serializedObject.Update();
                property.arraySize = 0;
                property.serializedObject.ApplyModifiedProperties();
            }

            if (items != null)
            {
                foreach (var el in items)
                {
                    collectionBackground.Remove(el);
                }
                items.Clear();
            }
            CurrentSize = 0;
            UpdateCounterAndFoldout();
        }

        #endregion


        #endregion

        /// <summary>
        /// Selects the given item instance and toggles the Selected state on the previously selected item.
        /// </summary>
        /// <param name="E">Item to select (or null to clear selection).</param>
        public void Select(ITEM E)
        {
            if (selectedItem != null) selectedItem.Selected = false;
            selectedItem = E;
            if (selectedItem != null) selectedItem.Selected = true;
        }

        /// <summary>
        /// Visual header for a SuperList instance. Contains foldout, counter and add/remove buttons.
        /// </summary>
        public class Header : VisualElement
        {
            /// <summary>
            /// Creates a header bound to the provided SuperList instance.
            /// </summary>
            /// <param name="Parent">Parent SuperList instance for callbacks and state.</param>
            /// <param name="showAddbutton">Whether to show the add (+) button.</param>
            /// <param name="showDeleteButton">Whether to show the delete (-) button.</param>
            /// <param name="disableCounter">Whether to disable the size counter field.</param>
            public Header(LIST Parent, bool showAddbutton = true, bool showDeleteButton = true, bool disableCounter = false)
            {
                parent = Parent;

                //Self
                {
                    name = "listof-headerbar";
                    style.flexDirection = FlexDirection.Row;
                    style.alignItems = Align.Center;
                    style.height = 20;
                    style.backgroundColor = .2078432f.Gray();
                    style.borderRightColor = .1411765f.Gray();
                    style.borderLeftColor = .1411765f.Gray();
                    style.borderTopColor = .1411765f.Gray();
                    style.borderBottomColor = .1411765f.Gray();
                    style.borderRightWidth = 1;
                    style.borderLeftWidth = 1;
                    style.borderTopWidth = 1;
                    style.borderBottomWidth = 1;
                    style.paddingLeft = 4;
                    style.borderTopLeftRadius = 6;
                    style.borderTopRightRadius = 6;
                    style.justifyContent = Justify.SpaceBetween;
                }

                FoldoutArrow = new FoldoutArrow(Clicked, Parent.expandedSource).AddTo(this, f =>
                {
                    f.style.alignSelf = Align.FlexStart;
                });
                if (Parent.CurrentSize == 0) FoldoutArrow.Expandable = false;
                else { FoldoutArrow.Expanded = Parent.expandedSource; }

                Label = new Label().AddTo(this, l =>
                {
                    l.text = Parent.property.displayName;
                    l.style.alignSelf = Align.FlexStart;
                    l.style.unityTextAlign = TextAnchor.MiddleLeft;
                    l.style.flexGrow = 1f;
                    l.style.height = 15;
                    new Highlighter(l, Color.cadetBlue).ApplySelect();
                    ContextMenu = new();
                    Parent.EstablishContextMenu(ContextMenu);
                    l.RegisterCallback<ContextClickEvent>(ContextClick);

                    void ContextClick(ContextClickEvent ev)
                    {
                        ev.StopPropagation();
                        ContextMenu.ShowAsContext();
                    }
                });

                Counter = new IntegerField().AddTo(this, c =>
                {
                    c.name = "superlist-counter";
                    c.style
                        .Align(alignSelf: Align.FlexEnd)
                        .FixedSize(width: 36)
                        .Text(null, TextAnchor.MiddleRight)
                        .Colors(.85f.Gray(), Color.clear)
                        .Margins(right: 6)
                        .BorderNull();
                    if (c.QCache(out VisualElement b, className: "unity-base-text-field__input"))
                    {
                        b.style.unityTextAlign = TextAnchor.MiddleRight;
                        b.style.backgroundColor = Color.clear;
                        b.style.borderTopColor = Color.clear;
                        b.style.borderBottomColor = Color.clear;
                        b.style.borderLeftColor = Color.clear;
                        b.style.borderRightColor = Color.clear;
                    }
                    if (disableCounter) c.SetEnabled(false);
                    c.RegisterValueChangedCallback(ev => parent.OnCounterTouched(ev.newValue));
                });
                Add(Counter);

                if (showAddbutton)
                {
                    AddButton = new Button(Parent.AddButtonPressed).AddTo(this, a =>
                    {
                        a.text = "+";
                        a.name = "superlist-add";
                        a.style
                            .Align(alignSelf: Align.FlexEnd)
                            .FixedSize(20, 18)
                            .Colors(null, Color.clear, Color.clear)
                            .Text(20, align: TextAnchor.MiddleCenter)
                            .Border(0)
                            .Radius(0, topLeft: 6)
                            .Margins(0)
                            .Padding(0);
                        new Highlighter(a, Color.lightGreen, Color.gray3).ApplyHover();

                        a.Highlighter(Color.lightGreen, Color.gray4);
                    });
                }

                if (showDeleteButton)
                {
                    DeleteButton = new Button(Parent.RemoveButtonPressed).AddTo(this, d =>
                    {
                        d.text = "-";
                        d.name = "superlist-remove";
                        d.style
                            .Align(alignSelf: Align.FlexEnd)
                            .FixedSize(20, 18)
                            .Text(20, align: TextAnchor.MiddleCenter)
                            .Colors(null, Color.clear, Color.clear)
                            .Text(14, TextAnchor.LowerCenter)
                            .Border(0)
                            .Radius(0, topRight: 6)
                            .Margins(0)
                            .Padding(0);
                        new Highlighter(d, Color.darkSalmon, Color.gray3).ApplyHover();
                    });
                }
            }

            /// <summary>
            /// The SuperList instance that owns this header.
            /// </summary>
            new public LIST parent { get; private set; }
            /// <summary>
            /// Visual foldout arrow control used to expand/collapse the list contents.
            /// </summary>
            public FoldoutArrow FoldoutArrow { get; private set; }
            /// <summary>
            /// Display label for the list (usually the serialized property's display name).
            /// </summary>
            public Label Label { get; private set; }
            /// <summary>
            /// Add (+) button instance. May be null when the header was constructed without an add control.
            /// </summary>
            public Button AddButton { get; private set; }
            /// <summary>
            /// Delete (-) button instance. May be null when the header was constructed without a delete control.
            /// </summary>
            public Button DeleteButton { get; private set; }
            /// <summary>
            /// Integer field used to view and change the list size directly.
            /// </summary>
            public IntegerField Counter { get; private set; }
            /// <summary>
            /// Context menu instance used by the header label (right-click menu).
            /// </summary>
            public GenericMenu ContextMenu { get; private set; }

            /// <summary>
            /// Internal callback invoked when the foldout arrow is clicked.
            /// </summary>
            /// <param name="value">True when expanded, false when collapsed.</param>
            void Clicked(bool value)
            {
                if (parent == null || parent.collectionBackground == null) return;
                parent.collectionBackground.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

    }
    /// <summary>
    /// A <see cref="VisualElement"/> that displays the individual items in a <see cref="SuperList{LIST, ITEM, VALUE}"/>.<br/>
    /// When derived from, should be paired with a derived form of <see cref="SuperList{LIST, ITEM, VALUE}"/>
    /// </summary>
    /// <typeparam name="LIST">Concrete SuperList type.</typeparam>
    /// <typeparam name="ITEM">Concrete SuperListItem type.</typeparam>
    /// <typeparam name="VALUE">Underlying value type stored in the serialized array.</typeparam>
    public class SuperListItem<LIST, ITEM, VALUE> : VisualElement
        where LIST : SuperList<LIST, ITEM, VALUE>
        where ITEM : SuperListItem<LIST, ITEM, VALUE>
    {
        public SuperListItem(LIST parentList, SerializedProperty thisProperty)
        {
            this.parentList = parentList;

            // Background container is the Item root itself
            name = "superlist-item";

            style.Flex(FlexDirection.Row, 1).Align(Align.Center, Justify.FlexStart).Border(vertical: .5f)
                .Colors(null, Color.clear, new(0, 0, 0, .1f)).Radius(4);

            style.flexGrow = 1;
            style.minHeight = 18;

            dragHandle = new VisualElement().AddTo(this, h =>
            {
                h.name = "superlist-item-grab-symbol";

                h.style
                    .FixedSize(width: 18)
                    .Align(null, null, Align.Stretch)
                    .Flex(shrink: 0)
                    .Margins(left: 2, right: 2)
                    .Colors(null, Color.clear, Color.clear)
                    .Border(0)
                    .Padding(0);

                h.style.justifyContent = Justify.Center;
                h.style.alignItems = Align.Center;

                h.style.position = Position.Relative;

                h.focusable = true;

                // Inner glyph label (purely visual)
                var glyph = new Label("≡") { name = "superlist-item-grab-glyph" };
                glyph.style
                    .Text(null, TextAnchor.MiddleCenter)
                    .Align(null, null, Align.Center)
                    .FixedSize(width: 16);
                glyph.style.flexGrow = 0;
                glyph.style.maxWidth = 16;
                glyph.style.marginTop = 0;
                glyph.style.marginBottom = 0;

                h.Add(glyph);

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
                            parentList.MoveItem(this as ITEM, delta);
                        }
                        catch { /* defensive: swallow */ }
                        evt.StopPropagation();
                    }
                });
            });

            //Register PointerDownEvent that allows trickledown so that tapping anywhere on the Item will select it. :)
            RegisterCallback<PointerDownEvent>((evt) =>
            {
                parentList.Select(this as ITEM);
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            Update(thisProperty);
        }

        public LIST parentList { get; protected set; }
        public SerializedProperty property { get; protected set; }
        public VisualElement dragHandle { get; protected set; }
        public VisualElement content { get; protected set; }

        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                UpdateBackground();
            }
        }
        bool _selected;
        public bool Invalid
        {
            get => _invalid;
            set
            {
                _invalid = value;
                UpdateBackground();
            }
        }
        bool _invalid;
        private void UpdateBackground() => style.backgroundColor =
                _selected ? _invalid ? new Color(.9f, .6f, .6f) : Color.gray3
                : _invalid ? new Color(.9f, .3f, .3f) : Color.clear;

        public void Update(SerializedProperty newprop)
        {
            if (newprop == null) return;
            property = newprop;
            if (content != null) this.Remove(content);
            content = Content();
            this.Add(content);
        }

        public virtual VisualElement Content()
        {
            PropertyField result = new(property);
            result.style.flexGrow = 1f;
            result.style.marginRight = 4;
            result.Bind(property.serializedObject);
            return result;
        }
    }

    /// <summary>
    /// A basic example of the highly customizable <see cref="SuperList{LIST, ITEM, VALUE}"/> made for basic objects.
    /// </summary>
    /// <typeparam name="T">The type this list will hold.</typeparam>
    public class SuperList<T> : SuperList<SuperList<T>, SuperListItem<T>, T>
    {
        public SuperList(SerializedProperty listProperty, Func<Header> HeaderOverride = null) : base(listProperty) { }
    }
    /// <summary>
    /// A basic example of the highly customizable <see cref="SuperListItem{LIST, ITEM, VALUE}{LIST, ITEM, VALUE}"/> made for basic objects.
    /// </summary>
    /// <typeparam name="T">The type this list will hold.</typeparam>
    public class SuperListItem<T> : SuperListItem<SuperList<T>, SuperListItem<T>, T>
    {
        public SuperListItem(SuperList<T> parentList, SerializedProperty thisProperty) : base(parentList, thisProperty) { }
    }
}