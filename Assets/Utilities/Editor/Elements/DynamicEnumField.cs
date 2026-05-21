using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;

namespace Utilities.Xtensions.VisualElements
{
    /// <summary>
    /// A simple dynamic enum-like field backed by a list of strings. <br/>
    /// - You can provide options at construction or later via SetOptions/AddOption(s). <br/>
    /// - When user changes selection the callback receives the selected index (int). <br/>
    /// - Call Rebuild() to force re-creation of the internal control.
    /// </summary>
    public class DynamicEnumField : VisualElement
    {
        private List<string> _options = new();
        private int _selectedIndex = -1;
        private VisualElement _currentControl;

        // Backing field for the optional prefix label
        private string _label = null;

        /// <summary>
        /// If set to a non-empty string, a label will be shown to the left of the popup (or hint).
        /// Setting this property triggers a Rebuild().
        /// </summary>
        public string label
        {
            get => _label;
            set
            {
                if (_label == value) return;
                _label = value;
                Rebuild();
            }
        }

        /// <summary>
        /// Invoked when the selection changes. Argument is the selected index (or -1 if none).
        /// </summary>
        public Action<int> OnSelectionChanged { get; set; }

        /// <summary>
        /// Create an empty DynamicEnumField.
        /// </summary>
        public DynamicEnumField()
        {
            name = "dynamic-enum-field";
            Rebuild();
        }

        /// <summary>
        /// Create and initialize with options.
        /// </summary>
        /// <param name="options">Initial option labels.</param>
        /// <param name="selectedIndex">Initial selected index.</param>
        /// <param name="onChanged">Callback invoked when user changes selection (index).</param>
        public DynamicEnumField(IEnumerable<string> options, int selectedIndex = 0, Action<int> onChanged = null)
        {
            name = "dynamic-enum-field";
            if (options != null) _options = new List<string>(options);
            _selectedIndex = ClampIndex(selectedIndex);
            OnSelectionChanged = onChanged;
            Rebuild();
        }

        /// <summary>
        /// Replace the entire option set.
        /// </summary>
        public void SetOptions(IEnumerable<string> options, int selectedIndex = 0)
        {
            _options = (options != null) ? new List<string>(options) : new List<string>();
            _selectedIndex = ClampIndex(selectedIndex);
            Rebuild();
        }

        /// <summary>
        /// Add a single option. Optionally select it.
        /// </summary>
        public void AddOption(string option, bool select = false)
        {
            if (option == null) option = string.Empty;
            _options.Add(option);
            if (select) _selectedIndex = _options.Count - 1;
            // Recreate the popup to ensure internal list matches (robust and simple).
            Rebuild();
        }

        /// <summary>
        /// Add multiple options.
        /// </summary>
        public void AddOptions(IEnumerable<string> options)
        {
            if (options == null) return;
            _options.AddRange(options);
            Rebuild();
        }

        /// <summary>
        /// Clear all options.
        /// </summary>
        public void ClearOptions()
        {
            _options.Clear();
            _selectedIndex = -1;
            Rebuild();
        }

        /// <summary>
        /// Selected index in the current options. -1 if none.
        /// Setting clamps the value and updates the UI.
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = ClampIndex(value);
                UpdateControlValue();
            }
        }

        /// <summary>
        /// Set: Attempts to find an index matching the input string and sets SelectedIndex. If not found, selection is unchanged. <br/>
        /// Get: Returns the currently selected option string, or null if index is invalid.
        /// </summary>
        public string SelectedValue
        {
            get => (_selectedIndex >= 0 && _selectedIndex < _options.Count) ? _options[_selectedIndex] : null;
            set
            {
                if(_options.Contains(value)) SelectedIndex = _options.IndexOf(value);
            }
        }

        /// <summary>
        /// Returns a read-only snapshot of options.
        /// </summary>
        public IReadOnlyList<string> Options => _options.AsReadOnly();

        /// <summary>
        /// Force recreate of the internal control to reflect current options/state.
        /// </summary>
        public void Rebuild()
        {
            // Remove existing child control(s)
            Clear();
            _currentControl = null;

            // Helper to add a prefix label if requested
            Label CreatePrefixLabel()
            {
                var prefix = new Label(_label ?? string.Empty) { name = "dynamic-enum-prefix" };
                prefix.style.unityTextAlign = TextAnchor.MiddleLeft;
#if UNITY_EDITOR
                // try to match Inspector label width when in editor
                prefix.style.minWidth = EditorGUIUtility.labelWidth;
#endif
                prefix.style.minHeight = 18;
                prefix.style.marginRight = 4;
                return prefix;
            }

            if (_options == null || _options.Count == 0)
            {
                // Show a hint label when there are no options
                var hint = new Label("(no options)") { name = "dynamic-enum-empty" };
                hint.style.unityTextAlign = TextAnchor.MiddleLeft;
                hint.style.minHeight = 18;
                _currentControl = hint;

                if (!string.IsNullOrEmpty(_label))
                {
                    var row = new VisualElement { name = "dynamic-enum-row" };
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.Add(CreatePrefixLabel());
                    row.Add(hint);
                    Add(row);
                }
                else
                {
                    Add(hint);
                }

                _selectedIndex = -1;
                return;
            }

            // Ensure selected index is valid
            _selectedIndex = ClampIndex(_selectedIndex);

            // Create a PopupField<string> with the current options
            var popup = new PopupField<string>(new List<string>(_options), Math.Max(0, _selectedIndex), s => s, s => s)
            {
                name = "dynamic-enum-popup",
                style =
                {
                    minHeight = 18,
                    flexGrow = 1
                }
            };

            // Wire change callback to map selected value to index and invoke OnSelectionChanged
            popup.RegisterValueChangedCallback(evt =>
            {
                var newVal = evt.newValue;
                int idx = _options.IndexOf(newVal);
                if (idx < 0 || idx >= _options.Count) idx = -1;
                _selectedIndex = idx;
                try
                {
                    OnSelectionChanged?.Invoke(_selectedIndex);
                }
                catch
                {
                    // swallow user callback exceptions to avoid breaking editor UI
                }
            });

            // Keep reference pointed at the popup so UpdateControlValue can act on it directly
            _currentControl = popup;

            if (!string.IsNullOrEmpty(_label))
            {
                var row = new VisualElement { name = "dynamic-enum-row" };
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.Add(CreatePrefixLabel());
                row.Add(popup);
                Add(row);
            }
            else
            {
                Add(popup);
            }
        }

        private void UpdateControlValue()
        {
            if (_currentControl is PopupField<string> pf)
            {
                // If index valid, set value; otherwise set to first item or keep consistent
                if (_selectedIndex >= 0 && _selectedIndex < _options.Count)
                {
                    pf.value = _options[_selectedIndex];
                }
                else if (_options.Count > 0)
                {
                    pf.value = _options[0];
                    _selectedIndex = 0;
                }
            }
            else
            {
                // If currently showing label and we now have options, rebuild to popup
                if (_options != null && _options.Count > 0) Rebuild();
            }
        }

        private int ClampIndex(int idx)
        {
            if (_options == null || _options.Count == 0) return -1;
            if (idx < 0) return 0;
            if (idx >= _options.Count) return _options.Count - 1;
            return idx;
        }
    }
}

