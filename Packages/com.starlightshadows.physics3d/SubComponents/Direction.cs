using System.Collections;
using UnityEngine;

namespace SLS.Physics3D
{
    /// <summary>
    /// <see cref="MovingBody"/> Sub-component that tracks the facing direction for a PhysicsBody. The Direction is used when converting between local and global velocities and for rotation helper functions (quick turns, limited turns, etc.).
    /// </summary>
    [System.Serializable]
    public class Direction : PhysicsSubComponent
    {
        #region Config
        /// <summary>
        /// The minimum Angle from this Body's UP direction that is considered a slope rather than just a floor.
        /// </summary>
        [field: SerializeField] public float angleSlope { get; private set; } = 10f;
        /// <summary>
        /// The maximum angle from this Body's UP direction that is considered standable.
        /// </summary>
        [field: SerializeField] public float angleStandable { get; private set; } = 40f;
        /// <summary>
        /// The minimum Angle from this Body's UP direction that is considered a wall rather than a slope.
        /// </summary>
        [field: SerializeField] public float angleWall { get; private set; } = 80f;
        /// <summary>
        /// The minimum Angle from this Body's UP direction that is considered an inverted slope rather than a wall.
        /// </summary>
        [field: SerializeField] public float angleInvertedSlope { get; private set; } = 100f;
        /// <summary>
        /// The minimum Angle from this Body's UP direction that is considered a ceiling.
        /// </summary>
        [field: SerializeField] public float angleCeiling { get; private set; } = 150f;
        #endregion

        /// <summary>
        /// The currently cached forward vector used by the physics body.
        /// </summary>
        public Vector3 Value { get; private set; }

        /// <summary>
        /// The up direction the body should consider "vertical". Defaults to Vector3.up.
        /// Use SetUp to change this at runtime; changing Up will recompute the body's rotation
        /// so the forward Value is preserved relative to the new up direction.
        /// </summary>
        public Vector3 Up { get; private set; } = Vector3.up;
        public static implicit operator Vector3(Direction This) => This.Value;

        /// <summary>
        /// Smoothly rotates the current facing value toward <paramref name="target"/>
        /// using a maximum turn speed measured in degrees per second.
        /// </summary>
        /// <param name="target">Target forward vector in world space.</param>
        /// <param name="maxTurnDegrees">Maximum degrees per second to rotate.</param>
        public void Set(Vector3 target, float maxTurnDegrees)
        {
            if (target == Vector3.zero) return;
            Vector3 res = Vector3.RotateTowards(Value, target.normalized, maxTurnDegrees * Mathf.PI, 1);
            Set(res);
        }
        /// <summary>
        /// Immediately sets the facing direction to <paramref name="target"/>
        /// and updates the underlying rotation quaternion on the owner's Rigidbody.
        /// </summary>
        /// <param name="target">Target forward vector in world space.</param>
        public void Set(Vector3 target)
        {
            if (Value == target || target == Vector3.zero) return;
            Value = target;
            RotationQ = Quaternion.LookRotation(target, Up);
        }

        /// <summary>
        /// Sets the body's local up direction. This updates the body's rotation
        /// so the current forward Value is maintained relative to the new up.
        /// </summary>
        /// <param name="up">New up vector in world space. Must be non-zero.</param>
        public void SetUp(Vector3 up)
        {
            if (up == Vector3.zero) return;
            var n = up.normalized;
            if (Up == n) return;
            Up = n;
            // Recompute rotation so the current forward direction is preserved using the new up.
            // If Value is zero fall back to current transform.forward.
            var forward = Value == Vector3.zero ? transform.forward : Value;
            RotationQ = Quaternion.LookRotation(forward, Up);
        }

        /// <summary>
        /// Gets or sets the owner's rigidbody rotation as a Quaternion. Setting this
        /// property will call <see cref="Velocity.CallThisPostRotation"/> to keep
        /// the velocity representations consistent.
        /// </summary>
        public Quaternion RotationQ
        {
            get => RB.rotation;
            set
            {
                RB.rotation = value;
                Velocity.CallThisPostRotation();
            }
        }
        /// <summary>
        /// Gets or sets the owner's transform.eulerAngles. Setting triggers <see cref="Velocity.CallThisPostRotation"/>
        /// </summary>
        public Vector3 Rotation
        {
            get => transform.eulerAngles;
            set
            {
                transform.eulerAngles = value;
                Velocity.CallThisPostRotation();
            }
        }
        /// <summary>
        /// Gets or sets the owner's transform.eulerAngles.y. Setting triggers <see cref="Velocity.CallThisPostRotation"/>
        /// </summary>
        public float RotationY
        {
            get => Rotation.y;
            set
            {
                Vector3 prev = Rotation;
                prev.y = value;
                Rotation = prev;
            }
        }

