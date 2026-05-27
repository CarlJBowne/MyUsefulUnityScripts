using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ListUtilities.Editor.Internal;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using UnityEngine.Windows;

namespace ListUtilities.Editor
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
        #region Initialization
        /// <summary>
        /// Creates a new SuperList bound to a SerializedProperty representing an array/list. <br/>
        /// Derived types may call the protected constructor with Override = true to replace initialization with their own. <br/>
        /// In Derived constructors, call BuildBasicElements(), and any BindProperty() function that establishes the Data sources and ends with FinishBind().
        /// </summary>
        /// <param name="rootProperty">The serialized array property to represent.</param>
        /// <param name="HeaderOverride">Optional factory to provide a custom header instance.</param>
        protected SuperList(SerializedProperty rootProperty, bool Override = true)
        {
            if (Override) return;
            BuildBasicElements();
            BindProperty(rootProperty);
        }

        /// <summary>
        /// Builds the minimal visual structure for the list (header and collection container).
        /// This does not bind any serialized properties; call <see cref="Header.Bind"/> during BindProperty() to attach data.
        /// </summary>
        protected void BuildBasicElements()
        {
            header = new Header(this as LIST).AddTo(this);

            collectionBackground = new VisualElement().AddTo(this, b =>
            {
                b.name = "superlist-collection";
                b.style
                    .Colors(null, .254902f.Gray(), .1411765f.Gray())
                    .Border(1, top: 0)
                    .Radius(0, bottom: 4)
                    .Flex(FlexDirection.Column)
                    .Display(false);
            });
        }
        /// <summary>
        /// The header VisualElement for this list (foldout, counter and add/remove actions).
        /// </summary>
        public Header header { get; protected set; }
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
            public Header(LIST Parent)
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

                Foldout = new Foldout()
                {
                    text = "Placeholder",
                    value = false,
                    style =
                    {
                        flexGrow = 1f
                    }
                }.AddTo(this, F =>
                {
                    F.DelayedBuild((() =>
                    {
                        F.RegisterCallback<ContextualMenuPopulateEvent>(Parent.EstablishContextMenu);

                        F.RegisterValueChangedCallback(ButtonClicked);

                        Toggle = F.Q<Toggle>(null, Foldout.toggleUssClassName);
                        if (Toggle != null) Toggle.style.marginLeft = 0;

                        Label = F.Q<Label>(null, "unity-label");

                        FoldoutArrow = F.Q<VisualElement>(null, "unity-foldout__checkmark");
                        UpdateCounter();
                    }));
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
                    if (!Parent.allowCounterEdit) c.SetEnabled(false);
                    c.RegisterValueChangedCallback(ev => parent.OnCounterTouched(ev.newValue));
                });
                Add(Counter);

                if (Parent.allowAdd)
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
                        new ElementHighlighter(a, Color.lightGreen, Color.gray3).Hover();
                        new ElementHighlighter(a, Color.white, Color.lightGreen).Click();
                    });
                }

                if (Parent.allowDelete)
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
                        new ElementHighlighter(d, Color.darkSalmon, Color.gray3).Hover();
                        new ElementHighlighter(d, Color.white, Color.darkSalmon).Click();
                    });
                }
            }

            /// <summary>
            /// The SuperList instance that owns this header.
            /// </summary>
            new public LIST parent { get; protected set; }
            /// <summary>
            /// The Visual Foldout Parent used to hook into the Prefab System.
            /// </summary>
            public Foldout Foldout { get; protected set; }
            /// <summary>
            /// The Visual Toggle generated from the Foldout.
            /// </summary>
            public Toggle Toggle { get; protected set; }
            /// <summary>
            /// Visual foldout arrow control generated from the Foldout.
            /// </summary>
            public VisualElement FoldoutArrow { get; protected set; }
            /// <summary>
            /// Display label for the list generated from the Foldout.
            /// </summary>
            public Label Label { get; protected set; }
            /// <summary>
            /// Add (+) button instance. May be null when the header was constructed without an add control.
            /// </summary>
            public Button AddButton { get; protected set; }
            /// <summary>
            /// Delete (-) button instance. May be null when the header was constructed without a delete control.
            /// </summary>
            public Button DeleteButton { get; protected set; }
            /// <summary>
            /// Integer field used to view and change the list size directly.
            /// </summary>
            public IntegerField Counter { get; protected set; }

            /// <summary>
            /// Bind the header to a foldout serialized property and set its display title.
            /// </summary>
            /// <param name="title">Display title for the header.</param>
            /// <param name="FoldoutBinder">SerializedProperty representing the foldout binding.</param>
            public void Bind(string title, SerializedProperty FoldoutBinder)
            {
                Foldout.text = title;
                Foldout.BindProperty(FoldoutBinder);
                UpdateExpanded(parent.Expanded);
            }

            /// <summary>
            /// Convenience overload that binds using the property's display name.
            /// </summary>
            /// <param name="bindinstantly">SerializedProperty to bind to.</param>
            public void Bind(SerializedProperty bindinstantly) => Bind(bindinstantly.displayName, bindinstantly);

            /// <summary>
            /// Update the counter visual and visility of the Foldout Arrow.
            /// </summary>
            /// <param name="expand">When true and there are items, expand the list.</param>
            public void UpdateCounter(bool expand = false)
            {
                Counter.SetValueWithoutNotify(parent.CurrentSize);
                if (parent.CurrentSize > 0)
                {
                    if (FoldoutArrow != null && !FoldoutArrow.visible) FoldoutArrow.visible = true;
                    if (expand) parent.Expanded = true;
                }
                else
                {
                    UpdateExpanded(false);
                    if (FoldoutArrow != null) FoldoutArrow.visible = false;
                }
            }

            /// <summary>
            /// Callback for foldout value changes initiated by the user.
            /// </summary>
            /// <param name="ev">Change event for the foldout toggle.</param>
            private void ButtonClicked(ChangeEvent<bool> ev)
            {
                if (ev.newValue == ev.previousValue ||
                                parent == null ||
                                parent.collectionBackground == null ||
                                !FoldoutArrow.visible) return;
                parent.Expanded = ev.newValue;
            }

            /// <summary>
            /// Updates the visual expanded state of the header and collection container without firing change callbacks.
            /// </summary>
            /// <param name="value">Desired expanded state.</param>
            public void UpdateExpanded(bool value)
            {
                Foldout.SetValueWithoutNotify(value);
                parent.collectionBackground.Display(value);
            }
        }
        /// <summary>
        /// Basic Binding functionality. Should be overridden by a new signature if necessary to process any data sources passed in in the derived constructor. <br/>
        /// Derivation Guide: Establish necessary Data sources, then call header.Bind, then FinishBind().
        /// </summary>
        /// <param name="listProperty">SerializedProperty representing the array this list will display.</param>
        public void BindProperty(SerializedProperty listProperty)
        {
            property = listProperty;
            this.Bind(property.serializedObject);
            header.Bind(listProperty);
            FinishBind();
        }

        /// <summary>
        /// Finalize binding: build items and register undo/redo handlers.
        /// </summary>
        protected void FinishBind()
        {
            BuildItems();
            Undo.undoRedoPerformed += BuildItems;
        }

        /// <summary>
        /// Root container that holds the item elements for this list.
        /// </summary>
        public VisualElement collectionBackground { get; protected set; }

        #endregion


        #region Data
        /// <summary>
        /// The serialized property (array) that this list represents.
        /// </summary>
        public SerializedProperty property { get; protected set; }
        /// <summary>
        /// The visual item holders currently displayed by the list. The index/order matches the serialized array.
        /// </summary>
        public List<ITEM> items { get; protected set; } = new();
        /// <summary>
        /// Currently selected item in the list, or null when nothing is selected.
        /// </summary>
        public ITEM selectedItem { get; protected set; }

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

            CurrentSize++;

            property.serializedObject.ApplyModifiedProperties();

            newID = CurrentSize - 1;
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
        /// Duplicate the serialized array element at the given index. Rebuilds visuals and refreshes prefab markers.
        /// </summary>
        /// <param name="index">Index to duplicate.</param>
        public virtual void DuplicatePropertySlotAt(int index)
        {
            if (property == null) return;

            try
            {
                // Insert a new element after the current index so the duplicated element appears after the original.
                int insertAt = Mathf.Clamp(index + 1, 0, property.arraySize);
                property.InsertArrayElementAtIndex(insertAt);

                // Attempt to copy common field types from the original element into the newly inserted slot.
                var src = property.GetArrayElementAtIndex(index);
                var dst = property.GetArrayElementAtIndex(insertAt);
                if (src != null && dst != null)
                {
                    try
                    {
                        switch (src.propertyType)
                        {
                            case SerializedPropertyType.Integer:
                                dst.intValue = src.intValue;
                                break;
                            case SerializedPropertyType.Boolean:
                                dst.boolValue = src.boolValue;
                                break;
                            case SerializedPropertyType.Float:
                                dst.floatValue = src.floatValue;
                                break;
                            case SerializedPropertyType.String:
                                dst.stringValue = src.stringValue;
                                break;
                            case SerializedPropertyType.Enum:
                                dst.intValue = src.intValue;
                                break;
                            case SerializedPropertyType.ObjectReference:
                                dst.objectReferenceValue = src.objectReferenceValue;
                                break;
                            case SerializedPropertyType.ManagedReference:
                                try { dst.managedReferenceValue = DeepClone(src.managedReferenceValue); } catch { }
                                break;
                            default:
                                // For other/complex types rely on Unity's insertion behavior.
                                break;
                        }
                    }
                    catch { }
                }

                static object DeepClone(object obj)
                {
                    using var ms = new MemoryStream();
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(ms, obj);
                    ms.Position = 0;
                    return formatter.Deserialize(ms);
                }

                property.serializedObject.ApplyModifiedProperties();
                BuildItems();
                //RemoveItemElement(items[index]);
                //CreateItemElement(index);
                //CreateItemElement(insertAt);
            }
            catch { }



            // Rebuild UI and try to refresh prefab markers
            BuildItems();
            header.UpdateCounter();
            TryForceRefreshPrefabMarkers();
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
            BuildItems();
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
        }

        /// <summary>
        /// Removes the ITEM visual from the list UI and internal collection. <br/>
        /// Functionally pointless because it just Builds Items anyway. <br/>
        /// Maybe make use of this later if I can be arsed to figure out how to properly update things without destroying them.
        /// </summary>
        /// <param name="I">The item instance to remove.</param>
        [System.Obsolete]
        public virtual void RemoveItemElement(ITEM I)
        {
            if (items == null || I == null) return;

            items.Remove(I);
            collectionBackground?.Remove(I);
            BuildItems();
        }

        #endregion

        public virtual bool Expanded
        {
            get => property.isExpanded;
            set
            {
                property.isExpanded = value;
                header.UpdateExpanded(value);
            }
        }

        /// <summary>
        /// Gets or sets the array size (property.arraySize). Setting adjusts the underlying serialized array size.
        /// </summary>
        public virtual int CurrentSize
        {
            get => property.arraySize;
            set
            {
                bool nowBigger = value > property.arraySize;
                property.arraySize = value;
                header.UpdateCounter(nowBigger);

                // Do not ApplyModifiedProperties here — callers should apply as needed, but keep UI in sync
            }
        }

        /// <summary>
        /// Overridable definition of whether this allows reordering.
        /// </summary>
        public virtual bool allowReorder => true;
        public virtual bool allowAdd => true;
        public virtual bool allowDelete => true;
        public virtual bool allowCounterEdit => true;

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
        /// Moves the specified item in the underlying serialized array by delta positions (negative moves up).
        /// </summary>
        /// <param name="item">Item instance to move.</param>
        /// <param name="delta">Relative movement (-1, +1 etc.).</param>
        public void MoveItem(ITEM item, int delta)
        {
            if (!allowReorder) return;
            if (property == null) return;

            bool wasSelected = item == selectedItem;

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

            Select(items[newIndex]);

            //Tried to move the mouse to the new position but it wouldn't work.
            //Vector2 pos = EditorWindow.focusedWindow.position.position + selectedItem.dragHandle.worldBound.position 
            //    + new Vector2(7,7);
            //User32.SetCursorPos((int)pos.x, (int)pos.y);
        }

        #region Context Menu

        /// <summary>
        /// Establishes context menu entries for the list header. Override to add custom context items.
        /// </summary>
        /// <param name="menu">The GenericMenu instance to populate.</param>
        protected virtual void EstablishContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.InsertAction(0, "Clear", ClearContextMenu);
            var list = evt.menu.MenuItems();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not DropdownMenuAction iAction) continue;

                if (iAction.name.StartsWith("Apply to Prefab")) list[i] = new DropdownMenuAction(iAction.name, T => ApplyOrRevertContextMenu(iAction), DropDownMenuStatus);
                if (iAction.name.StartsWith("Revert"))
                {
                    list[i] = new DropdownMenuAction(iAction.name, T => ApplyOrRevertContextMenu(iAction), DropDownMenuStatus);
                    if (iAction.name.StartsWith("Revert (identical value to Prefab")) (list[i] as DropdownMenuAction).Execute();
                }
            }
        }

        /// <summary>
        /// Clears the underlying serialized array and removes all visuals from the UI.
        /// </summary>
        protected virtual void ClearContextMenu(DropdownMenuAction C)
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
        }
        /// <summary>
        /// Applies or Reverts Prefab changes before forcing an update.
        /// </summary>
        /// <param name="Def"></param>
        protected virtual void ApplyOrRevertContextMenu(DropdownMenuAction Def)
        {
            Def.Execute();
            property.serializedObject.Update();
            BuildItems();
            header.UpdateCounter();
            TryForceRefreshPrefabMarkers();
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
        /// Creates and registers an ITEM visual for the element at the given index.
        /// The ITEM type must Acquire its information in the overridden BindProperty function based on the ID given.
        /// </summary>
        /// <param name="ID">Index of the array element to create a visual for.</param>
        public void CreateItemElement(int ID)
        {
            ITEM holder = Activator.CreateInstance(typeof(ITEM), this as LIST, ID) as ITEM;

            items.Add(holder);
            collectionBackground.Add(holder);
        }

        /// <summary>
        /// Attempt to refresh the prefab markers that may be on this list.
        /// </summary>
        public void TryForceRefreshPrefabMarkers()
        {
#if UNITY_EDITOR
            try
            {
                var target = property?.serializedObject?.targetObject;
                if (target != null) EditorUtility.SetDirty(target);

                try
                {
#if UNITY_EDITOR
                    // Ensure Unity records instance property modifications so prefab override markers update
                    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(target);
#endif
                }
                catch { }

                // Repaint inspector(s) and hierarchy to prompt Unity to refresh override markers
                EditorApplication.RepaintHierarchyWindow();
                var iwType = Type.GetType("UnityEditor.InspectorWindow, UnityEditor");
                if (iwType != null)
                {
                    var wins = UnityEngine.Resources.FindObjectsOfTypeAll(iwType) as UnityEditor.EditorWindow[];
                }
                // Best-effort call to repaint all editor windows
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
            catch { }
#endif
        }


        public static DropdownMenuAction.Status DropDownMenuStatus(DropdownMenuAction A) => DropdownMenuAction.Status.Normal;
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
        #region Initialization
        /// <summary>
        /// Unlike the SuperList itself, it is not recommended to override the constructor heavily. Instead, simply override BindProperty() and get all the data sources from the SuperList.
        /// </summary>
        /// <param name="parentList"></param>
        /// <param name="Index"></param>
        protected SuperListItem(LIST parentList, int Index)
        {
            this.parent = parentList;
            this.Index = Index;
            BuildBasicElements();
            BindProperty();
        }
        protected void BuildBasicElements()
        {
            this.parent = parent;

            name = "superlist-item";
            style.Flex(FlexDirection.Row, 1).Align(Align.Center, Justify.FlexStart).Border(vertical: .5f).Colors(null, Color.clear, new(0, 0, 0, .1f)).Radius(4).MinMaxSize(minHeight: 18);

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
                    .FixedSize(width: 16)
                    .Flex(grow: 0)
                    .Margins(vertical: 0)
                    .Colors(Color.gray5);

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
                            parent.MoveItem(this as ITEM, delta);
                        }
                        catch { /* defensive: swallow */ }
                        evt.StopPropagation();
                    }
                });
            });

            //Register PointerDownEvent that allows trickledown so that tapping anywhere on the Item will select it. :)
            RegisterCallback<PointerDownEvent>((evt) => parent.Select(this as ITEM), TrickleDown.TrickleDown);
        }
        /// <summary>
        /// Unlike the SuperList itself, it is not recommended to override the constructor heavily. Instead, simply override this function and get all the data sources from the SuperList.
        /// </summary>
        protected virtual void BindProperty()
        {
            property = parent.property.GetArrayElementAtIndex(Index);
            FinishBind();
        }
        protected void FinishBind()
        {
            if (content != null) this.Remove(content);
            content = Content();
            //Make absolutely sure the content at least takes up a minimum amount of space.
            content.style.flexGrow = 1f;
            content.style.minHeight = 14;
            this.Add(content);
            content.DelayedBuild(() =>
            {
                PostContent();
                if (ContextMenuTarget != null)
                {
                    ContextMenuTarget.RegisterCallback<ContextualMenuPopulateEvent>(ContextMenu, TrickleDown.TrickleDown);
                    dragHandle.MoveCallback<ContextClickEvent>(ContextMenuTarget, TrickleDown.TrickleDown);
                }
                else
                    dragHandle.RegisterCallback<ContextualMenuPopulateEvent>(ContextMenu, TrickleDown.TrickleDown);

                (ContextMenuTarget ?? dragHandle)
                .RegisterCallback<ContextualMenuPopulateEvent>(ContextMenu, TrickleDown.TrickleDown);
            });
        }
        #endregion Initialization

        #region Data
        /// <summary>
        /// The parent <see cref="SuperList{LIST, ITEM, VALUE}"/> that owns this Item.
        /// </summary>
        new public LIST parent { get; protected set; }
        /// <summary>
        /// The serialized property tied to the item this element represents.
        /// </summary>
        public SerializedProperty property { get; protected set; }
        /// <summary>
        /// The "hamburger" icon at the left-most part of the item.
        /// </summary>
        public VisualElement dragHandle { get; protected set; }
        /// <summary>
        /// The <see cref="VisualElement"/> created to hold the visual information displayed by this item.
        /// <br/> Created and assigned by <see cref="Content()"/>
        /// </summary>
        public VisualElement content { get; protected set; }
        /// <summary>
        /// The <see cref="UnityEngine.UIElements.Label"/> that displays the name of the item, should one exist.
        /// <br/> (Should also be assigned in <see cref="Content"/> or <see cref="PostContent"/> if a proper one exists.)
        /// </summary>
        public Label Label { get; protected set; }
        /// <summary>
        /// This is the <see cref="VisualElement"/> that the Context Menu for this item should appear when right-clicking.
        /// <br/> Should be set in <see cref="Content"/> or <see cref="PostContent"/>
        /// <br/> If nothing is designated as the target, the DragHandle will be used instead.
        /// </summary>
        public VisualElement ContextMenuTarget { get; protected set; }
        /// <summary>
        /// The Index of this Item within its owning List
        /// </summary>
        public int Index { get; protected set; }
        #endregion Data


        #region Virtuals


        /// <summary>
        /// An overridable function defining how <see cref="content"/> is created.
        /// </summary>
        /// <returns></returns>
        public virtual VisualElement Content()
        {
            PropertyField result = new(property);
            result.style.flexGrow = 1f;
            result.style.marginRight = 4;
            result.Bind(property.serializedObject);
            return result;
        }

        /// <summary>
        /// An overridable function happening a frame after <see cref="content"/> is attached to its Panel.
        /// </summary>
        protected virtual void PostContent()
        {
            Label = content.Q<Label>(null, "unity-label");
            ContextMenuTarget = Label;
        }

        #region Context Menus 

        /// <summary>
        /// An overridable Re-definition function for the Context Menu tied to this item.
        /// <br/> base implementation should be called in any overrides, as it is responsible for replacing Unity's default Duplicate and Delete Context Menu Items with ones that actually work in SuperLists.
        /// </summary>
        /// <param name="evt"></param>
        protected virtual void ContextMenu(ContextualMenuPopulateEvent evt)
        {
            var list = evt.menu.MenuItems();
            bool duplicateFound = false;
            bool deleteFound = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not DropdownMenuAction iAction) continue;

                if (iAction.name.StartsWith("Apply to Prefab")) list[i] = new DropdownMenuAction(iAction.name, T => ApplyOrRevertContextMenu(iAction), DropDownMenuStatus);
                if (iAction.name.StartsWith("Revert")) list[i] = new DropdownMenuAction(iAction.name, T => ApplyOrRevertContextMenu(iAction), DropDownMenuStatus);

                if (iAction.name == "Duplicate Array Element")
                {
                    list[i] = new DropdownMenuAction("Duplicate", DuplicateContextMenu, DropDownMenuStatus);
                    duplicateFound = true;
                }
                if (iAction.name == "Delete Array Element")
                {
                    list[i] = new DropdownMenuAction("Delete", DeleteContextMenu, DropDownMenuStatus);
                    deleteFound = true;
                }
            }

            if (!duplicateFound)
                list.Add(new DropdownMenuAction("Duplicate", DuplicateContextMenu, DropDownMenuStatus));
            if (!deleteFound)
                list.Add(new DropdownMenuAction("Delete", DeleteContextMenu, DropDownMenuStatus));
        }

        /// <summary>
        /// The Action called when the "Duplicate" Context Menu Item is called.
        /// </summary>
        /// <param name="C"></param>
        public virtual void DuplicateContextMenu(DropdownMenuAction C) => parent.DuplicatePropertySlotAt(Index);
        /// <summary>
        /// The Action called when the "Delete" Context Menu Item is called.
        /// </summary>
        /// <param name="C"></param>
        public virtual void DeleteContextMenu(DropdownMenuAction C)
        {
            if (parent == null) return;
            parent.DeletePropertySlotAt(Index);
            //parent.RemoveItemElement(this as ITEM);
            parent.BuildItems();
            parent.TryForceRefreshPrefabMarkers();
        }
        /// <summary>
        /// The Action called when the "Apply to Prefab" or "Revert" Context Menu Items are called.
        /// <br/> Takes in a copy of the original so it can be executed before running custom logic.
        /// </summary>
        /// <param name="Def"></param>
        public virtual void ApplyOrRevertContextMenu(DropdownMenuAction Def)
        {
            Def.Execute();
            //Update(null);
        }

        #endregion

        #endregion

        #region Colors

        /// <summary>
        /// Sets the visual state for if this object is selected, highlighting it blue.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                UpdateBackground();
            }
        }
        protected bool selected;
        /// <summary>
        /// Sets the visual state for if this object is invalid, highlighting it red.
        /// </summary>
        public bool Invalid
        {
            get => invalid;
            set
            {
                invalid = value;
                UpdateBackground();
            }
        }
        protected bool invalid;
        /// <summary>
        /// An overridable function defining how the background of this item is colored based on different states.
        /// </summary>
        protected virtual void UpdateBackground() => style.backgroundColor =
                selected ? invalid ? SelectionInvalidColor : SelectionColor
                : invalid ? InvalidColor : Color.clear;

        public static Color SelectionColor => ElementHighlighter.ButtonClickedBack;
        public static Color InvalidColor = new(.44f, .24f, .24f);
        public static Color SelectionInvalidColor = new(.486f, .274f, .428f);

        #endregion

        public static DropdownMenuAction.Status DropDownMenuStatus(DropdownMenuAction A) => DropdownMenuAction.Status.Normal;

    }

    /// <summary>
    /// A basic example of the highly customizable <see cref="SuperList{LIST, ITEM, VALUE}"/> made for basic objects.
    /// </summary>
    /// <typeparam name="T">The type this list will hold.</typeparam>
    public class SuperList<T> : SuperList<SuperList<T>, SuperListItem<T>, T>
    {
        public SuperList(SerializedProperty listProperty, Func<Header> HeaderOverride = null) : base(listProperty, false) { }
    }
    /// <summary>
    /// A basic example of the highly customizable <see cref="SuperListItem{LIST, ITEM, VALUE}{LIST, ITEM, VALUE}"/> made for basic objects.
    /// </summary>
    /// <typeparam name="T">The type this list will hold.</typeparam>
    public class SuperListItem<T> : SuperListItem<SuperList<T>, SuperListItem<T>, T>
    {
        public SuperListItem(SuperList<T> parentList, int Index) : base(parentList, Index) { }
    }
}