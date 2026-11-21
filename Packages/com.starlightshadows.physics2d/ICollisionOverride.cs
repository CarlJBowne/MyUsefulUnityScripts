using UnityEngine;

namespace SLS.Physics2D
{
    public interface ICollisionOverride
    {
        void CollisionOverride(ref PhysicsSweep collision);
    }

    public interface IPushable : ICollisionOverride
    {
        public MovementBody Body { get; }
        public IPushable active { get; }
        int PushPriority { get; }
        float PushResistance { get; }
        bool RecievePush(IPushable source, Vector2 direction, float distance, out float acceptedDistance);
    }
}
