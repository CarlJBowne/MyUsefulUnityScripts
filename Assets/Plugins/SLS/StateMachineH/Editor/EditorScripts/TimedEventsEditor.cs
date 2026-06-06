using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SLS.StateMachineH.Editor;
using SLS.StateMachineH.Timelines;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static SLS.StateMachineH.Timelines.TimedEvents;
using SLS.ListUtilities.Editor;
using SLS.EditorUtilities.Editor;

namespace SLS.StateMachineH.Timelines.Editor
{
    [CustomEditor(typeof(TimedEvents))]
    public class TimedEventsEditor : UnityEditor.Editor
    {
        SerializedProperty eventsListProperty;
        Label emptyLabel;
        VisualElement root;
        ListDrawer list;

        private string noEventsHelpBoxText = "No timed events have been added. This system will not work without at least one. Click the + button to add one.";

        public override VisualElement CreateInspectorGUI()
        {
            root = new();

            serializedObject.Update();
            eventsListProperty = serializedObject.FindProperty(nameof(TimedEvents.events));
            list = new ListDrawer(eventsListProperty).AddTo(root);
            list.PostBuildAction += () => emptyLabel.visible = list.items.Count == 0;

            emptyLabel = new Label(noEventsHelpBoxText)
            {
                style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        paddingLeft = 2,
                        paddingTop = 2,
                        paddingBottom = 2,
                    }
            }.AddTo(root);
            list.PostBuildAction();

            SerializedProperty currentLoopValue = serializedObject.FindProperty(nameof(TimedEvents.loopAfterLastEvent));
            Button loopButton = null;
            loopButton = new Button(UpdateLoop)
            {
                text = "Loop",
                style =
                    {
                        backgroundColor = currentLoopValue.boolValue ? Color.gray5 : Color.gray2,
                        color = currentLoopValue.boolValue ? Color.white : Color.gray4,
                        width = 80,
                        height = 18,
                    }
            }.AddTo(root);
            void UpdateLoop()
            {
                currentLoopValue.boolValue = !currentLoopValue.boolValue;
                loopButton.style.backgroundColor = currentLoopValue.boolValue ? Color.gray5 : Color.gray2;
                loopButton.style.color = currentLoopValue.boolValue ? Color.white : Color.gray4;
                serializedObject.ApplyModifiedProperties();
            }


            return root;
        }



        internal class ListDrawer : SuperList<ListDrawer, ItemDrawer, TimedEvent>
        {
            public ListDrawer(SerializedProperty listProperty) : base(listProperty)
            {
                BuildBasicElements();
                BindProperty(listProperty);
            }

            public override bool allowReorder => false;


            public override void SetOrCreateItemValue(int ID, object input = null)
            {
                base.SetOrCreateItemValue(ID, input);
                if (ID == 0) return;
                property.GetArrayElementAtIndex(ID).FindPropertyRelative("time").floatValue =
                    property.GetArrayElementAtIndex(ID - 1).FindPropertyRelative("time").floatValue + .001f;
                property.serializedObject.ApplyModifiedProperties();
            }
            //public override void CreateItemElement(int ID)
            //{
            //    if (property == null) throw new InvalidOperationException("Property is null");
            //    // Grab a fresh serialized property for this slot
            //    SerializedProperty elemProp = property.GetArrayElementAtIndex(ID) ?? throw new ArgumentOutOfRangeException/(nameof/(ID));
            //
            //    ItemDrawer holder = new(this as ListDrawer, elemProp);
            //
            //    items.Add(holder);
            //    collectionBackground.Add(holder);
            //
            //    // Bind the newly created element to the owner object so it displays immediately and reacts to changes.
            //    try { holder.Bind(property.serializedObject); } catch { }
            //}

            public void ReorderElements(int index, ChangeEvent<float> ev)
            {
                if (ev.previousValue == ev.newValue) return;
                int dest = index;
                if (ev.previousValue < ev.newValue)
                {
                    if (index == items.Count - 1) return;
                    for (; dest < items.Count - 1; dest++)
                    {
                        if (ev.newValue < items[index + 1].TimeProp.floatValue) break;
                    }
                    if (dest == index) return;
                }
                else
                {
                    if (ev.newValue < 0)
                    {
                        items[index].TimeProp.floatValue = 0;
                        items[index].TimeField.SetValueWithoutNotify(0);
                    }
                    if (index == 0) return;
                    for (; dest > 0; dest--)
                    {
                        if (ev.newValue > items[index - 1].TimeProp.floatValue) break;
                    }
                }

                if (dest != index)
                {
                    property.MoveArrayElement(index, dest);
                }

                property.serializedObject.ApplyModifiedProperties();
                BuildItems();
            }

            public override void BuildItems()
            {
                base.BuildItems();
                PostBuildAction?.Invoke();
            }

            public Action PostBuildAction;

        }
        internal class ItemDrawer : SuperListItem<ListDrawer, ItemDrawer, TimedEvent>
        {
            public ItemDrawer(ListDrawer parentList, int Index) : base(parentList, Index)
            {
            }

            public SerializedProperty TimeProp { get; private set; }
            public FloatField TimeField { get; private set; }
            public SerializedProperty EventProp { get; private set; }
            public PropertyField EventField { get; private set; }

            public override VisualElement Content()
            {
                TimeProp = property.FindPropertyRelative("time");
                EventProp = property.FindPropertyRelative("output");

                content = new VisualElement()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row
                    }
                };

                TimeField = new FloatField().AddTo(content, t =>
                {
                    t.label = "T:";
                    t.labelElement.style.flexGrow = 0;
                    t.labelElement.style.flexShrink = 1f;
                    t.labelElement.style.minWidth = 0;
                    t.style.maxHeight = EditorGUIUtility.singleLineHeight;
                    t.style.minWidth = 90;
                    t.style.maxWidth = 90;
                    t.isDelayed = true;
                    t.BindProperty(TimeProp);
                    ContextMenuTarget = t;
                    t.DelayedBuild(() => t.RegisterValueChangedCallback(ev => parent.ReorderElements(Index, ev)));
                });

                EventField = new PropertyField().AddTo(content, e =>
                {
                    e.BindProperty(EventProp);
                    e.style.flexGrow = 1f;
                });

                return content;
            }
        }
    }
}
