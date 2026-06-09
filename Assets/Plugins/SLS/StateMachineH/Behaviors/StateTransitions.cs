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
        [SerializeField] public State TargetState { get; private set; }
        [SerializeField] public bool TransitionAtEnd { get; private set; }
        [SerializeField] public EVENT BeginEvent { get; private set; }
        [SerializeField] public float Length { get; private set; }
        [SerializeField] public EVENT EndEvent { get; private set; }
        [SerializeField] public AnimatorAction Animation { get; private set; }
    }

    public List<Transition> transitions = new();
    [SerializeField, HeaderItem(true, nameof(_GetAnim))] public Animator Animator { get; private set; }
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
            //Disable StateAnimator on Target once disabling is implemented
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

}
