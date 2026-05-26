using System.Collections;
using System.Collections.Generic;
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
            action.Do(animator);
        }

        #region Auxilary Alternatives
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

        #endregion
    }

    [System.Serializable]
    public class AnimatorAction
    {
        public enum Type
        {
            Play,
            PlayAtPoint,
            PlaySynced,
            CrossFade,
            CrossFadeAtPoint,
            CrossFadeSynced,
            SetTrigger,
            SetBool,
            SetFloat,
            SetInt,
            Null,
        }
        public Type type;
        public string NameID;
        public int cachedHash = -1;
        public int layer = -1;

        public float floatValue1;
        public float floatValue2;
        public int intValue;
        public bool boolValue;

        public void Do(Animator animator)
        {
            if (cachedHash == -1) CacheID();
            switch (type)
            {
                case Type.Play:
                    animator.Play(cachedHash);
                    break;
                case Type.PlayAtPoint:
                    animator.Play(cachedHash, layer, floatValue1);
                    break;
                case Type.PlaySynced:
                    animator.Play(cachedHash, layer, animator.GetCurrentAnimatorStateInfo(layer).normalizedTime);
                    break;
                case Type.CrossFade:
                    animator.CrossFade(cachedHash, floatValue1, layer);
                    break;
                case Type.CrossFadeAtPoint:
                    animator.CrossFade(cachedHash, floatValue1, layer, floatValue2);
                    break;
                case Type.CrossFadeSynced:
                    animator.CrossFade(cachedHash, floatValue1, layer, animator.GetCurrentAnimatorStateInfo(layer).normalizedTime);
                    break;
                case Type.SetTrigger:
                    if (boolValue) animator.SetTrigger(cachedHash);
                    else animator.ResetTrigger(cachedHash);
                    break;
                case Type.SetFloat:
                    animator.SetFloat(cachedHash, floatValue1);
                    break;
                case Type.SetInt:
                    animator.SetInteger(cachedHash, intValue);
                    break;
                case Type.SetBool:
                    animator.SetBool(cachedHash, boolValue);
                    break;
                default: break;
            }
        }
        public void CacheID() => cachedHash = Animator.StringToHash(NameID);
    }
}