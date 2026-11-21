using System;
using System.Collections.Generic;
using System.Text;
using SLS.Physics3D;
using SLS.StateMachineUltimate;

namespace SLS.Physics3D.States
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
