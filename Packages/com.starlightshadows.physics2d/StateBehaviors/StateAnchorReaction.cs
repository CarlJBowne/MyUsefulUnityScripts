using UnityEngine;
using System.Collections.Generic;
using SLS.StateMachineUltimate;
using SLS.Physics2D;
using SLS.GeneralUtilities.Events;
using UltEvents;

namespace SLS.Physics2D.States
{
    public class StateAnchorReaction : StateMovementBehavior
    {
        public UltEvent onAnchor;
        public UltEvent onDeAnchor;
        public UltEvent onLand;
        public UltEvent onStick;
        public bool onlyOnNewlyAnchored;

        List<EventTicket> events = new();
        protected override void OnAwake()
        {
            if (onAnchor.HasCalls || onLand.HasCalls || onStick.HasCalls) events.Add(Body.Anchor.SubscribeOnAnchor(OnAnchor, false));
            if (onDeAnchor.HasCalls) events.Add(Body.Anchor.SubscribeOnDeAnchor(OnDeAnchor, false));
        }

        protected override void OnEnter(State prev, bool isFinal) => events.SubscribeAll();
        protected override void OnExit(State next) => events.UnSubscribeAll();


        void OnAnchor(AnchorState state, bool wasntAnchored, bool objectChange)
        {
            if (!wasntAnchored && onlyOnNewlyAnchored) return;
            onAnchor?.Invoke();
            if (state.Standing) onLand?.Invoke();
            if (state.Sticking) onStick?.Invoke();
        }
        void OnDeAnchor(AnchorState state) => onDeAnchor?.Invoke();
    }
}