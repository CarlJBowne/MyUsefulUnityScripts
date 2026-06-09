using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace SLS.StateMachineH
{
    [CustomEditor(typeof(State), false)]
    public class StateEditor : UnityEditor.Editor
    {
        protected VisualElement root;
        protected VisualElement primaryRow;
        protected VisualElement activeRow;
        protected VisualElement additions;
        protected State state;
        protected Label onOffLabel;
        protected Label childStateLabel;
        protected State childState;


        public override VisualElement CreateInspectorGUI()
        {
            state = (State)target;
            if (state.Machine == null) return new Label("This State is not attached to a State Machine.");

            root = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Column
                }
            };
            MakeVisual();
            return root;
        }

        void MakeVisual()
        {
            root.Clear();

            MakePrimaryRow();
            MakeActiveRow();
            MakeAdditionalRow(6);

            root.Bind(serializedObject);
            serializedObject.ApplyModifiedProperties();
        }

        public virtual void MakePrimaryRow()
        {
            primaryRow = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            root.Add(primaryRow);
            MakeMachineIcon();
            MakeBuildButton();
            MakeAttributeLabels();
            MakeAddButton();
        }
        public virtual void MakeMachineIcon()
        {
            Image icon = new()
            {
                image = StateMachine_EditorUtilities.GetTexture("StateMachine"),
                style =
                {
                    width = 18,
                    height = 18
                }
            };
            icon.RegisterCallback<ClickEvent>(ev =>
            {
                EditorGUIUtility.PingObject(state.Machine as Object);
                Selection.activeObject = state.Machine as Object;
            });
            icon.Highlighter(.2f);
            icon.SetCursor(MouseCursor.Orbit);

            primaryRow.Add(icon);
        }
        public virtual void MakeBuildButton()
        {
            Color backgroundColor = state.Machine.StatesSetup ? Color.darkGreen : Color.darkRed;
            Color textColor = backgroundColor.Up(.45f);
            Color outlineColor = backgroundColor.Up(.2f);
            Button buildButton = new(BuildButtonPress)
            {
                text = state.Machine.StatesSetup ? "Built" : "Build!",
                style =
                    {
                        color = textColor,
                        backgroundColor = backgroundColor,
                        borderBottomColor = outlineColor,
                        borderLeftColor = outlineColor,
                        borderRightColor = outlineColor,
                        borderTopColor = outlineColor,
                        height = 18,

                    }
            };
            buildButton.Highlighter(textColor.Up(.2f), backgroundColor.Up(.2f), outlineColor.Up(.4f));
            buildButton.SetCursor(MouseCursor.Link);

            primaryRow.Add(buildButton);

        }
        public virtual void BuildButtonPress()
        {
            if (Application.isPlaying) return;

            if (!state.Machine.StatesSetup)
            {
                state.Machine.Setup(state.Machine, state.Machine, -1, true);

                EditorSceneManager.MarkSceneDirty(state.gameObject.scene);

                EditorUtility.DisplayDialog("Setup Complete", "This State Machine has been setup, be sure to save changes to the prefab.", "Nice.");
            }
            else
            {
                state.Setup(state.Machine, state.Parent, state.Layer, true);

                EditorSceneManager.MarkSceneDirty(state.gameObject.scene);

                EditorUtility.DisplayDialog("Setup Complete", "This State's own Children and Behaviors have been updated.", "Nice.");

                MakeVisual();
            }
        }

        public virtual void MakeAddButton()
        {
            Color backgroundColor = new(.145f, .345f, .584f);
            Color textColor = Color.turquoise;
            Color outlineColor = backgroundColor.Up(-.1f);
            Button addButton = new(AddButtonPress)
            {
                text = "+",
                style =
                    {
                        color = textColor,
                        backgroundColor = backgroundColor,
                        borderBottomColor = outlineColor,
                        borderLeftColor = outlineColor,
                        borderRightColor = outlineColor,
                        borderTopColor = outlineColor,
                        height = 18,
                        right = -4,
                        position = Position.Absolute
                    }
            };
            addButton.Highlighter(textColor.Up(.3f), backgroundColor.Up(.2f), outlineColor.Up(-.3f));
            addButton.SetCursor(MouseCursor.Link);
            primaryRow.Add(addButton);

            void AddButtonPress()
            {
                state.AddChildNode();
                MakeVisual();
            }

        }
        public virtual void MakeAttributeLabels()
        {
            Label childrenLabel = new($"({state.ChildCount} Children)");
            Label behaviorsLabel = new($"({state.Behaviors.Length} Behaviors)");
            childrenLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            behaviorsLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            primaryRow.Add(childrenLabel);
            primaryRow.Add(behaviorsLabel);
        }


        public virtual void MakeActiveRow()
        {
            if (!Application.isPlaying) return;

            state.Machine.AfterStateTransition += UpdateActiveRow;

            activeRow = new();
            activeRow.style.flexDirection = FlexDirection.Row;
            root.Add(activeRow);
            onOffLabel = new()
            {
                style =
                {
                    minWidth = 12
                }
            };

            childStateLabel = new();
            childStateLabel.style.color = new Color(.2f, .4f, .8f);
            childStateLabel.Highlighter(-.1f);
            childStateLabel.RegisterCallback<ClickEvent>(ev =>
            {
                EditorGUIUtility.PingObject(childState as Object);
                Selection.activeObject = childState as Object;
            });

            activeRow.Add(onOffLabel);
            activeRow.Add(childStateLabel);
            childStateLabel.Highlighter(-.1f);
        }

        public virtual void UpdateActiveRow()
        {
            if (!Application.isPlaying) return;
            if (state == null) return;

            onOffLabel.text = state.Active ? "[ON]" : "[OFF]";
            onOffLabel.style.color = state.Active ? Color.green : Color.red;

            childState = state.CurrentChild;

            childState = state.CurrentChild;
            childStateLabel.visible = childState != null;
            childStateLabel.text = childState != null ? childState.name : "<none>";
        }

        public virtual void MakeAdditionalRow(float propsToMovePast = 8)
        {
            if (target.GetType() != typeof(StateMachine))
            {
                additions = new();
                additions.style.marginTop = 4;
                root.Add(additions);

                var iterator = serializedObject.GetIterator();
                if (iterator.NextVisible(true))
                {
                    // Skip the next 8 properties to match original behavior (skip total of 9)
                    for (int i = 0; i < propsToMovePast; i++)
                    {
                        if (!iterator.NextVisible(false)) break;
                    }

                    while (iterator.NextVisible(false))
                    {
                        var copy = iterator.Copy();
                        var field = new PropertyField(copy);
                        field.Bind(serializedObject);
                        additions.Add(field);
                    }
                }
            }

        }

        private void OnDisable() => state.Machine.AfterStateTransition -= UpdateActiveRow;


        public override void OnInspectorGUI()
        {
            Rect position = EditorGUILayout.GetControlRect();
            float startY = position.y;

            StateMachine_EditorUtilities.DrawStateNode(ref position, (State)target, serializedObject, true);

            if (Application.isPlaying) StateMachine_EditorUtilities.DrawActiveStateMachineDetails(ref position, (State)target);

            GUILayout.Space(position.yMax - startY);
        }
    }

    [CustomEditor(typeof(StateMachine), true)]
    public class StateMachineEditor : StateEditor
    {
        StateMachine stateMachine;
        Label arrowLabel;
        Label finalStateLabel;
        State finalState;


        public override VisualElement CreateInspectorGUI()
        {
            state = (State)target;
            root = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Column
                }
            };
            stateMachine = (StateMachine)target;
            MakeVisual();
            return root;
        }

        void MakeVisual()
        {
            root.Clear();

            MakePrimaryRow();
            MakeActiveRow();
            MakeAdditionalRow();

            root.Bind(serializedObject);
            serializedObject.ApplyModifiedProperties();
        }

        public override void MakePrimaryRow()
        {
            primaryRow = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            root.Add(primaryRow);
            MakeBuildButton();
            MakeAttributeLabels();
            MakeAddButton();
        }

        public override void BuildButtonPress()
        {
            if (Application.isPlaying) return;

            if (stateMachine.StatesSetup)
            {
                bool answer = EditorUtility.DisplayDialog("Setup", "This State Machine has already been setup, do you still want to setup again?", "Yes", "No");
                if (!answer) return;
            }

            stateMachine.Setup(stateMachine, stateMachine, -1, true);

            EditorSceneManager.MarkSceneDirty(stateMachine.gameObject.scene);

            EditorUtility.DisplayDialog("Setup Complete", "This State Machine has been setup, be sure to save changes to the prefab.", "Nice.");

            MakeVisual();
        }

        public override void MakeActiveRow()
        {
            if (!Application.isPlaying) return;
            if (stateMachine == null) return;

            stateMachine.AfterStateTransition += UpdateActiveRow;

            activeRow = new();
            activeRow.style.flexDirection = FlexDirection.Row;
            root.Add(activeRow);
            onOffLabel = new()
            {
                style =
                {
                    minWidth = 12
                }
            };

            childStateLabel = new();
            childStateLabel.style.color = new Color(.2f, .4f, .8f);
            childStateLabel.Highlighter(-.1f);
            childStateLabel.RegisterCallback<ClickEvent>(ev =>
            {
                if (childState != null)
                {
                    EditorGUIUtility.PingObject(childState as Object);
                    Selection.activeObject = childState as Object;
                }
            });

            arrowLabel = new();
            arrowLabel.visible = false;

            finalStateLabel = new();
            finalStateLabel.style.color = new Color(.2f, .4f, .8f);
            finalStateLabel.Highlighter(-.1f);
            finalStateLabel.RegisterCallback<ClickEvent>(ev =>
            {
                if (finalState != null)
                {
                    EditorGUIUtility.PingObject(finalState as Object);
                    Selection.activeObject = finalState as Object;
                }
            });
            finalStateLabel.visible = false;

            activeRow.Add(onOffLabel);
            activeRow.Add(childStateLabel);
            activeRow.Add(arrowLabel);
            activeRow.Add(finalStateLabel);
            UpdateActiveRow();
        }

        public override void UpdateActiveRow()
        {
            if (!Application.isPlaying) return;
            if (stateMachine == null) return;

            if (onOffLabel != null)
            {
                onOffLabel.text = stateMachine.Active ? "[ON]" : "[OFF]";
                onOffLabel.style.color = stateMachine.Active ? Color.green : Color.red;
            }

            childState = stateMachine.CurrentChild;
            childStateLabel.visible = childState != null;
            childStateLabel.text = childState != null ? childState.name : "<none>";

            finalState = stateMachine.CurrentState;
            finalStateLabel.text = finalState != null ? finalState.name : "<none>";

            if (finalState != null && childState != null && finalState != childState)
            {
                arrowLabel.visible = true;
                finalStateLabel.visible = true;
                if (stateMachine.CurrentState != null)
                    arrowLabel.text = $" {new string('-', Mathf.Max(0, stateMachine.CurrentState.Layer - 1))}>";
            }
            else
            {
                arrowLabel.visible = false;
                finalStateLabel.visible = false;
            }
        }

        private void OnDisable()
        {
            if (stateMachine != null)
                stateMachine.AfterStateTransition -= UpdateActiveRow;
        }

        public VisualElement CreateInspectorGUIO()
        {
            var so = serializedObject;
            so.Update();

            var root = new VisualElement();
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;

            var stateMachine = (StateMachine)target;

            // Header (State node) without machine icon (false)
            //var header = StateMachine_EditorUtilities.MakeStateNodeVisual(stateMachine, so, false);
            //root.Add(header);

            // Active details when playing
            if (Application.isPlaying)
            {
                //var activeDetails = StateMachine_EditorUtilities.MakeActiveStateMachineDetails(stateMachine);
                //root.Add(activeDetails);
            }
            else
            {
                // Setup button block
                var container = new VisualElement();
                container.style.marginTop = 6;
                var setupButton = new Button(() =>
                {
                    if (stateMachine.StatesSetup)
                    {
                        bool answer = EditorUtility.DisplayDialog("Setup", "This State Machine has already been setup, do you still want to setup again?", "Yes", "No");
                        if (!answer) return;
                    }

                    stateMachine.Setup(stateMachine, stateMachine, -1, true);

                    EditorSceneManager.MarkSceneDirty(stateMachine.gameObject.scene);

                    EditorUtility.DisplayDialog("Setup Complete", "This State Machine has been setup, be sure to save changes to the prefab.", "Nice.");
                })
                {
                    text = stateMachine.StatesSetup ? "State Machine Tree Built!" : "State Machine Tree needs Building!"
                };

                // Color styling (best-effort)
                if (stateMachine.StatesSetup)
                    setupButton.style.backgroundColor = new StyleColor(new Color(.9f, 1.1f, .9f));
                else
                    setupButton.style.backgroundColor = new StyleColor(new Color(1f, .7f, .7f));

                setupButton.style.height = EditorGUIUtility.singleLineHeight + 8;
                container.Add(setupButton);
                root.Add(container);
            }

            // Space before other fields
            root.Add(new VisualElement { style = { height = 6 } });

            // If the actual inspector target is a derived type, show its unique fields (skip base fields)
            if (target.GetType() != typeof(StateMachine))
            {
                var iterator = so.GetIterator();
                if (iterator.NextVisible(true))
                {
                    // Skip the next 8 properties to match original behavior (skip total of 9)
                    for (int i = 0; i < 8; i++)
                    {
                        if (!iterator.NextVisible(false)) break;
                    }

                    while (iterator.NextVisible(false))
                    {
                        var copy = iterator.Copy();
                        var field = new PropertyField(copy);
                        field.Bind(so);
                        root.Add(field);
                    }
                }
            }

            root.Bind(so);
            so.ApplyModifiedProperties();

            return root;
        }

        public override void OnInspectorGUI()
        {
            var stateMachine = (StateMachine)target;

            Rect position = EditorGUILayout.GetControlRect();
            float startY = position.y;

            StateMachine_EditorUtilities.DrawStateNode(ref position, (State)target, serializedObject, false);

            if (Application.isPlaying)
                StateMachine_EditorUtilities.DrawActiveStateMachineDetails(ref position, (State)target);
            else
            {
                position.height += 15;

                GUI.color = stateMachine.StatesSetup ? new(.9f, 1.1f, .9f) : new(1, .7f, .7f);
                if (GUI.Button(position, stateMachine.StatesSetup ? "State Machine Tree Built!" : "State Machine Tree needs Building!"))
                {
                    if (stateMachine.StatesSetup)
                    {
                        bool answer = EditorUtility.DisplayDialog("Setup", "This State Machine has already been setup, do you still want to setup again?", "Yes", "No");
                        if (!answer) return;
                    }

                    stateMachine.Setup(stateMachine, stateMachine, -1, true);

                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(stateMachine.gameObject.scene);

                    EditorUtility.DisplayDialog("Setup Complete", "This State Machine has been setup, be sure to save changes to the prefab.", "Nice.");
                }
                GUI.color = Color.white;
                position.y = position.yMax;
            }

            GUILayout.Space(position.yMax - startY + 5);

            if (target.GetType() != typeof(StateMachine))
            {
                // Display additional fields unique to derived classes  
                SerializedProperty property = serializedObject.GetIterator();
                property.NextVisible(true);

                // Skip the first 9 properties (Script, Name, Layer, Active, CurrentChild, Type, HasChildren, Behaviors, ChildCount)
                for (int i = 0; i < 8; i++)
                    if (!property.NextVisible(false))
                        break;

                while (property.NextVisible(false))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(property, true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

        }
    }

    public static class StateMachine_EditorUtilities
    {
        public static void DrawScriptClicker<T>(Object target, Rect position) where T : MonoBehaviour
        {
            GUI.enabled = false;
            EditorGUI.ObjectField(position, "Script", MonoScript.FromMonoBehaviour((T)target), typeof(T), false);
            GUI.enabled = true;
        }

        public static string PluginsPath = "Assets/Plugins/HierarchyStateMachine/";
        public static string PackagesPath = "Packages/com.starlightshadows.hierarchystatemachine/";
        public static string IconsPath = "Editor/Icons/";

        public static Texture GetTexture(string name)
        {
            Texture result;

            result = AssetDatabase.LoadAssetAtPath<Texture>($"{PackagesPath}{IconsPath}{name}.png");
            if (result == null) result = AssetDatabase.LoadAssetAtPath<Texture>($"{PluginsPath}{IconsPath}{name}.png");

            return result;
        }

        public static void LinkToObject(this Rect rect, Object target)
        {
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.PingObject(target as Object);
                Selection.activeObject = target as Object;
            }
        }

        public static void DrawStateNode(ref Rect position, State target, SerializedObject serializedObject, bool showMachineIcon = true)
        {
            if ((target as State).Machine == null)
            {
                EditorGUI.LabelField(position, "This State is not connected to a StateMachine.");
                return;
            }


            float iconSize = EditorGUIUtility.singleLineHeight;
            float spacing = 4f;

            // Script Icon  
            Rect scriptRect = new(position.x, position.y, iconSize, iconSize);
            Texture monoIcon = EditorGUIUtility.IconContent("cs Script Icon").image;
            GUI.DrawTexture(scriptRect, monoIcon, ScaleMode.ScaleToFit);
            EditorGUIUtility.AddCursorRect(scriptRect, MouseCursor.Link);
            if (Event.current.type == EventType.MouseDown && scriptRect.Contains(Event.current.mousePosition))
                EditorGUIUtility.PingObject(MonoScript.FromMonoBehaviour((MonoBehaviour)target));

            // Machine Icon  
            Rect machineRect = new(scriptRect.xMax + spacing, position.y, iconSize, iconSize);
            if (showMachineIcon)
            {
                StateMachine machine = target is StateMachine ? target as StateMachine : (target as State)?.Machine;
                if (machine != null)
                {
                    Texture machineIcon = EditorGUIUtility.GetIconForObject(machine);
                    if (machineIcon == null) machineIcon = GetTexture("StateMachine");

                    GUI.DrawTexture(machineRect, machineIcon, ScaleMode.ScaleToFit);
                    machineRect.LinkToObject(machine);
                }
            }


            bool showWarningIcon = !((State)target).Machine.StatesSetup;
            // Warning Icon  
            Rect warningRect = new(machineRect.xMax + spacing, position.y, iconSize, iconSize);
            if (showWarningIcon && target is State stateChild && stateChild.Machine != null && !stateChild.Machine.StatesSetup)
            {
                GUIContent warning = new("!!!", "This State Machine's Tree either has not been set up or has been labeled Dirty!");
                GUI.color = Color.red;
                GUI.Label(warningRect, warning);
                GUI.color = Color.white;
                warningRect.LinkToObject(stateChild.Machine);
            }

            // (X Children) Text  
            Rect childrenRect = new(warningRect.xMax + spacing, position.y, GUI.skin.label.CalcSize(new GUIContent($"({target.ChildCount} Children)")).x, iconSize);
            GUI.Label(childrenRect, $"({target.ChildCount} Children)");

            // (X Behaviors) Text  
            Rect behaviorsRect = new(childrenRect.xMax + spacing, position.y, GUI.skin.label.CalcSize(new GUIContent($"({target.Behaviors?.Length ?? 0} Behaviors)")).x, iconSize);
            GUI.Label(behaviorsRect, $"({target.Behaviors?.Length ?? 0} Behaviors)");

            // Add Child Button
            float buttonWidth = EditorGUIUtility.singleLineHeight * 1.5f;
            Rect addChildButtonRect = new(position.xMax - buttonWidth, position.y, buttonWidth, iconSize);
            GUI.color = new(.341f, .706f, 1.141f);
            if (GUI.Button(addChildButtonRect, "+"))
            {
                State newNode = target.AddChildNode();
                RenameAfterCreate(newNode.gameObject);
            }
            GUI.color = Color.white;
            position.y += position.yMax;
        }

        async static void RenameAfterCreate(GameObject select)
        {
            Selection.activeObject = select;
            await Task.Delay(20);
            EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
            EditorApplication.ExecuteMenuItem("Edit/Rename");
        }

        internal static Color Up(this Color c, float a)
        {
            float r = Mathf.Clamp01(c.r + a);
            float g = Mathf.Clamp01(c.g + a);
            float b = Mathf.Clamp01(c.b + a);
            return new Color(r, g, b, c.a);
        }

        internal static void Highlighter(this VisualElement V, Color highlightColor, Color? backgroundHighlightColor = null, Color? borderHighlightColor = null)
        {
            Color initialColor = V.style.color.value;
            Color initialColorBack = V.style.backgroundColor.value;
            Color initialColorBorder = V.style.borderTopColor.value;

            V.RegisterCallback<MouseOverEvent>(Hover);
            V.RegisterCallback<MouseLeaveEvent>(UnHover);

            void Hover(MouseOverEvent E)
            {
                V.style.color = highlightColor;
                if (backgroundHighlightColor != null) V.style.backgroundColor = backgroundHighlightColor.Value;
                if (borderHighlightColor != null)
                {
                    V.style.borderTopColor = borderHighlightColor.Value;
                    V.style.borderBottomColor = borderHighlightColor.Value;
                    V.style.borderLeftColor = borderHighlightColor.Value;
                    V.style.borderRightColor = borderHighlightColor.Value;
                }
            }
            void UnHover(MouseLeaveEvent E)
            {
                V.style.color = initialColor;
                if (backgroundHighlightColor != null) V.style.backgroundColor = initialColorBack;
                if (borderHighlightColor != null)
                {
                    V.style.borderTopColor = initialColorBorder;
                    V.style.borderBottomColor = initialColorBorder;
                    V.style.borderLeftColor = initialColorBorder;
                    V.style.borderRightColor = initialColorBorder;
                }
            }
        }
        internal static void Highlighter(this VisualElement V, float factor = .3f)
        {
            Color initialColor = V.style.color.value;

            V.RegisterCallback<MouseOverEvent>(Hover);
            V.RegisterCallback<MouseLeaveEvent>(UnHover);

            void Hover(MouseOverEvent E) => V.style.color = new Color(initialColor.r + factor, initialColor.g + factor, initialColor.b + factor);
            void UnHover(MouseLeaveEvent E) => V.style.color = initialColor;
        }


        public static void DrawActiveStateMachineDetails(ref Rect position, State target)
        {
            float iconSize = EditorGUIUtility.singleLineHeight;
            float spacing = 4f;

            // Active Status Icon  
            Rect statusRect = new(position.x, position.y, GUI.skin.label.CalcSize(new("[OFF]")).x, iconSize);
            GUIContent statusContent = new(target.Active ? "[ON]" : "[OFF]", target.Active ? "StateMachine is Active" : "StateMachine is Inactive");
            GUI.color = target.Active ? Color.green : Color.red;
            GUI.Label(statusRect, statusContent);
            GUI.color = Color.white;

            // Active Child Details  
            Rect childRect = new(statusRect.xMax + spacing, position.y, position.width - statusRect.width - spacing, iconSize);
            if (target.HasChildren && target.CurrentChild != null)
            {
                GUIContent markerContent = new("Active: ");
                childRect.width = GUI.skin.label.CalcSize(markerContent).x;
                GUI.Label(childRect, markerContent);

                childRect.x = childRect.xMax;
                GUIContent directContent = new(target.CurrentChild.name, "Click to focus on the active child.");
                childRect.width = GUI.skin.label.CalcSize(directContent).x;

                childRect.LinkToObject(target.CurrentChild);
                GUI.Label(childRect, directContent, EditorStyles.linkLabel);

                if (target is StateMachine Machine && Machine.CurrentChild != null)
                {
                    childRect.x = childRect.xMax;
                    GUIContent arrowContent = new($" {new string('-', Machine.CurrentState.Layer)}> ");
                    childRect.width = GUI.skin.label.CalcSize(arrowContent).x;
                    GUI.Label(childRect, arrowContent);

                    childRect.x = childRect.xMax;
                    GUIContent finalContent = new(Machine.CurrentState.name, "Click to focus on the active state.");
                    childRect.width = GUI.skin.label.CalcSize(finalContent).x;

                    childRect.LinkToObject(Machine.CurrentState);
                    GUI.Label(childRect, finalContent, EditorStyles.linkLabel);
                }


            }
            position.y += position.yMax + 12;
        }
        internal static void SetCursor(this VisualElement element, MouseCursor cursor)
        {
            object objCursor = new UnityEngine.UIElements.Cursor();
            PropertyInfo fields = typeof(UnityEngine.UIElements.Cursor)
                .GetProperty("defaultCursorId", BindingFlags.NonPublic | BindingFlags.Instance);
            fields.SetValue(objCursor, (int)cursor);
            element.style.cursor = new StyleCursor((UnityEngine.UIElements.Cursor)objCursor);
        }

    }

}