#if UNITY_EDITOR
#endif

using SLS.Physics;
using UnityEngine;

namespace SLS.Physics3D
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
        bool RecievePush(IPushable source, Vector3 direction, float distance, out float acceptedDistance);
    }
}
