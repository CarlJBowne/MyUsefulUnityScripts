using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SLS.EditorUtilities.ComponentHeaders;
using SLS.Physics3D;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using static SLS.Physics3D.MovingBody;

namespace SLS.Physics3D
{
    /// <summary>
    /// This is a helper class for the MovingBody component. It is used to provide additional functionality and convenience methods for working with MovingBody instances, primarily for UltEvents so that various functions and properties belonging to SubComponents can be accessible.
    /// </summary>
    [RequireComponent(typeof(MovingBody))]
    public class MovingBodyHelper : MonoBehaviour
    {
        [HeaderItem(true)] public MovingBody _movingBody;

        Velocity velocity;
        AnchorState anchor;
        Direction direction;

        private void Reset() => _movingBody = GetComponent<MovingBody>();

        private void Awake()
        {
            if (_movingBody == null) _movingBody = GetComponent<MovingBody>();
            if (_movingBody == null) { Destroy(this); return; }
            velocity = _movingBody.Velocity;
            anchor = _movingBody.Anchor;
            direction = _movingBody.Direction;
        }

        #region Velocity

        public float VelocityF
        {
            get => velocity.f;
            set => velocity.f = value;
        }
        public float VelocityU
        {
            get => velocity.u;
            set => velocity.u = value;
        }
        public float VelocityS
        {
            get => velocity.s;
            set => velocity.s = value;
        }
        public float VelocityX
        {
            get => velocity.x;
            set => velocity.x = value;
        }
        public float VelocityY
        {
            get => velocity.y;
            set => velocity.y = value;
        }
        public float VelocityZ
        {
            get => velocity.z;
            set => velocity.z = value;
        }
        public Vector3 VelocityLocal
        {
            get => velocity.Local;
            set => velocity.Local = value;
        }
        public Vector3 VelocityGlobal
        {
            get => velocity.Global;
            set => velocity.Global = value;
        }

        public float VelocityCarryLocal
        {
            get => velocity.carryLocal;
            set => velocity.carryLocal = value;
        }
        public float VelocityCarryGlobal
        {
            get => velocity.carryGlobal;
            set => velocity.carryGlobal = value;
        }
        public float VelocityLook
        {
            get => velocity.lookVelocity;
            set => velocity.lookVelocity = value;
        }
        public void Zero() => velocity.ZeroOut();

        #endregion

        #region Direction

        public Vector3 Direction => direction.Value;
        public Vector3 DirectionUP => direction.Up;

        /// <summary>
        /// Smoothly rotates the current facing value toward <paramref name="target"/>
        /// using a maximum turn speed measured in degrees per second.
        /// </summary>
        /// <param name="target">Target forward vector in world space.</param>
        /// <param name="maxTurnDegrees">Maximum degrees per second to rotate.</param>
        public void SetDirection(Vector3 target, float maxTurnDegrees) => direction.Set(target, maxTurnDegrees);
        /// <summary>
        /// Immediately sets the facing direction to <paramref name="target"/>
        /// and updates the underlying rotation quaternion on the owner's Rigidbody.
        /// </summary>
        /// <param name="target">Target forward vector in world space.</param>
        public void SetDirection(Vector3 target) => direction.Set(target);

        /// <summary>
        /// Sets the body's local up direction. This updates the body's rotation
        /// so the current forward Value is maintained relative to the new up.
        /// </summary>
        /// <param name="up">New up vector in world space. Must be non-zero.</param>
        public void SetDirectionUp(Vector3 up) => direction.SetUp(up);

        /// <summary>
        /// Gets or sets the owner's rigidbody rotation as a Quaternion. Setting this
        /// property will call <see cref="Velocity.CallThisPostRotation"/> to keep
        /// the velocity representations consistent.
        /// </summary>
        public Quaternion RotationQ
        {
            get => direction.RotationQ;
            set => direction.RotationQ = value;
        }
        /// <summary>
        /// Gets or sets the owner's transform.eulerAngles. Setting triggers <see cref="Velocity.CallThisPostRotation"/>
        /// </summary>
        public Vector3 Rotation
        {
            get => transform.eulerAngles;
            set => direction.Rotation = value;
        }
        /// <summary>
        /// Gets or sets the owner's transform.eulerAngles.y. Setting triggers <see cref="Velocity.CallThisPostRotation"/>
        /// </summary>
        public float RotationY
        {
            get => direction.RotationY;
            set => direction.RotationY = value;
        }

        /// <summary>
        /// Performs a smooth quick-turn toward <paramref name="target"/> over the provided duration (in seconds). This method runs a coroutine and adjusts the facing vector incrementally each FixedUpdate.
        /// </summary>
        /// <param name="target">Target forward vector (XZ only).</param>
        /// <param name="lengthSeconds">Time duration to complete the quick turn.</param>
        public void QuickTurnTime(Vector3 target, float lengthSeconds) => direction.QuickTurnTime(target, lengthSeconds);
        /// <summary>
        /// Performs a smooth quick-turn toward <paramref name="target"/> with the provided maximum delta. This method runs a coroutine and adjusts the facing vector incrementally each FixedUpdate.
        /// </summary>
        /// <param name="target">Target forward vector (XZ only).</param>
        /// <param name="maxDelta">The maximum delta the body is allowed to move during a frame.</param>
        public void QuickTurnLimited(Vector3 target, float maxDelta) => direction.QuickTurnLimited(target, maxDelta);
        /// <summary>
        /// Performs a smooth quick-turn toward <paramref name="target"/> over the provided duration (in seconds). This method runs a coroutine and adjusts the facing vector incrementally each FixedUpdate.
        /// </summary>
        /// <param name="target">Target forward vector (XZ only).</param>
        /// <param name="lengthSeconds">Time duration to complete the quick turn.</param>
        public void QuickUpTurnTime(Vector3 target, float lengthSeconds) => direction.QuickUpTurnTime(target, lengthSeconds);
        /// <summary>
        /// Performs a smooth quick-turn toward <paramref name="target"/> with the provided maximum delta. This method runs a coroutine and adjusts the facing vector incrementally each FixedUpdate.
        /// </summary>
        /// <param name="target">Target forward vector (XZ only).</param>
        /// <param name="maxDelta">The maximum delta the body is allowed to move during a frame.</param>
        public void QuickUpTurnLimited(Vector3 target, float maxDelta) => direction.QuickUpTurnLimited(target, maxDelta);

