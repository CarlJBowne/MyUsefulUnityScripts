using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SLS.StateMachineH
{
    /// <summary>  
    /// A behavior that manages animation states within a <see cref="StateMachine"/>.  
    /// </summary>  
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceClassName: "StateAnimator")]
    public class StateAnimator_Legacy : StateBehavior
    {
        /// <summary>  
        /// Defines the type of animation action to perform when entering a state.  
        /// </summary>  
        public enum EntryAnimAction { None, Play, CrossFade, Trigger }

        /// <summary>  
        /// The animation action to perform when entering a state.  
        /// </summary>  
        public EntryAnimAction onEntry;

        /// <summary>  
        /// The name of the animation to play, crossfade, or trigger.  
        /// </summary>  
        public string onEnterName;

        /// <summary>  
        /// The duration of the crossfade animation.  
        /// </summary>  
        public float onEnterTime;

        /// <summary>  
        /// Indicates whether the animation should be performed when the state is not final.  
        /// </summary>  
        public bool doWhenNotFinal;

        /// <summary>  
        /// The <see cref="Animator"/> component used to control animations.  
        /// </summary>  
        [SerializeField] private Animator animator;
        public Animator Animator => animator;

        /// <summary>  
        /// Sets up the <see cref="StateAnimator_Legacy"/> by attempting to retrieve the <see cref="Animator"/> component.  
        /// </summary>  
        protected override void OnSetup()
        {
            TryGetComponentFromMachine(out animator);
            if (animator == null) Destroy(this);
        }

        /// <summary>  
        /// Executes the animation action when entering a state.  
        /// </summary>  
        /// <param name="prev">The previous <see cref="State"/>.</param>  
        /// <param name="isFinal">Indicates if this is the final <see cref="State"/>.</param>  
        protected override void OnEnter(State prev, bool isFinal)
        {
            if (!isFinal && !doWhenNotFinal) return;
            if (onEntry == EntryAnimAction.Play) Play(onEnterName);
            if (onEntry == EntryAnimAction.CrossFade) CrossFade(onEnterName, onEnterTime);
            if (onEntry == EntryAnimAction.Trigger) Trigger(onEnterName);
        }

        /// <summary>  
        /// Plays the specified animation.  
        /// </summary>  
        /// <param name="name">The name of the animation to play.</param>  
        public void Play(string name) => animator.Play(name);

        /// <summary>  
        /// Crossfades to the specified animation over a given duration.  
        /// </summary>  
        /// <param name="name">The name of the animation to crossfade to.</param>  
        /// <param name="time">The duration of the crossfade.</param>  
        public void CrossFade(string name, float time = 0f) => animator.CrossFade(name, time, 0);

        /// <summary>  
        /// Triggers the specified animation.  
        /// </summary>  
        /// <param name="name">The name of the animation trigger.</param>  
        public void Trigger(string name) => animator.SetTrigger(name);

        /// <summary>  
        /// Plays the specified animation starting at the current normalized time of the animator.  
        /// </summary>  
        /// <param name="name">The name of the animation to play.</param>  
        public void PlayAtCurrentPoint(string name) => animator.Play(name, -1, animator.GetCurrentAnimatorStateInfo(-1).normalizedTime);

        /// <summary>  
        /// Crossfades to the specified animation starting at the current normalized time of the animator.  
        /// </summary>  
        /// <param name="name">The name of the animation to crossfade to.</param>  
        /// <param name="time">The duration of the crossfade.</param>  
        public void CrossFadeAtCurrentPoint(string name, float time = 0f) => animator.CrossFade(name, time, 0, animator.GetCurrentAnimatorStateInfo(-1).normalizedTime);

        [ContextMenu("Transfer To New")]
        public void TransferToNew()
        {
            State state = this.State;
            StateAnimator New = state.gameObject.AddComponent<StateAnimator>();
            New.doWhenNotFinal = doWhenNotFinal;

            New.action.NameID = onEnterName;
            New.action.CacheID();
            if (onEntry == EntryAnimAction.Play) New.action.type = AnimatorAction.Type.Play;
            else if (onEntry == EntryAnimAction.CrossFade)
            {
                New.action.type = AnimatorAction.Type.CrossFade;
                New.action.floatValue1 = onEnterTime;
            }
            else if (onEntry == EntryAnimAction.Trigger)
            {
                New.action.type = AnimatorAction.Type.SetTrigger;
                New.action.boolValue = true;
            }

            DestroyImmediate(this);
        }
    }

}