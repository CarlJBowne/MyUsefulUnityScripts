using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SLS.StateMachineH.Timelines
{
    /// <summary>
    /// A Central <see cref="StateBehavior"/> to manage <see cref="StateTimeline"/> behaviors.
    /// </summary>
    [RequireComponent(typeof(StateMachine))]
    public class StateTimelineManager : StateBehavior
    {
        public enum TimelineSpace
        {
            UpdateScaled,
            FixedScaled,
            UpdateUnscaled,
            FixedUnscaled
        }

        public TimelineSpace timelineSpace = TimelineSpace.UpdateScaled;

        public List<StateTimeline> activeBehaviors { get; private set; } = new();


        private void Tick(float delta)
        {
            for (int i = 0; i < activeBehaviors.Count; i++) activeBehaviors[i].Tick(delta);
        }

        protected override void OnUpdate()
        {
            if(timelineSpace is TimelineSpace.UpdateScaled or TimelineSpace.UpdateUnscaled)
                Tick(timelineSpace == TimelineSpace.UpdateScaled ? Time.deltaTime : Time.unscaledDeltaTime);            
        }
        protected override void OnFixedUpdate()
        {
            if(timelineSpace is TimelineSpace.FixedScaled or TimelineSpace.FixedUnscaled)
                Tick(timelineSpace == TimelineSpace.FixedScaled ? Time.fixedDeltaTime : Time.fixedUnscaledDeltaTime);
        }
    }
}
