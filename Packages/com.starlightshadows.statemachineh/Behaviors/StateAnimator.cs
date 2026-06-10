using System.Collections;
using System.Collections.Generic;
using SLS.EditorUtilities.ComponentHeaders;
using UnityEngine;

namespace SLS.StateMachineH
{
    /// <summary>  
    /// A behavior that manages animation states within a <see cref="StateMachine"/>.  
    /// </summary>  
    public class StateAnimator : StateBehavior
    {
        public AnimatorAction action = new();

        /// <summary>  
        /// Indicates whether the animation should be performed when the state is not final.  
        /// </summary>  
        public bool doWhenNotFinal;

        /// <summary>  
        /// The <see cref="Animator"/> component used to control animations.  
        /// </summary>  
        [field: SerializeField, HeaderItem(true, nameof(_GetAnim))] public Animator Animator { get; private set; }
        Animator _GetAnim() => GetComponentFromMachine<Animator>();

        /// <summary>  
        /// Sets up the <see cref="StateAnimator"/> by attempting to retrieve the <see cref="Animator"/> component.  
        /// </summary>  
        protected override void OnSetup()
        {
            Animator = GetComponentFromMachine<Animator>();
            if (Animator == null) Animator = Machine.gameObject.AddComponent<Animator>();
        }

        /// <summary>  
        /// Executes the animation action when entering a state.  
        /// </summary>  
        /// <param name="prev">The previous <see cref="State"/>.</param>  
        /// <param name="isFinal">Indicates if this is the final <see cref="State"/>.</param>  
        protected override void OnEnter(State prev, bool isFinal)
        {
            if (!isFinal && !doWhenNotFinal) return;
            action.Do(Animator);
        }

        #region Auxilary Alternatives
        /// <summary>  
        /// Plays the specified animation.  
        /// </summary>  
        /// <param name="name">The name of the animation to play.</param>  
        public void Play(string name) => Animator.Play(name);

        /// <summary>  
        /// Crossfades to the specified animation over a given duration.  
        /// </summary>  
        /// <param name="name">The name of the animation to crossfade to.</param>  
        /// <param name="time">The duration of the crossfade.</param>  
        public void CrossFade(string name, float time = 0f) => Animator.CrossFade(name, time, 0);

        /// <summary>  
        /// Triggers the specified animation.  
        /// </summary>  
        /// <param name="name">The name of the animation trigger.</param>  
        public void Trigger(string name) => Animator.SetTrigger(name);

        /// <summary>  
        /// Plays the specified animation starting at the current normalized time of the Animator.  
        /// </summary>  
        /// <param name="name">The name of the animation to play.</param>  
        public void PlayAtCurrentPoint(string name) => Animator.Play(name, -1, Animator.GetCurrentAnimatorStateInfo(-1).normalizedTime);

        /// <summary>  
        /// Crossfades to the specified animation starting at the current normalized time of the Animator.  
        /// </summary>  
        /// <param name="name">The name of the animation to crossfade to.</param>  
        /// <param name="time">The duration of the crossfade.</param>  
        public void CrossFadeAtCurrentPoint(string name, float time = 0f) => Animator.CrossFade(name, time, 0, Animator.GetCurrentAnimatorStateInfo(-1).normalizedTime);

        #endregion
    }
}