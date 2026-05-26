using System.Collections;
using UnityEngine;

namespace SLS.StateMachineH.Timelines
{
    public class TransitionAtEnd : StateTimeline
    {
        public float endTime = 1f;
        public State targetState;

        protected override void OnTick(float delta)
        {
            if(elapsedTime >= endTime) targetState.Enter();
        }
    }
}