using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueEditor : GraphViewEditorWindow
{
    public static DialogueEditor WindowInstance { get; private set; }

    DialogueAsset selectedAsset = null;

    VisualElement headerBar;
    ObjectField assetField;
    Button saveDataButton;

    DialogueGraphView graphView;
    List<DialoguePiece> pieces = new();
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

        headerBar = new Toolbar();

        var assetField = new ObjectField()
        {
            objectType = typeof(DialogueAsset),
            allowSceneObjects = false,
            style = { minWidth = 200 }
        };
        assetField.RegisterValueChangedCallback(UpdateSelectedAsset);

        saveDataButton = new Button(SaveSelectedAsset)
        {
            text = "Save",
            style =
            {
                position = Position.Absolute,
                right = -1
            }
        };

        headerBar.Add(assetField);
        headerBar.Add(saveDataButton);

        rootVisualElement.Add(headerBar);
         
        graphView = new DialogueGraphView()
        {
            name = "DialogueGraphView",
            style =
            {
                backgroundColor = Color.gray1,
                top = EditorGUIUtility.singleLineHeight + 2,
                flexGrow = 1,
            }
        };

        rootVisualElement.Add(graphView);

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
        ClearGraph();
        JArray Data = JArray.Parse(selectedAsset.rawData);
        for (int i = 0; i < Data.Count; i++)
        {
            pieces.Add(DialoguePiece.Parse(Data[i]));
            nodes.Add(DialoguePieceNode.Create(pieces[i], i));
            graphView.AddElement(nodes[i]);
        }
    }

    void ClearGraph()
    {
        nodes.Clear();
        pieces.Clear();
        graphView.Clear();
    }


    void SaveSelectedAsset()
    {
        if (selectedAsset == null)
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Dialogue Asset", "NewDialogueAsset", "asset", "Create a new DialogueAsset");
            if (string.IsNullOrEmpty(path)) return;
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            assetField.value = asset;
            return;
        }

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
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }

        public void PopulateFromAsset(DialogueAsset asset)
        {
            // Clear old elements
            graphViewChanged?.Invoke(new GraphViewChange());
            this.DeleteElements(this.nodes.ToList());

            if (asset?.Value == null) return;

            int count = asset.Value.Count;
            for (int i = 0; i < count; i++)
            {
                var token = asset.Value[i];
                DialoguePiece piece;
                try { piece = DialoguePiece.Parse(token); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse dialogue piece at index {i}: {ex.Message}");
                    continue;
                }

                var node = CreateNodeForPiece(piece, i);
                // Position nodes in a simple vertical list
                node.SetPosition(new Rect(50, 50 + i * 150, nodeWidth, 120));
                AddElement(node);
            }
        }

        Node CreateNodeForPiece(DialoguePiece piece, int index)
        {
            var node = new Node() { title = GetTitle(piece, index) };
            var main = node.mainContainer;

            // Add a small label describing the piece
            var label = new Label(GetBodyText(piece));
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexWrap = Wrap.Wrap;
            main.Add(label);

            // Add index in the footer
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexStart;
            footer.style.marginTop = 6;
            footer.Add(new Label($"Index: {index}"));
            node.extensionContainer.Add(footer);

            node.capabilities |= Capabilities.Movable | Capabilities.Selectable;
            return node;
        }

        static string GetTitle(DialoguePiece piece, int index)
        {
            if (piece is DialogueSegment seg)
            {
                var name = seg.profile?.displayName ?? seg.profile?.ToString() ?? "";
                if (string.IsNullOrEmpty(name) && seg.profile != null)
                    name = seg.profile.displayName;
                var shortText = Truncate(seg.text, 36);
                return string.IsNullOrEmpty(name) ? shortText : $"{name}: {shortText}";
            }
            if (piece is DialogueActions)
                return "Action";
            return "DialoguePiece";
        }

        static string GetBodyText(DialoguePiece piece)
        {
            if (piece is DialogueSegment seg)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(seg.text ?? "");
                if (seg.postActions != null)
                {
                    sb.AppendLine();
                    sb.Append("Actions: ");
                    sb.Append(string.Join(", ", seg.postActions.actions.Keys.Select(t => t.Name)));
                }
                return sb.ToString();
            }
            if (piece is DialogueActions acts)
            {
                return string.Join(", ", acts.actions.Keys.Select(t => t.Name));
            }
            return piece?.ToString() ?? string.Empty;
        }

        static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= max ? text : text.Substring(0, max - 1) + "…";
        }
    }

    public abstract class DialoguePieceNode : Node
    {
        public static DialoguePieceNode Create(DialoguePiece piece, int index)
        {
            if (piece is DialogueSegment seg)
                return new DialogueSegmentNode(seg, index);
            if (piece is DialogueActions acts)
                return new DialogueActionsNode(acts, index);
            throw new ArgumentException("Unsupported DialoguePiece type for node creation.");
        }
    }
    public class DialogueSegmentNode : DialoguePieceNode
    {
        DialogueSegment segment;
        public DialogueSegmentNode(DialogueSegment segment, int index)
        {
            this.segment = segment;
            title = $"Segment {index}";
            var main = this.mainContainer;
            var label = new Label(segment.text ?? "");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexWrap = Wrap.Wrap;
            main.Add(label);
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexStart;
            footer.style.marginTop = 6;
            footer.Add(new Label($"Index: {index}"));
            this.extensionContainer.Add(footer);
            capabilities |= Capabilities.Movable | Capabilities.Selectable;
        }
    }
    public class DialogueActionsNode : DialoguePieceNode
    {
        DialogueActions actions;
        public DialogueActionsNode(DialogueActions actions, int index)
        {
            this.actions = actions;
            title = $"Actions {index}";
            var main = this.mainContainer;
            var label = new Label(string.Join(", ", actions.actions.Keys.Select(t => t.Name)));
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexWrap = Wrap.Wrap;
            main.Add(label);
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexStart;
            footer.style.marginTop = 6;
            footer.Add(new Label($"Index: {index}"));
            this.extensionContainer.Add(footer);
            capabilities |= Capabilities.Movable | Capabilities.Selectable;
        }

    }
    public class DialogueProfileNode : Node
    {
        public DialogueProfileNode(DialogueProfile profile, int index)
        {
            title = $"Profile {index}";
            var main = this.mainContainer;
            var label = new Label(profile.displayName ?? profile.ToString() ?? "");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexWrap = Wrap.Wrap;
            main.Add(label);
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexStart;
            footer.style.marginTop = 6;
            footer.Add(new Label($"Index: {index}"));
            this.extensionContainer.Add(footer);
            capabilities |= Capabilities.Movable | Capabilities.Selectable;
        }
    }
}