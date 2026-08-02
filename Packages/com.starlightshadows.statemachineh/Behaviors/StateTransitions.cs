using System.Collections.Generic;
using SLS.EditorUtilities.ComponentHeaders;
using SLS.StateMachineH;
using SLS.StateMachineH.Timelines;
using UnityEngine;

#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

public class StateTransitions : StateTimeline
{
    [System.Serializable]
    public class Transition
    {
        public State TargetState;
        public bool TransitionAtEnd;
        public EVENT BeginEvent;
        public float Length;
        public EVENT EndEvent;
        public AnimatorAction Animation;
    }

    public List<Transition> transitions = new();
    [field: SerializeField, HeaderItem(true, nameof(_GetAnim))] public Animator Animator { get; private set; }
    Animator _GetAnim() => GetComponentFromMachine<Animator>();

    Transition activeTransition = null;
    float timer = 0f;

    public Transition this[int i] => transitions[i];
    public void FireTransition(int i)
    {
        activeTransition = this[i];
        if (activeTransition == null) return;
        Begin();
    }
    protected override void OnBegin()
    {
        if (!activeTransition.TransitionAtEnd && activeTransition.TargetState != null)
            activeTransition.TargetState.Enter();

        activeTransition.BeginEvent.Invoke();

        if (activeTransition.Animation != null && activeTransition.Animation.type is not AnimatorAction.Type.Null)
        {
            activeTransition.Animation.Do(Animator);
            if (activeTransition.TargetState.TryGetComponent(out StateAnimator animator)) animator.BlockForThisCycle();

        }

        if(activeTransition.Length <= 0f) End();
    }
    protected override void OnTick(float delta)
    {
        timer += delta;
        if (timer >= activeTransition.Length) End();
    }
    protected override void OnEnd()
    {
        if (activeTransition.TransitionAtEnd && activeTransition.TargetState != null) 
            activeTransition.TargetState.Enter();
        activeTransition.EndEvent.Invoke();
        activeTransition = null;
        timer = -1f;
    }

    protected override void OnEnter(State prev, bool isFinal) { }
}
