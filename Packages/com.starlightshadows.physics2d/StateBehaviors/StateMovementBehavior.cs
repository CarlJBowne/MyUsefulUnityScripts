using System;
using System.Collections.Generic;
using System.Text;
using SLS.Physics2D;
using SLS.StateMachineUltimate;

namespace SLS.Physics2D.States
{
    public abstract class StateMovementBehavior : StateBehavior<MovementBody>
    {
        public MovementBody Body => CoreComp;
    }
}
