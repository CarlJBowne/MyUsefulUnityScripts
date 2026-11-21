using System;
using System.Collections.Generic;
using System.Text;
using SLS.Physics3D;

namespace SLS.StateMachineUltimate.Physics
{
    public abstract class StateMovementBehavior : StateBehavior<MovementBody>
    {
        public MovementBody Body => CoreComp;
    }
}
