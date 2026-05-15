using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Utilities.Xtensions.VisualElements;

public class DialogueEditor : GraphViewEditorWindow
{
    public static DialogueEditor WindowInstance { get; private set; }

    DialogueAsset selectedAsset = null;

    Toolbar headerBar;
    ObjectField assetField;
    Button saveDataButton;

    DialogueGraphView graphView;
    List<DialoguePieceNode> nodes = new();

    [MenuItem("Window/Dialogue Editor")]
    public static void CreateWindow()
    {
        WindowInstance = (DialogueEditor)EditorWindow.GetWindow(typeof(DialogueEditor), false, "Dialogue Editor");
        WindowInstance.Show();
    }

    void OnEnable()
    {
        // Build UI
        rootVisualElement.style.flexDirection = FlexDirection.Column;

        rootVisualElement.CreateAddAndStore(out headerBar, new Toolbar());

        headerBar.CreateAddAndStore(out assetField, () =>
        {
            var field = new ObjectField()
            {
                objectType = typeof(DialogueAsset),
                allowSceneObjects = false,
                style = { minWidth = 200 }
            };
            field.RegisterValueChangedCallback(UpdateSelectedAsset);
            return field;
        });
        headerBar.CreateAddAndStore(out saveDataButton, new Button(SaveSelectedAsset)
        {
            text = "Save",
            style =
            {
                position = Position.Absolute,
                right = -1
            }
        });

        rootVisualElement.CreateAddAndStore(out graphView, new DialogueGraphView()
        {
            name = "DialogueGraphView",
            style =
            {
                backgroundColor = Color.gray1,
                top = EditorGUIUtility.singleLineHeight + 2,
                flexGrow = 1,
            }
        });

        if (selectedAsset != null) LoadSelectedAsset();
    }

    void OnDisable()
    {
        graphView = null; // Was Dispose. Check if better garbage handling is needed later.
    }

    void UpdateSelectedAsset(ChangeEvent<UnityEngine.Object> e)
    {
        if (e.newValue is not null and not DialogueAsset)
        {
            ShowNotification(new GUIContent("Selected object is not a DialogueAsset."));
            return;
        }
        var newAsset = e.newValue as DialogueAsset;

        if (selectedAsset != null)
        {
            //Display dialogue to Save or discard changes before switching assets
            int i = EditorUtility.DisplayDialogComplex("Switch Dialogue Asset", "Do you want to save changes to the current asset before switching?", "Save", "Cancel", "Discard Changes");
            if (i == 0) SaveSelectedAsset();
            else if (i == 1)
            {
                assetField.value = selectedAsset; // Revert selection
                return;
            }
        }
        selectedAsset = newAsset;
        ClearGraph();
        if (selectedAsset != null) LoadSelectedAsset();
    }

    void LoadSelectedAsset()
    {
        selectedAsset.Focus();
        DialogueProfileNode.NameCache.Init(true);
        ClearGraph();
        JArray Data = JArray.Parse(selectedAsset.rawData);
        for (int i = 0; i < Data.Count; i++)
        {
            nodes.Add(DialoguePieceNode.Create(Data[i], i));
            graphView.AddElement(nodes[i]);
        }
    }

    void ClearGraph()
    {
        nodes.Clear();
        if (graphView != null)
            graphView.DeleteElements(graphView.nodes.ToList());
    }


    void SaveSelectedAsset()
    {
        if (selectedAsset == null)
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Dialogue Asset", "NewDialogueAsset", "asset", "Create a new DialogueAsset");
            if (string.IsNullOrEmpty(path)) return;
            selectedAsset = ScriptableObject.CreateInstance<DialogueAsset>();
            AssetDatabase.CreateAsset(selectedAsset, path);
            AssetDatabase.SaveAssets();
            assetField.value = selectedAsset;
            return;
        }

        JArray data = new();
        for (int i = 0; i < nodes.Count; i++)
            data.Add(nodes[i].Serialize());
        selectedAsset.rawData = data.ToString();

