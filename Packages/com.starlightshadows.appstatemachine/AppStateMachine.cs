using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SLS.Singletons;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace SLS.AppStateMachine
{
    [DefaultExecutionOrder(-160)]
    public class AppStateMachine : GlobalAsset<AppStateMachine>
    {
        [field: SerializeField] public List<AppState> AllStates { get; private set; } = new();
        public static Dictionary<string, AppState> Dict;

        public static event Action Setup;

        public override void OnInit()
        {
            Dict = AllStates.ToDictionary(s => s.name);
            for (int i = 0; i < AllStates.Count; i++)
                if (AllStates[i] != null) AllStates[i].OnEnable();
                else
                {
                    AllStates.RemoveAt(i);
                    i--;
                }
            Setup?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() => Get.AllStates[0].Enter().InstantSafe();

#if UNITY_EDITOR

#if UNITY_EDITOR
        // Non-generic variant to create/load assets by runtime Type
        new public static AppState GetOrCreate(Type t, string path = "Data/GameStates/")
        {
            if (t == null) return null;

            string searchFilter = $"t:{t.Name}";
            string[] guids = UnityEditor.AssetDatabase.FindAssets(searchFilter);

            if (guids != null && guids.Length > 0)
            {
                if (guids.Length > 1)
                    for (int i = guids.Length - 1; i > 0; i--)
                    {
                        UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
                        if (obj != null) Destroy(obj);
                    }

                UnityEngine.Object loaded = UnityEditor.AssetDatabase.LoadMainAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                if (loaded is AppState asset) return asset;
            }

            // Create new ScriptableObject instance of the requested Type
            ScriptableObject created = CreateInstance(t);
            if (created == null) return null;

            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Application.dataPath, path));
            UnityEditor.AssetDatabase.CreateAsset(created, $"Assets/{path}{t.Name}.asset");
            UnityEditor.AssetDatabase.SaveAssets();

            return created as AppState;
        }

        public static bool TryGetAlreadyActive(Type t, out AppState result)
        {
            FieldInfo singletonField = typeof(AppStateGlobal<>).MakeGenericType(t)
                .GetField("S", BindingFlags.Static | BindingFlags.NonPublic);
            PropertyInfo slotField = typeof(Singleton<>).MakeGenericType(t)
                .GetProperty("slot", BindingFlags.Instance | BindingFlags.Public);

            object single = singletonField.GetValue(null);
            if (single is null)
            {
                result = null;
                return false;
            }
            object slot = slotField.GetValue(single);
            if (slot is null || slot.GetType() != t)
            {
                result = null;
                return false;
            }
            result = slot as AppState;
            return true;
        }

#endif


        public class PostProcessor : UnityEditor.AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (!TryGet(out AppStateMachine registry))
                    registry = _GlobalAssetBase.GetOrCreate(typeof(AppStateMachine)) as AppStateMachine;

                _GlobalAssetBase.Enable_For_Editor(registry);

                Type GlobalAssetType = typeof(AppStateGlobal<>);
                List<Type> globalAssetTypes = GlobalAssetType.GetAllInheritors();

                foreach (Type type in globalAssetTypes)
                {
                    // If no existing in-memory instance is found, ensure an asset exists on disk
                    if (TryGetAlreadyActive(type, out AppState currentInstance))
                    {
                        if (!registry.AllStates.Contains(currentInstance)) registry.AllStates.Add(currentInstance);
                        currentInstance.OnEnable();
                    }
                    else
                    {
                        AppState created = GetOrCreate(type);
                        if (!registry.AllStates.Contains(created)) registry.AllStates.Add(created);
                        created.OnEnable();
                    }
                }

                Type GlobalSubAssetType = typeof(AppSubStateGlobal<>);
                List<Type> globalSubAssetTypes = GlobalAssetType.GetAllInheritors();

                foreach (Type type in globalAssetTypes)
                {
                    // If no existing in-memory instance is found, ensure an asset exists on disk
                    if (TryGetAlreadyActive(type, out AppState currentInstance))
                    {
                        if (!registry.AllStates.Contains(currentInstance)) registry.AllStates.Add(currentInstance);
                        currentInstance.OnEnable();
                    }
                    else
                    {
                        AppState created = GetOrCreate(type);
                        if (!registry.AllStates.Contains(created)) registry.AllStates.Add(created);
                        created.OnEnable();
                    }
                }

                //last minute run through of registry's assets to get rid of Null values.
                for (int i = registry.AllStates.Count - 1; i >= 0; i--)
                    if (registry.AllStates[i] == null) registry.AllStates.RemoveAt(i);

                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }

        }

        [UnityEditor.CustomEditor(typeof(AppStateMachine))]
        internal class Editor : UnityEditor.Editor
        {
            public override VisualElement CreateInspectorGUI()
            {
                VisualElement root = new();

                root.Add(new UnityEditor.UIElements.PropertyField(
                    serializedObject.FindProperty($"<{nameof(AppStateMachine.AllStates)}>k__BackingField")));

                Button CreateNewBasicStateButton = new(CreateNewBasicState)
                { text = "Create New Basic State" };
                root.Add(CreateNewBasicStateButton);

                root.Add(new StateTypeTemplate("Boot"));
                root.Add(new StateTypeTemplate("Gameplay"));
                root.Add(new StateTypeTemplate("Paused"));


                return root;
            }


            void CreateNewBasicState()
            {
                AppState NewState = ScriptableObject.CreateInstance<AppState>();
                ProjectWindowUtil.CreateAsset(NewState, "Assets/Data/GameStates/New Game State.asset");
                Get.AllStates.Add(NewState);
                EditorUtility.SetDirty(Get);
            }

            class StateTypeTemplate : VisualElement
            {
                public StateTypeTemplate(string name)
                {
                    title = name;
                    if (AlreadyMade()) return;
                    Button button = new(ButtonPressed)
                    {
                        text = $"Clone {name} State"
                    };
                    Add(button);
                }

                string title;

                bool AlreadyMade()
                {
                    for (int i = 0; i < Get.AllStates.Count; i++)
                        if (Get.AllStates[i] != null && Get.AllStates[i].name == title)
                            return true;
                    return false;
                }

                void ButtonPressed()
                {
                    //Clone new .cs file from the .txt file template in the package.
                    string templatePath = $"Packages/com.starlightshadows.gamestatemachine/Templates/{title}Template.txt";
                    string newFilePath = $"Assets/Data/GameStates/Scripts/{title}.cs";
                    ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, newFilePath);
                    //UnityEditor.AssetDatabase.CopyAsset(templatePath, newFilePath);
                    ////Change .txt file to .cs
                    //UnityEditor.AssetDatabase.RenameAsset(newFilePath, $"{title}.cs");

                }
            }
        }
#endif
    }
}