        /// <summary>
        /// Performs a smooth quick-turn toward <paramref name="target"/> over the provided duration (in seconds). This method runs a coroutine and adjusts the facing vector incrementally each FixedUpdate.
        /// </summary>
        /// <param name="target">Target forward vector (XZ only).</param>
        /// <param name="lengthSeconds">Time duration to complete the quick turn.</param>
        public void QuickTurnTime(Vector3 target, float lengthSeconds)
        {
            target = target.XZ(); //Ensure no weird rotations

            if (lengthSeconds <= 0f)
            {
                Value = target;
                return;
            }

            Coroutine.Begin(ref QuickTurnRoutine, Enum(), Body, true);
            IEnumerator Enum()
            {
                float deltaRad = Vector3.Angle(Value, target) * Mathf.Deg2Rad;
                float rateRadPerSec = deltaRad / lengthSeconds; // radians per second

                while (deltaRad > 0f)
                {
                    Value = Vector3.RotateTowards(Value, target, rateRadPerSec * Time.fixedDeltaTime, 0f);
                    yield return new WaitForFixedUpdate();
                    deltaRad -= rateRadPerSec * Time.fixedDeltaTime;
                }
                Value = target;
            }
        }
        /// <summary>
        /// Performs a smooth quick-turn toward <paramref name="target"/> with the provided maximum delta. This method runs a coroutine and adjusts the facing vector incrementally each FixedUpdate.
        /// </summary>
        /// <param name="target">Target forward vector (XZ only).</param>
        /// <param name="maxDelta">The maximum delta the body is allowed to move during a frame.</param>
        public void QuickTurnLimited(Vector3 target, float maxDelta)
        {
            target = target.XZ(); //Ensure no weird rotations
            if (maxDelta <= 0f) return;

            Coroutine.Begin(ref QuickTurnRoutine, Enum(), Body, true);
            IEnumerator Enum()
            {
                float fullDelta = Vector3.Angle(Value, target) * Mathf.Deg2Rad;

                while (fullDelta > 0f)
                {
                    Value = Vector3.RotateTowards(Value, target, maxDelta * Time.fixedDeltaTime, 0f);
                    yield return null;
                    fullDelta -= maxDelta * Time.fixedDeltaTime;
                }

                Value = target;
            }
        }
        /// <summary>
        /// Performs a smooth quick-turn toward <paramref name="target"/> over the provided duration (in seconds). This method runs a coroutine and adjusts the facing vector incrementally each FixedUpdate.
        /// </summary>
        /// <param name="target">Target forward vector (XZ only).</param>
        /// <param name="lengthSeconds">Time duration to complete the quick turn.</param>
        public void QuickUpTurnTime(Vector3 target, float lengthSeconds)
        {
            target = target.XZ(); //Ensure no weird rotations

            if (lengthSeconds <= 0f)
            {
                Up = target;
                return;
            }

            Coroutine.Begin(ref QuickTurnRoutine, Enum(), Body, true);
            IEnumerator Enum()
            {
                float deltaRad = Vector3.Angle(Up, target) * Mathf.Deg2Rad;
                float rateRadPerSec = deltaRad / lengthSeconds; // radians per second

                while (deltaRad > 0f)
                {
                    Up = Vector3.RotateTowards(Up, target, rateRadPerSec * Time.fixedDeltaTime, 0f);
                    yield return new WaitForFixedUpdate();
                    deltaRad -= rateRadPerSec * Time.fixedDeltaTime;
                }
                Up = target;
            }
        }
        /// <summary>
        /// Performs a smooth quick-turn toward <paramref name="target"/> with the provided maximum delta. This method runs a coroutine and adjusts the facing vector incrementally each FixedUpdate.
        /// </summary>
        /// <param name="target">Target forward vector (XZ only).</param>
        /// <param name="maxDelta">The maximum delta the body is allowed to move during a frame.</param>
        public void QuickUpTurnLimited(Vector3 target, float maxDelta)
        {
            target = target.XZ(); //Ensure no weird rotations
            if (maxDelta <= 0f) return;

            Coroutine.Begin(ref QuickTurnRoutine, Enum(), Body, true);
            IEnumerator Enum()
            {
                float fullDelta = Vector3.Angle(Up, target) * Mathf.Deg2Rad;

                while (fullDelta > 0f)
                {
                    Up = Vector3.RotateTowards(Up, target, maxDelta * Time.fixedDeltaTime, 0f);
                    yield return null;
                    fullDelta -= maxDelta * Time.fixedDeltaTime;
                }

                Up = target;
            }
        }
        private Coroutine QuickTurnRoutine;

        public Transform lookTarget;

        /// <summary>
        /// Gets the NormalType of the <see cref="AnchorPoint"/>'s Normal based on this Body's angle definitions
        /// </summary>
        public AnchorPoint.Angle Angle() => Angle(Body.Anchor.Normal);
        /// <summary>
        /// Gets the NormalType of the input angle based on this Body's angle definitions
        /// </summary>
        public AnchorPoint.Angle Angle(Vector3 inNormal)
        {
            if (inNormal == Vector3.zero) return AnchorPoint.Angle.Null;
            float val = Vector3.Angle(Direction.Up, inNormal);
            return val < angleSlope ? AnchorPoint.Angle.Floor
                : val <= angleStandable ? AnchorPoint.Angle.Slope
                : val < angleWall ? AnchorPoint.Angle.SteepSlope
                : val < angleInvertedSlope ? AnchorPoint.Angle.Wall
                : val < angleCeiling ? AnchorPoint.Angle.InvertedSlope
                : AnchorPoint.Angle.Ceiling;
        }
    }
}