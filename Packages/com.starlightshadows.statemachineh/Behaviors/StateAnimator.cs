using System.Collections;
using System.Collections.Generic;
using SLS.EditorUtilities.ComponentHeaders;
using UnityEngine;

namespace SLS.StateMachineUltimate
{
    /// <summary>  
    /// A behavior that manages animation states within a <see cref="StateMachine"/>.  
    /// </summary>  
    public class StateAnimator : StateBehavior<Animator>
    {
        public AnimatorAction action = new();

        /// <summary>  
        /// Indicates whether the animation should be performed when the state is not final.  
        /// </summary>  
        public bool doWhenNotFinal;


        /// <summary>  
        /// Executes the animation action when entering a state.  
        /// </summary>  
        /// <param name="prev">The previous <see cref="State"/>.</param>  
        /// <param name="isFinal">Indicates if this is the final <see cref="State"/>.</param>  
        protected override void OnEnter(State prev, bool isFinal)
        {
            if (!isFinal && !doWhenNotFinal) return;
            action.Do(CoreComp);
        }

        #region Auxilary Alternatives
        /// <summary>  
        /// Plays the specified animation.  
        /// </summary>  
        /// <param name="name">The name of the animation to play.</param>  
        public void Play(string name) => CoreComp.Play(name);

        /// <summary>  
        /// Crossfades to the specified animation over a given duration.  
        /// </summary>  
        /// <param name="name">The name of the animation to crossfade to.</param>  
        /// <param name="time">The duration of the crossfade.</param>  
        public void CrossFade(string name, float time = 0f) => CoreComp.CrossFade(name, time, 0);

        /// <summary>  
        /// Triggers the specified animation.  
        /// </summary>  
        /// <param name="name">The name of the animation trigger.</param>  
        public void Trigger(string name) => CoreComp.SetTrigger(name);

        /// <summary>  
        /// Plays the specified animation starting at the current normalized time of the Animator.  
        /// </summary>  
        /// <param name="name">The name of the animation to play.</param>  
        public void PlayAtCurrentPoint(string name) => CoreComp.Play(name, -1, CoreComp.GetCurrentAnimatorStateInfo(-1).normalizedTime);

        /// <summary>  
        /// Crossfades to the specified animation starting at the current normalized time of the Animator.  
        /// </summary>  
        /// <param name="name">The name of the animation to crossfade to.</param>  
        /// <param name="time">The duration of the crossfade.</param>  
        public void CrossFadeAtCurrentPoint(string name, float time = 0f) => CoreComp.CrossFade(name, time, 0, CoreComp.GetCurrentAnimatorStateInfo(-1).normalizedTime);

        #endregion
    }
}