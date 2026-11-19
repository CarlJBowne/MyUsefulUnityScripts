using UnityEngine;
#if UNITY_EDITOR
#endif

namespace SLS.Physics3D
{
    public struct SweepPayload
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
        public static implicit operator bool(SweepPayload payload) => payload.hit;
        public static implicit operator AnchorPoint(SweepPayload payload) => payload.anchorPoint;
    }
}
