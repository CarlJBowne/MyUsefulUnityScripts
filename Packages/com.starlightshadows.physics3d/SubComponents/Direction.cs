using System.Collections;
using UnityEngine;

namespace RageRooster.Physics
{
    /// <summary>
    /// <see cref="PhysicsBody"/> Sub-component that tracks the facing direction for a PhysicsBody. The Direction is used when converting between local and global velocities and for rotation helper functions (quick turns, limited turns, etc.).
    /// </summary>
    [System.Serializable]
    public class Direction : PhysicsSubComponent
    {
        /// <summary>
        /// The currently cached forward vector used by the physics body.
        /// </summary>
        public Vector3 value { get; private set; }
        public static implicit operator Vector3(Direction This) => This.value;

        /// <summary>
        /// Smoothly rotates the current facing value toward <paramref name="target"/>
        /// using a maximum turn speed measured in degrees per second.
        /// </summary>
        /// <param name="target">Target forward vector in world space.</param>
        /// <param name="maxTurnDegrees">Maximum degrees per second to rotate.</param>
        public void Set(Vector3 target, float maxTurnDegrees)
        {
            if (target == Vector3.zero) return;
            Vector3 res = Vector3.RotateTowards(value, target.normalized, maxTurnDegrees * Mathf.PI, 1);
            Set(res);
        }
        /// <summary>
        /// Immediately sets the facing direction to <paramref name="target"/>
        /// and updates the underlying rotation quaternion on the owner's Rigidbody.
        /// </summary>
        /// <param name="target">Target forward vector in world space.</param>
        public void Set(Vector3 target)
        {
            if (value == target || target == Vector3.zero) return;
            value = target;
            RotationQ = Quaternion.LookRotation(target, Vector3.up);
        }

        /// <summary>
        /// Gets or sets the owner's rigidbody rotation as a Quaternion. Setting this
        /// property will call <see cref="Velocity.CallThisPostRotation"/> to keep
        /// the velocity representations consistent.
        /// </summary>
        public Quaternion RotationQ
        {
            get => body.RB.rotation;
            set
            {
                body.RB.rotation = value;
                body.Velocity.CallThisPostRotation();
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
                body.Velocity.CallThisPostRotation();
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
                value = target;
                return;
            }

            Coroutine.Begin(ref QuickTurnRoutine, Enum(), body, true);
            IEnumerator Enum()
            {
                float deltaRad = Vector3.Angle(value, target) * Mathf.Deg2Rad;
                float rateRadPerSec = deltaRad / lengthSeconds; // radians per second

                while (deltaRad > 0f)
                {
                    value = Vector3.RotateTowards(value, target, rateRadPerSec * Time.fixedDeltaTime, 0f);
                    yield return new WaitForFixedUpdate();
                    deltaRad -= rateRadPerSec * Time.fixedDeltaTime;
                }
                value = target;
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

            Coroutine.Begin(ref QuickTurnRoutine, Enum(), body, true);
            IEnumerator Enum()
            {
                float fullDelta = Vector3.Angle(value, target) * Mathf.Deg2Rad;

                while (fullDelta > 0f)
                {
                    value = Vector3.RotateTowards(value, target, maxDelta * Time.fixedDeltaTime, 0f);
                    yield return null;
                    fullDelta -= maxDelta * Time.fixedDeltaTime;
                }

                value = target;
            }
        }
        private Coroutine QuickTurnRoutine;
    }
}