        // For now, simply mark the asset dirty so it can be saved from the inspector/project window.
        EditorUtility.SetDirty(selectedAsset);
        AssetDatabase.SaveAssets();
        ShowNotification(new GUIContent("Marked asset dirty and saved."));
    }


    // Simple GraphView specialized for dialogue pieces
    class DialogueGraphView : GraphView
    {
        const int nodeWidth = 280;

        public DialogueGraphView()
        {
            this.StretchToParentSize();
            SetupZoom(ContentZoomer.DefaultMinScale * 3, ContentZoomer.DefaultMaxScale * 2);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }
    }

    public abstract class DialoguePieceNode : Node
    {
        public static DialoguePieceNode Create(JToken piece, int index) =>
              piece.Type == JTokenType.String ? new DialogueSegmentNode(piece, index)
            : piece.Type == JTokenType.Array ? new DialogueSegmentNode(piece, index)
            : piece.Type == JTokenType.Object ? new DialogueActionsNode(piece, index)
            : (DialoguePieceNode)null;
        public abstract JToken Serialize();
    }
    public class DialogueSegmentNode : DialoguePieceNode
    {
        int nodeIndex;
        string activeText;

        TextField textField;
        DialogueProfileNode profileField;
        DialogueActionsNode actionsField;

        public DialogueSegmentNode(JToken segment, int index)
        {
            nodeIndex = index;
            title = $"Segment {index}";

            JToken textToken = segment.Type == JTokenType.String ? segment : segment[0];
            JToken profileToken = segment.Type == JTokenType.Array && segment.Count() > 1 ? segment[1] : null;
            activeText = (string)textToken;

            mainContainer.CreateAddAndStore(out textField, () =>
            {
                var res = new TextField()
                {
                    value = activeText,
                    multiline = true,
                    style =
                    {
                        width = DialogueGlobalData.TextBoxWidth
                    }
                };
                if (res.QCache(out VisualElement textInput, "unity-text-input"))
                    textInput.style.height = 1 + (DialogueGlobalData.TextBoxLineCount * 14);
                res.RegisterValueChangedCallback(e => { textToken = e.newValue; });
                return res;
            });
            mainContainer.CreateAddAndStore(out profileField, () => new DialogueProfileNode(profileToken));

            this.expanded = false;
            this.m_CollapseButton.style.display = DisplayStyle.None;
            this.titleContainer.style.height = 18;
            capabilities |= Capabilities.Movable | Capabilities.Selectable | Capabilities.Copiable | Capabilities.Deletable | Capabilities.Groupable | Capabilities.Snappable | Capabilities.Stackable;
        }

        private JToken SerializedText() => (JToken)textField.value;
        public override JToken Serialize()
        {
            JToken prof = profileField.Serialize();
            JToken act = actionsField != null ? actionsField.Serialize() : null;

            if (prof is null && act is null) return SerializedText();
            else
            {
                JArray result = new() { SerializedText() };
                if (prof is not null || act is not null) result.Add(prof ?? "Current");
                if (act is not null) result.Add(act);
                return result;
            }
        }

    }
    public class DialogueActionsNode : DialoguePieceNode
    {
        DialogueActions actions;
        int nodeIndex;
        public DialogueActionsNode(JToken actions, int index)
        {
            //this.actions = actions;
            //nodeIndex = index;
            //title = $"Actions {index}";
            //var main = this.mainContainer;
            //
            //RefreshMainContents(main);
            //
            //var footer = new VisualElement();
            //footer.style.flexDirection = FlexDirection.Row;
            //footer.style.justifyContent = Justify.FlexStart;
            //footer.style.marginTop = 6;
            //footer.Add(new Label($"Index: {index}"));
            //this.extensionContainer.Add(footer);
            //capabilities |= Capabilities.Movable | Capabilities.Selectable;
        }

        void RefreshMainContents(VisualElement main)
        {
            main.Clear();

            // Show current actions
            if (actions.actions.Count == 0)
            {
                main.Add(new Label("No actions"));
            }
            else
            {
                foreach (var kv in actions.actions)
                {
                    var t = kv.Key;
                    var act = kv.Value;
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.style.marginBottom = 4;

                    var nameLabel = new Label(t.Name) { style = { minWidth = 80 } };
                    row.Add(nameLabel);

                    // Specific editors for known action types
                    if (act is DialogueAction.GoTo g)
                    {
                        var intField = new IntegerField("Goto") { value = g.segmentIndex };
                        intField.RegisterValueChangedCallback(e => g.segmentIndex = e.newValue);
                        row.Add(intField);
                    }
                    else if (act is DialogueAction.Event ev)
                    {
                        var intField = new IntegerField("EventID") { value = ev.eventID };
                        intField.RegisterValueChangedCallback(e => ev.eventID = e.newValue);
                        row.Add(intField);
                    }
                    else if (act is DialogueAction.HideBox hb)
                    {
                        var toggle = new Toggle("Hidden") { value = hb.hidden };
                        toggle.RegisterValueChangedCallback(e => hb.hidden = e.newValue);
                        row.Add(toggle);
                    }
                    else if (act is DialogueAction.AutoSkip)
                    {
                        row.Add(new Label("AutoSkip"));
                    }
                    else if (act is DialogueAction.End)
                    {
                        row.Add(new Label("End"));
                    }

                    var removeBtn = new Button(() => { actions.RemoveAction(t); RefreshMainContents(main); }) { text = "Remove" };
                    row.Add(removeBtn);

                    main.Add(row);
                }
            }

            // Controls to add new actions
            var addRow = new VisualElement();
            addRow.style.flexDirection = FlexDirection.Row;
            addRow.style.alignItems = Align.Center;
            addRow.style.marginTop = 8;

            var addGoto = new Button(() => { actions.AddAction(new DialogueAction.GoTo(0)); RefreshMainContents(main); }) { text = "Add GoTo" };
            var addEvent = new Button(() => { actions.AddAction(new DialogueAction.Event(0)); RefreshMainContents(main); }) { text = "Add Event" };
            var addHide = new Button(() => { actions.AddAction(new DialogueAction.HideBox(false)); RefreshMainContents(main); }) { text = "Add HideBox" };
            var addAuto = new Button(() => { actions.AddAction(new DialogueAction.AutoSkip()); RefreshMainContents(main); }) { text = "Add AutoSkip" };
            var addEnd = new Button(() => { actions.AddAction(new DialogueAction.End()); RefreshMainContents(main); }) { text = "Add End" };

            addRow.Add(addGoto);
            addRow.Add(addEvent);
            addRow.Add(addHide);
            addRow.Add(addAuto);
            addRow.Add(addEnd);

            main.Add(addRow);
        }

        public override JToken Serialize()
        {
            return null;
        }

    }
    public class DialogueProfileNode : VisualElement
    {
        DynamicEnumField Enum;
        TextField customName;
        DynamicEnumField customVoiceID;
        FloatField customTextSpeed;
        BaseBoolField customHideNameplate;

        internal static class NameCache
        {
            internal static List<string> IDs;
            internal static List<string> soundIDs;
            internal static int customOptionIndex;
            const int firstNamedProfileIndex = 4; // Previous, Default, ???, Narrator are reserved indices before custom profiles
            static bool built = false;

            internal static void Init(bool doAnyway = false)
            {
                if (built && !doAnyway) return;
                built = true;
                IDs = DialogueProfile.Profiles.Keys.ToList();
                IDs.Insert(0, "Current");
                IDs.Insert(1, "Default");
                IDs.Insert(2, "Unknown");
                IDs.Insert(3, "Narrator");
                IDs.Add("Custom");
                customOptionIndex = IDs.Count - 1;

                soundIDs = DialogueGlobalData.Get.audioClips.Keys.ToList();
                soundIDs.Insert(0, "Default");
                soundIDs.Insert(1, "Current");
                soundIDs.Insert(2, "Unknown");
                soundIDs.Insert(3, "Narrator");
                soundIDs.InsertRange(4, DialogueProfile.Profiles.Keys);
                soundIDs.InsertRange(soundIDs.Count, DialogueAsset.Current.sfx.Keys);
            }
        }

        public DialogueProfileNode(JToken input)
        {
            this.CreateAddAndStore(out Enum, new DynamicEnumField(NameCache.IDs, 0, Updated));
            this.CreateAddAndStore(out customName, () =>
            {
                var res = new TextField()
                {
                    label = "Name:",
                    style = {
                        marginLeft = 18 ,
                        maxWidth = DialogueGlobalData.TextBoxWidth - 16
                    }
                };
                if (res.QCache(out Label label, className: "unity-label")) label.style.minWidth = 42;
                return res;
            });
            this.CreateAddAndStore(out customVoiceID, () =>
            {
                var res = new DynamicEnumField(NameCache.soundIDs, 0)
                {
                    label = "Voice:",
                    style = {
                        marginLeft = 18 ,
                        maxWidth = DialogueGlobalData.TextBoxWidth - 14,
                    }
                };
                if (res.QCache(out Label label, className: "unity-label")) label.style.minWidth = 36;
                return res;
            });
            this.CreateAddAndStore(out customTextSpeed, () =>
            {
                var res = new FloatField()
                {
                    label = "Speed:",
                    value = DialogueProfile.Default.textSpeed,
                    style = {
                        marginLeft = 18 ,
                        maxWidth = DialogueGlobalData.TextBoxWidth - 16
                    }
                };
                if (res.QCache(out Label label, className: "unity-label")) label.style.minWidth = 42;
                return res;
            });
            this.CreateAddAndStore(out customHideNameplate, new Toggle()
            {
                label = "Hide Name:",
                style = {
                    marginLeft = 18 ,
                    maxWidth = DialogueGlobalData.TextBoxWidth - 20
                }
            });

            void Updated(int newValue)
            {
                customName.style.display = newValue == NameCache.customOptionIndex ? DisplayStyle.Flex : DisplayStyle.None;
                customVoiceID.style.display = newValue == NameCache.customOptionIndex ? DisplayStyle.Flex : DisplayStyle.None;
                customTextSpeed.style.display = newValue == NameCache.customOptionIndex ? DisplayStyle.Flex : DisplayStyle.None;
                customHideNameplate.style.display = newValue == NameCache.customOptionIndex ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (input == null) { }
            else if (input.Type is JTokenType.String)
            {
                string value = (string)input;

                value = value is "" or "Prev" or "prev" or "current" or "Previous" or "previous"
                ? "Current"

                : value is " " or "Def" or "def" or "default"
                ? "Default"

                : value is "?" or "???" or "Unknown" or "unknown"
                ? "Unknown"

                : value is "n" or "N" or "Narrate" or "Narration" or "n" or "narrate" or "narration" or "narrator"
                ? "Narrator"

                : value;

                Enum.SelectedValue = value;
            }
            else if (input.Type is JTokenType.Object)
            {
                Enum.SelectedValue = "Custom";
                JObject obj = input as JObject;
                if (obj.TryGetValue("name", out JToken displayNameToken)) customName.value = (string)displayNameToken;
                if (obj.TryGetValue("voiceID", out JToken voiceIDToken)) customVoiceID.SelectedValue = (string)voiceIDToken;
                if (obj.TryGetValue("textSpeed", out JToken textSpeedToken)) customTextSpeed.value = (float)textSpeedToken;
                if (obj.TryGetValue("hideNameplate", out JToken hideNameplateToken)) customHideNameplate.value = (bool)hideNameplateToken;
            }

            Updated(Enum.SelectedIndex);
        }

        public JToken Serialize()
        {
            if (Enum.SelectedValue == "Current") return null;
            if (Enum.SelectedValue != "Custom") return (JToken)Enum.SelectedValue;

            string voiceID = customVoiceID.SelectedValue != DialogueProfile.Default.ID
                ? customVoiceID.SelectedValue : null;
            float? textSpeed = customTextSpeed.value != DialogueProfile.Default.textSpeed
                ? customTextSpeed.value : null;

            if (voiceID == null && textSpeed == null && !customHideNameplate.value) return (JToken)customName.value;

            JObject res = new() { ["name"] = customName.value };
            if (customVoiceID.SelectedValue != DialogueProfile.Default.ID) res["voice"] = customVoiceID.SelectedValue;
            if (customTextSpeed.value != DialogueProfile.Default.textSpeed) res["speed"] = customTextSpeed.value;
            if (customHideNameplate.value) res["hideNameplate"] = true;
            return res;
        }

    }
}