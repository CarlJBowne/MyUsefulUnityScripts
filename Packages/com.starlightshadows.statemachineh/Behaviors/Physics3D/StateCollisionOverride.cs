using System;
using System.Collections.Generic;
using System.Text;
using SLS.Physics3D;

namespace SLS.StateMachineUltimate.Physics.Packages
{
    public abstract class StateCollisionOverride : StateMovementBehavior, ICollisionOverride
    {
        public abstract void CollisionOverride(ref PhysicsSweep collision);

        protected override void OnEnter(State prev, bool isFinal) => Body.collisionOverrides.Add(this);
        protected override void OnExit(State next) => Body.collisionOverrides.Remove(this);
    }
}
