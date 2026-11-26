using System;
using System.Collections.Generic;
using System.Text;
using SLS.Physics3D;
using SLS.StateMachineUltimate;

namespace SLS.Physics3D.States
{
    public abstract class StateMovementBehavior : StateBehavior<MovementBody>
    {
        public MovementBody Body => CoreComp;
    }
}
