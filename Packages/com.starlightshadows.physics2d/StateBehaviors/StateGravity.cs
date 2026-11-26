
using UnityEngine;

namespace SLS.Physics2D.States
{
    public class StateGravity : StateMovementBehavior
    {
        public float gravity = 9.81f;
        public float terminalVelocity = 100f;

        protected override void OnFixedUpdate() => Body.Velocity.y = (Body.Velocity.y - (gravity * Time.deltaTime)).Min(-terminalVelocity);
    }
}