using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace SLS.Physics3D
{
    /// <summary>
    /// <see cref="MovingBody"/> Sub-component that tracks whether the body is grounded and relevant information about the ground contact (normal, slope, collider, etc.). This component also provides helper methods for performing ground checks and transitioning between grounded and airborne states.
    /// </summary>
    [System.Serializable]
    public class AnchorState : PhysicsSubComponent
    {

        #region Values

        /// <summary>
        /// Whether this Body is currently anchored to a Physical Object (Floor, Wall, or Ceiling).
        /// </summary>
        public bool Anchored { get; private set; }
        /// <summary>
        /// Whether this body is currently airborne. (Not anchored to anything.)
        /// </summary>
        public bool Airborne => !Anchored;
        /// <summary>
        /// Whether this body is currently moving upward. 
        /// <br/>(<see cref="Airborne"/> and u Velocity is above .1f)
        /// </summary>
        public bool Rising => Airborne && Velocity.u > .1f;
        /// <summary>
        /// Whether this body is currently falling downward. 
        /// <br/>(<see cref="Airborne"/> and u Velocity is below -.1f)
        /// </summary>
        public bool Falling => Airborne && Velocity.u < -.1f;
        /// <summary>
        /// Whether this Body is currently anchored to a Floor. 
        /// <br/>(<see cref="Anchored"/> and Anchor Normal is within standable slope Angle)
        /// </summary>
        public bool Standing => Anchored && Standable(Normal);
        /// <summary>
        /// Whether this body is currently anchored to a Wall. 
        /// <br/>(<see cref="Anchored"/> and Anchor Normal isn't within standable slope Angle)
        /// </summary>
        public bool Sticking => Anchored && !Standable(Normal);
        /// <summary>
        /// Whether this Body is currently anchored to a Nav Mesh.
        /// </summary>
        public bool OnNavMesh => Standing && NavAgent != null && NavAgent.enabled;


        #endregion

        /// <summary>
        /// The anchor point representing the last ground contact (point, normal, collider).
        /// </summary>
        new public AnchorPoint AnchorPoint { get; private set; }

        /// <summary>
        /// If the current ground collider implements <see cref="IMovablePlatform"/>,
        /// this property will cache that interface for convenient platform-relative
        /// motion handling.
        /// </summary>
        public IMovablePlatform CurrentMovingAnchor { get; private set; }

        /// <summary>
        /// The Normal vector of the <see cref="AnchorPoint"/>
        /// </summary>
        public Vector3 Normal => AnchorPoint.normal;
        /// <summary>
        /// The Point value of the <see cref="AnchorPoint"/>
        /// </summary>
        public Vector3 Point => AnchorPoint.point;
        /// <summary>
        /// The Collider of the <see cref="AnchorPoint;"/>
        /// </summary>
        new public Collider Collider => AnchorPoint.collider;

        /// <summary>
        /// Attempts to Anchor to the passed in <see cref="Physics3D.AnchorPoint"/>
        /// </summary>
        /// <param name="newAnchorPoint">Defines the position and normal being Anchored too</param>
        /// <param name="tryNavMesh"> Whether the system should attempt to Anchor the player to the NavMesh</param>
        /// <param name="allowSticking"> Whether the system should allow anchorage to non-standable surfaces</param>
        /// <returns>Success</returns>
        new public bool Anchor(AnchorPoint newAnchorPoint, PhysicsResolver newResolver = null, bool allowSticking = false)
        {
            if (!HasOwner) return false;
            if (Body.groundResolver && newResolver) return false;
            bool wasntAnchored = !Anchored;
            bool objectChange = Collider != newAnchorPoint.collider;
            bool standable = Standable(newAnchorPoint.normal);
            if (!wasntAnchored && !objectChange) return true;

            if (!allowSticking && !standable) return false;

            Anchored = true;
            AnchorPoint = newAnchorPoint;

            if (objectChange)
            {
                CurrentMovingAnchor?.RemoveBody(Body);
                CurrentMovingAnchor = newAnchorPoint.collider.GetComponent<IMovablePlatform>();
                CurrentMovingAnchor?.AddBody(Body);
            }

            if (newResolver) Body.SelectResolver(newResolver);
            else if (standable) Body.SelectResolver(Body.groundResolver);

            Body.OnAnchor(wasntAnchored, objectChange);
            return true;
        }
        /// <summary>
        /// Attempts to Anchor to any collider found in the input direction
        /// </summary>
        /// <param name="vector">The direction to check for a collider</param>
        /// <param name="tryNavMesh"> Whether the system should attempt to Anchor the player to the NavMesh</param>
        /// <param name="allowSticking"> Whether the system should allow anchorage to non-standable surfaces</param>
        /// <returns>Success</returns>
        new public bool Anchor(Vector3 vector, PhysicsResolver newResolver = null, bool allowSticking = false) =>
            Body.Sweep(vector, out SweepPayload hit)
            && Anchor(hit, newResolver, allowSticking);
        /// <summary>
        /// Attempts to Anchor to any collision found direction of the Body's current Velocity vector
        /// </summary>
        /// <param name="tryNavMesh"> Whether the system should attempt to Anchor the player to the NavMesh</param>
        /// <param name="allowSticking"> Whether the system should allow anchorage to non-standable surfaces</param>
        /// <returns>Success</returns>
        new public bool Anchor(PhysicsResolver newResolver = null, bool allowSticking = false) =>
            Body.Sweep(Body.Velocity.Global, out SweepPayload hit)
            && Anchor(hit, newResolver, allowSticking);

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
        public void DeAnchor(PhysicsResolver.AirType newResolver = null)
        {
            if (!HasOwner || !Anchored) return;
            //if (Body.airResolver && newResolver) return;

            Anchored = false;
            if (NavAgent != null) NavAgent.enabled = false;
            AnchorPoint = default;

            CurrentMovingAnchor?.RemoveBody(Body);
            CurrentMovingAnchor = null;

            if (newResolver) Body.SelectResolver(newResolver);
            else Body.SelectResolver(Body.airResolver);

            Body.OnDeanchor();
        }

        /// <summary>
        /// True if the NormalType of the input angle is, based on the Body's angle definitions, standable.
        /// </summary>
        public bool Standable(Vector3 inNormal) => Direction.Angle(inNormal) is AnchorPoint.Angle.Floor or AnchorPoint.Angle.Slope;

        /// <summary>
        /// Performs a sweep test using the internal Rigidbody to determine whether this
        /// body would collide when translated by <paramref name="offset"/> with a surface that is standable. Optionally
        /// supports a temporary origin and a buffer distance to shrink the effective start
        /// location for the sweep.
        /// </summary>
        /// <param name="offset">The desired translation vector to sweep along.</param>
        /// <param name="hit">Outputs an <see cref="Physics3D.AnchorPoint"/> based on the first <see cref="RaycastHit"/> detected by the sweep (if any).</param>
        /// <param name="buffer">A small buffer to back the test origin up along <paramref name="offset"/>. Defaults to 0.</param>
        /// <param name="tempOrigin">An optional temporary origin to perform the sweep from instead of the current RB position.</param>
        /// <param name="queryTriggerInteraction">Whether the sweep should hit trigger colliders. Defaults to Ignore.</param>
        /// <returns>True if the sweep detected a collider, otherwise false.</returns>
        public bool SweepStandable(Vector3 offset, out SweepPayload hit, float? buffer = null, Vector3? tempOrigin = null, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore) => 
            Body.Sweep(offset, out hit, false, buffer, tempOrigin, queryTriggerInteraction) && Standable(hit.normal);
        /// <summary>
        /// Performs a sweep test using the internal Rigidbody to determine whether this
        /// body would collide when translated by <paramref name="offset"/> with a surface that is standable. Optionally
        /// supports a temporary origin and a buffer distance to shrink the effective start
        /// location for the sweep.
        /// </summary>
        /// <param name="offset">The desired translation vector to sweep along.</param>
        /// <param name="hit">Outputs an <see cref="Physics3D.AnchorPoint"/> based on the first <see cref="RaycastHit"/> detected by the sweep (if any).</param>
        /// <param name="buffer">A small buffer to back the test origin up along <paramref name="offset"/>. Defaults to 0.</param>
        /// <param name="tempOrigin">An optional temporary origin to perform the sweep from instead of the current RB position.</param>
        /// <param name="queryTriggerInteraction">Whether the sweep should hit trigger colliders. Defaults to Ignore.</param>
        /// <returns>True if the sweep detected a collider that was considered standable, otherwise false.</returns>
        public bool SweepStandable(out SweepPayload hit, float? buffer = null, Vector3? tempOrigin = null, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore) =>
            Body.Sweep(Direction.Up * -.1f, out hit, false, buffer, tempOrigin, queryTriggerInteraction) && Standable(hit.normal);


        /// <summary>
        /// Attempts an immediate snap to the floor by sweeping a long distance downwards
        /// and moving the body to the detected surface when present. Useful for initial
        /// positioning in Awake.
        /// </summary>
        /// <param name="hit">Outputs the RaycastHit that was used for the snap.</param>
        /// <returns>True if a floor was found and the body was moved, otherwise false.</returns>
        public bool InstantSnapToFloor(out SweepPayload hit)
        {
            if (Body.Sweep(Vector3.down * 1000, out hit, buffer: .5f))
            {
                Position += Vector3.down * hit.distance;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            string res = "";
            if (Anchored)
            {
                res += $"Anchored - {Normal:F2} - ";
                if (Standing)
                {
                    res += "Standing";
                    if (OnNavMesh) res += "(Nav)";
                    res += $" - {Direction.Angle()}";
                }
                else
                {
                    res += $"Sticking - {Direction.Angle()}";
                }
            }
            else
            {
                res += "Unanchored";
                if (Rising) res += "- Rising";
                else if (Falling) res += "- Falling";
            }
            return res;
        }

    }

    public enum JumpPhase
    {
        Grounded,
        Jumping,
        Decelerating,
        Hangtime,
        Neutral,
        Falling,
        TerminalVelocity
    }
}