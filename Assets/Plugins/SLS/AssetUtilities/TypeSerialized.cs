using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class TypeSerialized : ISerializationCallbackReceiver
{
    [SerializeField] private string assemblyQualifiedName = string.Empty;

    public Type Type { get; set; }

    // Convert System.Type to string before Unity saves data
    public void OnBeforeSerialize() =>
        assemblyQualifiedName = Type != null ? Type.AssemblyQualifiedName : string.Empty;

    // Convert string back to System.Type after Unity loads data
    public void OnAfterDeserialize() =>
        Type = !string.IsNullOrEmpty(assemblyQualifiedName) ? Type.GetType(assemblyQualifiedName) : null;

    // Implicit conversion operator allows treating this class directly as a System.Type
    public static implicit operator Type(TypeSerialized serializableType) => serializableType?.Type;
}
[Serializable]
public class TypeSerialized<T> : TypeSerialized { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TypeSerialized))]
[CustomPropertyDrawer(typeof(TypeSerialized<>))]
public class TypeFieldDrawer : PropertyDrawer
{
    TextField textField;
    SerializedProperty boundProperty;
    VisualElement actualField;

    string currentAssemblyQualifiedName;
    Type currentType;

    // For "Please Wait..." restore handling
    string priorText;
    bool selectionMade;

    // Global cache to reduce reflection cost

    // Create the UI, bind property and cache property for later use
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        boundProperty = property;

        currentAssemblyQualifiedName = property.FindPropertyRelative("assemblyQualifiedName").stringValue;
        currentType = Type.GetType(currentAssemblyQualifiedName);

        textField = new TextField()
        {
            label = preferredLabel
        };
        // Bind property so Unity can handle undo/serialization properly
        textField.BindProperty(property);
        textField.SetValueWithoutNotify(currentType != null ? GetDisplayName(currentType) : "Click to Select Type");
        textField.isReadOnly = true;

        actualField = textField.Q("unity-text-input", "unity-base-text-field__input");
        actualField.style.borderLeftColor = Color.green;
        actualField.style.borderRightColor = Color.green;

        //Open picker on pointer down
        actualField.RegisterCallback<PointerDownEvent>(ShowPicker, TrickleDown.TrickleDown);

