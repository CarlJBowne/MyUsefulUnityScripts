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

namespace SLS.GameStateMachine
{
    [DefaultExecutionOrder(-160)]
    public class GameStateRegistry : GlobalAsset<GameStateRegistry>
    {
        [field: SerializeField] public List<GameState> AllStates { get; private set; } = new();
        public static Dictionary<string, GameState> Dict;

        public static Action Setup;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Boot() => Get.AllStates[0].Enter();

#if UNITY_EDITOR

        public class PostProcessor : UnityEditor.AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (!TryGet(out GameStateRegistry registry))
                    registry = GetOrCreate(typeof(GameStateRegistry)) as GameStateRegistry;

                registry.OnEnable();

                Type GlobalAssetType = typeof(GameStateSingle<>);
                Type[] globalAssetTypes = GlobalAssetType.GetAllInheritors();

                foreach (Type type in globalAssetTypes)
                {
                    // If no existing in-memory instance is found, ensure an asset exists on disk
                    if (_GameStateSingleBase.TryGetAlreadyActive(type, out _GameStateSingleBase currentInstance))
                    {
                        if (!registry.AllStates.Contains(currentInstance)) registry.AllStates.Add(currentInstance);
                        currentInstance.OnEnable();
                    }
                    else
                    {
                        _GameStateSingleBase created = _GameStateSingleBase.GetOrCreate(type);
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

        [UnityEditor.CustomEditor(typeof(GameStateRegistry))]
        internal class Editor : UnityEditor.Editor
        {
            public override VisualElement CreateInspectorGUI()
            {
                VisualElement root = new();

                root.Add(new UnityEditor.UIElements.PropertyField(
                    serializedObject.FindProperty($"<{nameof(GameStateRegistry.AllStates)}>k__BackingField")));

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
                GameState NewState = ScriptableObject.CreateInstance<GameState>();
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
