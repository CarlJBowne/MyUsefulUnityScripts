using System;
using System.Collections.Generic;
using SLS.EditorUtilities.ComponentHeaders;
using UnityEngine;

namespace SLS.StateMachineH.Timelines
{
    /// <summary>
    /// A <see cref="StateBehavior"/> for Behaviors that are meant to operate differently over a set time.
    /// <br/> Requires a <see cref="StateTimelineManager"/> on the <see cref="StateMachine"/> to function.
    /// </summary>
    [RequireComponent(typeof(State))]
    public class StateTimeline : StateBehavior
    {
        [SerializeField, HeaderItem(true, nameof(_GetMan))] protected StateTimelineManager timeline;
        public StateTimelineManager Manager => timeline;
        StateTimelineManager _GetMan() => GetComponentFromMachine<StateTimelineManager>();

        protected override void Reset()
        {
            base.Reset();
#if UNITY_EDITOR
            if(!State.Machine.TryGetComponent(out timeline))
            {
                timeline = State.Machine.gameObject.AddComponent<StateTimelineManager>();
                UnityEditor.EditorUtility.SetDirty(timeline);
            }
#endif
        }

        protected override void OnSetup()
        {
            if(!TryGetComponentFromMachine(out timeline)) 
                DestroyImmediate(this);
        }

        protected override void OnEnter(State prev, bool isFinal) => Begin();
        protected override void OnExit(State next) => End();

        protected float elapsedTime = 0f;
        protected float previousTickTime = 0f;


        public void Begin()
        {
            if (timeline.activeBehaviors.Contains(this)) return;
            timeline.activeBehaviors.Insert(0, this);
            elapsedTime = 0f;
            OnBegin();
        }
        public void Tick(float delta)
        {
            previousTickTime = elapsedTime;
            elapsedTime += delta;
            OnTick(delta);
        }
        public void End()
        {
            if (!timeline.activeBehaviors.Contains(this)) return;
            timeline.activeBehaviors.Remove(this);
            OnEnd();
        }
        protected bool WasPointPassed(float time) => previousTickTime < time && elapsedTime >= time;

        protected virtual void OnBegin() { }
        protected virtual void OnTick(float delta) { }
        protected virtual void OnEnd() { }
    }
}
