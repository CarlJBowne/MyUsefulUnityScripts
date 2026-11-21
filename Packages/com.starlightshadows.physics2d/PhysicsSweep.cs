using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace SLS.Physics2D
{
    public struct PhysicsSweep
    {
        public bool hit;
        public float distance;
        public Vector2 input;
        public Vector2 leftover;
        public Vector2 normal;
        public AnchorPoint anchorPoint;
        public AnchorPoint.Angle angle;
        public List<RaycastHit2D> collisions;
        public static implicit operator bool(PhysicsSweep payload) => payload.hit;
        public static implicit operator AnchorPoint(PhysicsSweep payload) => payload.anchorPoint;
    }
}
