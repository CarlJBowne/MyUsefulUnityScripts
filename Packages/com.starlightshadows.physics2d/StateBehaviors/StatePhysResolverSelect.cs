using System;
using System.Collections.Generic;
using System.Text;
using SLS.Physics2D;
using SLS.StateMachineUltimate;

namespace SLS.Physics2D.States
{
    public class StatePhysResolverSelect : StateMovementBehavior
    {
        public PhysicsResolver TargetResolver;
        public bool onlyIfFinal = false;

        protected override void OnEnter(State prev, bool isFinal)
        {
            if (onlyIfFinal && !isFinal) return;
            Body.SelectResolver(TargetResolver);
        }
    }
}
