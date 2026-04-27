using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimationAction : Polymorph
{
    public string name;
    public int layer = -1;
    public abstract void Do(Animator animator);

    public class Play : AnimationAction
    {
        public override void Do(Animator animator) => animator.Play(name, layer);
    }
    public class PlayAtPoint : AnimationAction
    {
        public float timeOffset;
        public override void Do(Animator animator) => animator.Play(name, layer, timeOffset);
    }
    public class PlaySynced : AnimationAction
    {
        public override void Do(Animator animator) => animator.Play(name, layer, animator.GetCurrentAnimatorStateInfo(layer).normalizedTime);
    }
    public class Crossfade : AnimationAction
    {
        public float transitionDuration;
        public float timeOffset;

        public override void Do(Animator animator) => animator.CrossFade(name, transitionDuration, layer, -1, timeOffset);
    }
    public class CrossFadeSynced : AnimationAction
    {
        public float transitionDuration;
        public override void Do(Animator animator) => animator.CrossFade(name, transitionDuration, layer, -1, animator.GetCurrentAnimatorStateInfo(layer).normalizedTime);
    }
    public class Trigger : AnimationAction
    {
        public override void Do(Animator animator) => animator.SetTrigger(name);
    }
    public class SetFloat : AnimationAction
    {
        public float value;
        public override void Do(Animator animator) => animator.SetFloat(name, value);
    }
    public class SetBool : AnimationAction
    {
        public bool value;
        public override void Do(Animator animator) => animator.SetBool(name, value);
    }
    public class SetInteger : AnimationAction
    {
        public int value;
        public override void Do(Animator animator) => animator.SetInteger(name, value);
    }
}