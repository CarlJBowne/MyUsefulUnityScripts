using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
#if ULT_EVENTS
using UltEvents;
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

namespace SLS.StateMachineH.Timelines
{
    public class TimedEvents : StateTimeline
    {
        [System.Serializable]
        public struct TimedEvent
        {
            public float time;
            public EVENT output;
        }
        public List<TimedEvent> events = new();
        public bool loopAfterLastEvent;

        int nextEventID = 0;

        protected override void OnSetup()
        {
            base.OnSetup();
#if UNITY_EDITOR
            if (events.Count == 0)
                Debug.LogWarning($"TimedEvents on State '{State.name}' in StateMachine '{Machine.name}' has no events configured and will not function.");
#endif
        }

        protected override void OnBegin()
        {
            elapsedTime = 0f;
            nextEventID = 0;
            if (events.Count > 0 && events[0].time == 0f)
            {
                events[0].output?.Invoke();
                nextEventID++;
            }
        }

        protected override void OnTick(float delta)
        {
            if (events.Count == 0) return;

            if (nextEventID < events.Count && WasPointPassed(events[nextEventID].time))
            {
                events[nextEventID].output?.Invoke();
                nextEventID++;
                if (nextEventID >= events.Count && loopAfterLastEvent)
                {
                    elapsedTime %= events[^1].time;
                    nextEventID = 0;
                }
            }

        }

#if UNITY_EDITOR
        [ContextMenu("Convert Animation Events")]
        void ConvertAnimationEvents()
        {
            //Show popup to get AnimationClip Input from user.
            string path = EditorUtility.OpenFilePanel("Select Animation Clip", "Assets\\Actors\\_Private\\Angus\\src\\Animations", "anim");
            if (string.IsNullOrEmpty(path)) return;
            path = "Assets" + path.Substring(Application.dataPath.Length);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) return;

            // Get the Animation Events from the clip
            AnimationEvent[] animationEvents = AnimationUtility.GetAnimationEvents(clip);
            if (animationEvents.Length == 0) return;

            int i = 0;
            bool[] converted = new bool[animationEvents.Length];

            // Convert each Animation Event to a TimedEvent
            foreach (var animEvent in animationEvents)
            {
                TimedEvent timedEvent = new()
                {
                    time = animEvent.time,
                    output = new()
                };

                if (animEvent.functionName == "FireSignalBasic" || animEvent.functionName == "FinishAction")
                {
                    TryGetComponent(out SLS.StateMachineH.Signals.SignalNode signal);
                    timedEvent.output = signal[animEvent.functionName == "FireSignalBasic" ? animEvent.stringParameter : "Finish"];
                    signal.signals.Remove(animEvent.functionName == "FireSignalBasic" ? animEvent.stringParameter : "Finish");
                    converted[i] = true;
                }

                if (animEvent.functionName == "Lock" || animEvent.functionName == "Unlock" || animEvent.functionName == "ReadyNextAction")
                {
                    TryGetComponentFromMachine(out SLS.StateMachineH.Signals.SignalManager signalManager);

                    UltEvent.AddPersistentCall(ref timedEvent.output,
                        animEvent.functionName == "Lock" ? signalManager.Lock
                        : signalManager.Unlock);
                    converted[i] = true;
                }

                // Add the TimedEvent to the TimedEvents component
                events.Add(timedEvent);
                i++;
            }
            //Remove animation events that have been converted.
            List<AnimationEvent> remainingEvents = new();
            for (int j = 0; j < animationEvents.Length; j++)
                if (!converted[j])
                    remainingEvents.Add(animationEvents[j]);
            AnimationUtility.SetAnimationEvents(clip, remainingEvents.ToArray());
        }
#endif
    }
}