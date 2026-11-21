using UnityEngine;
#if UNITY_EDITOR
#endif

namespace SLS.Physics3D
{
    public struct PhysicsSweep
    {
        public bool hit;
        public float distance;
        public Vector3 input;
        public Vector3 leftover;
        public Vector3 normal;
        public bool isRelative;
        public AnchorPoint anchorPoint;
        public AnchorPoint.Angle angle;
        public Vector3 barycentricCoordinate;
        public Collider collider;
        public static implicit operator bool(PhysicsSweep payload) => payload.hit;
        public static implicit operator AnchorPoint(PhysicsSweep payload) => payload.anchorPoint;
    }
}