        return textField;
    }

    // Called when the user clicks the text field
    public void ShowPicker(PointerDownEvent ev)
    {
        // Only react to clicks on the field itself
        if (boundProperty == null) return;

        // Save previous display, then show waiting text
        priorText = textField.value;
        textField.SetValueWithoutNotify("Please Wait. . .");

        // Determine if the field type imposes a generic constraint (TypeSerialized<T>)
        Type requiredBaseType = null;
        try
        {
            var fieldType = fieldInfo.FieldType;
            if (fieldType.IsGenericType)
            {
                var args = fieldType.GetGenericArguments();
                if (args != null && args.Length > 0)
                    requiredBaseType = args[0];
            }
        }
        catch
        {
            requiredBaseType = null;
        }

        // Reset selection flag then show tree; after it returns, restore text if user cancelled
        selectionMade = false;
        ShowAsIMGUI(requiredBaseType);

        // After dropdown closed: if no selection was made, restore previous text
        if (!selectionMade)
        {
            textField.SetValueWithoutNotify(priorText);
        }
    }

    void ShowAsContextMenu(Type requiredBaseType)
    {
        // Not used by default. Kept for potential future fallback.
        var menu = new GenericMenu();

        // Add option to clear selection
        menu.AddItem(new GUIContent("None"), currentType == null, () =>
        {
            boundProperty.serializedObject.Update();
            boundProperty.FindPropertyRelative("assemblyQualifiedName").stringValue = string.Empty;
            boundProperty.serializedObject.ApplyModifiedProperties();
            textField.SetValueWithoutNotify("Click to Select Type");
        });

        // Gather assemblies and types
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .OrderBy(a => a.GetName().Name);

        foreach (var asm in assemblies)
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }

            var candidateTypes = types
                .Where(t =>
                    // ignore generic type definitions (open generics), compiler generated anonymous types and private nested types
                    !t.IsGenericTypeDefinition &&
                    !t.IsNestedPrivate &&
                    !t.IsInterface &&
                    !t.IsAbstract &&
                    !string.IsNullOrEmpty(t.FullName))
                .OrderBy(t => t.Namespace ?? string.Empty)
                .ThenBy(t => GetDisplayName(t))
                .ToArray();

            if (candidateTypes.Length == 0) continue;

            string asmName = asm.GetName().Name;

            foreach (var t in candidateTypes)
            {
                if (t.Name.StartsWith("<PrivateImplementation") || t.Name.StartsWith("UnitySourceGenerated")) continue;
                // Build menu path: Assembly / namespace segments... / TypeName
                List<string> pathParts = new();
                pathParts.AddRange(asmName.Split("."));
                if (!string.IsNullOrEmpty(t.Namespace))
                    pathParts.AddRange(t.Namespace.Split('.'));
                pathParts.Add(GetDisplayName(t));
                var menuPath = string.Join("/", pathParts);

                // Capture type locally for closure
                var capturedType = t;
                //menu.AddItem(new(menuPath), IsSameType(capturedType, currentType), PostChoose, capturedType);
            }
        }

        // Show the menu at the TextField worldBound rectangle
        try
        {
            var world = textField.worldBound;
            menu.DropDown(new Rect(world.x, world.y, world.width, world.height));
        }
        catch
        {
            // Fallback: show at mouse position if DropDown fails
            var mouse = GUIUtility.GUIToScreenPoint(Event.current != null ? Event.current.mousePosition : Vector2.zero);
            menu.DropDown(new Rect(mouse.x, mouse.y, 1, 1));
        }

    }

    void ShowAsIMGUI(Type requiredBaseType)
    {
        TypeTreeDropdown.Show(textField.layout, requiredBaseType, PostChoose);
        //TypeTree target;
        //if(requiredBaseType is null)
        //{
        //    if(NoLimitTypeTree is null)
        //    {
        //        AdvancedDropdownState state = new();
        //        NoLimitTypeTree = new(state, requiredBaseType);
        //    }
        //    target = NoLimitTypeTree;
        //}
        //else
        //{
        //    if (!BuiltTypes.Contains(requiredBaseType))
        //    {
        //        AdvancedDropdownState state = new();
        //        BuiltTypes.Add(requiredBaseType);
        //        BuiltTypeTrees.Add(new(state, requiredBaseType));
        //
        //        if(BuiltTypes.Count > 3)
        //        {
        //            BuiltTypes.RemoveAt(0);
        //            BuiltTypeTrees.RemoveAt(0);
        //        }
        //    }
        //    target = BuiltTypeTrees[BuiltTypes.IndexOf(requiredBaseType)];
        //}
        //
        //// AdvancedDropdown.Show is expected to return after the dropdown is closed.
        //// We rely on that to perform restore logic in ShowPicker.
        //target.Show(textField.layout, PostChoose);
    }

    // Utility: produce a friendly display name for generic and nested types
    static string GetDisplayName(Type t)
    {
        if (t.IsGenericType)
        {
            var name = t.Name;
            var backtick = name.IndexOf('`');
            if (backtick > 0) name = name.Substring(0, backtick);
            var args = t.GetGenericArguments();
            var argNames = args.Select(a => a.Name).ToArray();
            return $"{name}<{string.Join(", ", argNames)}>";
        }
        return t.Name;
    }

    static bool IsSameType(Type a, Type b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return a.AssemblyQualifiedName == b.AssemblyQualifiedName;
    }

    public void PostChoose(Type chosen)
    {
        // Mark that a selection (including explicit clear) happened so the UI does not revert.
        selectionMade = true;

        if (chosen != null)
        {
            boundProperty.serializedObject.Update();
            boundProperty.FindPropertyRelative("assemblyQualifiedName").stringValue = chosen.AssemblyQualifiedName;
            boundProperty.serializedObject.ApplyModifiedProperties();
            textField.SetValueWithoutNotify(GetDisplayName(chosen));
        }
        else
        {
            boundProperty.serializedObject.Update();
            boundProperty.FindPropertyRelative("assemblyQualifiedName").stringValue = string.Empty;
            boundProperty.serializedObject.ApplyModifiedProperties();
            textField.SetValueWithoutNotify("Click to Select Type");
        }
    }

    internal class TypeTreeDropdown : AdvancedDropdown
    {


        Type requiredBaseType;
        Action<Type> result;

        private static AdvancedDropdownItem NoReqTree = null;
        private static List<Type> BuiltTypes = new();
        private static List<AdvancedDropdownItem> BuiltTypeTrees = new();

        public TypeTreeDropdown(AdvancedDropdownState state) : base(state){}


        private static TypeTreeDropdown Instance = null;
        public static void Show(Rect Placement, Type targetType, Action<Type> result)
        {
            if (Instance == null) Instance = new(new());
            Instance.ShowInstance(Placement, targetType, result);
        }
        private void ShowInstance(Rect Placement, Type targetType,  Action<Type> result)
        {
            requiredBaseType = targetType;
            this.result = result;
            this.Show(Placement);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            if(requiredBaseType is null)
            {
                if (NoReqTree is null) NewMenu(null);
                return NoReqTree;
            }
            else
            {
                if (!BuiltTypes.Contains(requiredBaseType)) NewMenu(requiredBaseType);
                return BuiltTypeTrees[BuiltTypes.IndexOf(requiredBaseType)];
            }
        }

        public AdvancedDropdownItem NewMenu(Type reqType)
        { 
            Folder root = new("Select Type");

            if(reqType is null)
            {
                NoReqTree = root;
            }
            else
            {
                BuiltTypes.Add(reqType);
                BuiltTypeTrees.Add(root);
                if (BuiltTypes.Count > 3)
                {
                    BuiltTypes.RemoveAt(0);
                    BuiltTypeTrees.RemoveAt(0);
                }
            }

                

            // Use precomputed cache and then apply requiredBaseType filter
            List<Entry> entries = new();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (string.IsNullOrEmpty(type.FullName)
                        || type.FullName.Contains("<PrivateImplementation")
                        || type.FullName.Contains("UnitySourceGenerated")
                        || type.IsGenericTypeDefinition
                        || type.IsNestedPrivate
                        || type.IsInterface
                        || type.IsAbstract
                        || (reqType is not null && !reqType.IsAssignableFrom(type))) continue;
                    entries.Add(new(type));
                }
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .OrderBy(a => a.GetName().Name);



            // Sort entries by pieces (namespace then displayName) for deterministic menu ordering
            entries.Sort((a, b) =>
            {
                int nsCompare = string.Compare(a.fullPath, b.fullPath, StringComparison.Ordinal);
                if (nsCompare != 0) return nsCompare;
                return string.Compare(a.type.Name, b.type.Name, StringComparison.Ordinal);
            });

            for (int i = 0; i < entries.Count; i++)
            {
                FinalSelection thisSel = new(entries[i].type);
                Folder pointer = root;
                for (int j = 0; j < entries[i].pieces.Count - 1; j++)
                    pointer = pointer.Traverse(entries[i][j]);
                pointer.AddChild(thisSel);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            FinalSelection entry = item as FinalSelection;
            result?.Invoke(entry.type);
        }

        public class Folder : AdvancedDropdownItem
        {
            new public Dictionary<string, Folder> children = new();

            public Folder(string name) : base(name) { }

            public Folder Traverse(string name)
            {
                if (!children.ContainsKey(name))
                {
                    Folder newF = new(name);
                    children.Add(name, newF);
                    AddChild(newF);
                }
                return children[name];
            }
        }

        public class Entry
        {
            public Type type;
            public string fullPath;
            public List<string> pieces;
            public string this[int i] => pieces[i];

            public Entry(Type t)
            {
                Assembly assembly = t.Assembly;
                type = t;

                string[] assemblyPath = assembly.GetName().Name.Split(".");
                pieces = type.Namespace != null ? type.Namespace.Split(".").ToList() : new();

                int shared = -1;
                for (; shared < assemblyPath.Length-1 && shared < pieces.Count-1; shared++)
                    if (assemblyPath[shared + 1] != pieces[shared + 1]) 
                        break;
                assemblyPath = assemblyPath[..(shared+1)];
                pieces.InsertRange(0, assemblyPath);
                pieces.Add(GetDisplayName(type));

                fullPath = string.Join('.', pieces);
            }
        }

        public class FinalSelection : AdvancedDropdownItem
        {
            public Type type;
            public FinalSelection(Type type) : base(GetDisplayName(type)) => this.type = type;
        }
    }


    //private static TypeTree NoLimitTypeTree;
    //private static List<Type> BuiltTypes = new();
    //private static List<TypeTree> BuiltTypeTrees = new();

}
#endif