        /// <summary>
        /// Gets the NormalType of the input angle based on this Body's angle definitions
        /// </summary>
        public AnchorPoint.Angle Angle(Vector3 inNormal) => direction.Angle(inNormal);

        #endregion

        #region AnchorState

        public Vector3 AnchorNormal => anchor.Normal;
        public Vector3 AnchorPoint => anchor.Point;

        /// <summary>
        /// Attempts to Anchor to the passed in <see cref="Physics3D.AnchorPoint"/>
        /// </summary>
        /// <param name="newAnchorPoint">Defines the position and normal being Anchored too</param>
        /// <param name="tryNavMesh"> Whether the system should attempt to Anchor the player to the NavMesh</param>
        /// <param name="allowSticking"> Whether the system should allow anchorage to non-standable surfaces</param>
        /// <returns>Success</returns>
        public bool Anchor(AnchorPoint newAnchorPoint, PhysicsResolver newResolver = null, bool allowSticking = false)
            => anchor.Anchor(newAnchorPoint, newResolver, allowSticking);
        /// <summary>
        /// Attempts to Anchor to any collider found in the input direction
        /// </summary>
        /// <param name="vector">The direction to check for a collider</param>
        /// <param name="tryNavMesh"> Whether the system should attempt to Anchor the player to the NavMesh</param>
        /// <param name="allowSticking"> Whether the system should allow anchorage to non-standable surfaces</param>
        /// <returns>Success</returns>
        public bool Anchor(Vector3 vector, PhysicsResolver newResolver = null, bool allowSticking = false)
            => anchor.Anchor(vector, newResolver, allowSticking);
        /// <summary>
        /// Attempts to Anchor to any collision found direction of the Body's current Velocity vector
        /// </summary>
        /// <param name="tryNavMesh"> Whether the system should attempt to Anchor the player to the NavMesh</param>
        /// <param name="allowSticking"> Whether the system should allow anchorage to non-standable surfaces</param>
        /// <returns>Success</returns>
        public bool Anchor(PhysicsResolver newResolver = null, bool allowSticking = false) => anchor.Anchor(newResolver, allowSticking);

        /// <summary>
        /// Attempts to Anchor to the passed in <see cref="Physics3D.AnchorPoint"/> (Only Standable Ground)
        /// </summary>
        /// <param name="newAnchorPoint">Defines the position and normal being Anchored too</param>
        /// <param name="tryNavMesh"> Whether the system should attempt to Anchor the player to the NavMesh</param>
        /// <returns>Success</returns>
        public bool Land(AnchorPoint newAnchorPoint, PhysicsResolver.GroundType newResolver = null) => Anchor(newAnchorPoint, newResolver);
        /// <summary>
        /// Attempts to Anchor to any collider found in the input direction (Only Standable Ground)
        /// </summary>
        /// <param name="vector">The direction to check for a collider</param>
        /// <param name="tryNavMesh"> Whether the system should attempt to Anchor the player to the NavMesh</param>
        /// <returns>Success</returns>
        public bool Land(Vector3 vector, PhysicsResolver.GroundType newResolver = null) => Anchor(vector, newResolver);
        /// <summary>
        /// Attempts to Anchor to any collision found direction of the Body's current Velocity vector (Only Standable Ground)
        /// </summary>
        /// <param name="tryNavMesh"> Whether the system should attempt to Anchor the player to the NavMesh</param>
        /// <returns>Success</returns>
        public bool Land(PhysicsResolver.GroundType newResolver = null) => Anchor(newResolver);

        /// <summary>
        /// Attempts to Anchor to the passed in <see cref="Physics3D.AnchorPoint"/> (Allows Sticking)
        /// </summary>
        /// <param name="newAnchorPoint">Defines the position and normal being Anchored too</param>
        /// <returns>Success</returns>
        public bool Stick(AnchorPoint newAnchorPoint, PhysicsResolver.StickyType newResolver = null) => Anchor(newAnchorPoint, newResolver, true);
        /// <summary>
        /// Attempts to Anchor to any collider found in the input direction (Allows Sticking)
        /// </summary>
        /// <param name="vector">The direction to check for a collider</param>
        /// <returns>Success</returns>
        public bool Stick(Vector3 vector, PhysicsResolver.StickyType newResolver = null) => Anchor(vector, newResolver, true);
        /// <summary>
        /// Attempts to Anchor to any collision found direction of the Body's current Velocity vector (Allows Sticking)
        /// </summary>
        /// <returns>Success</returns>
        public bool Stick(PhysicsResolver.StickyType newResolver = null) => Anchor(newResolver, true);

        /// <summary>
        /// Deanchors this Body from whatever Anchor it has.
        /// </summary>
        /// <param name="setJumping"></param>
        public void DeAnchor(PhysicsResolver.AirType newResolver = null) => anchor.DeAnchor(newResolver);

        /// <summary>
        /// True if the NormalType of the input angle is, based on the Body's angle definitions, standable.
        /// </summary>
        public bool Standable(Vector3 inNormal) => anchor.Standable(inNormal);




        #endregion

    }
}
