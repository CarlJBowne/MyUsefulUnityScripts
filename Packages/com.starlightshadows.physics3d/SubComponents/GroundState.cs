using System;
using System.Collections.Generic;
using UnityEngine;

namespace SLS.Physics3D
{
    /// <summary>
    /// <see cref="PhysicsBody"/> Sub-component that tracks whether the body is grounded and relevant information about the ground contact (normal, slope, collider, etc.). This component also provides helper methods for performing ground checks and transitioning between grounded and airborne states.
    /// </summary>
    [System.Serializable]
    public class GroundState : PhysicsSubComponent
    {
        #region Config
        /// <summary>
        /// The buffer (in world units) used when performing a downwards sweep to
        /// determine whether the body is grounded. Small positive values help
        /// tolerate minor geometry gaps and numerical jitter.
        /// </summary>
        [field: SerializeField] public float groundCheckBuffer { get; private set; } = 0.1f;

        /// <summary>
        /// The maximum allowed slope angle (in degrees) a surface can have for the
        /// body to be considered standable. This is compared against the surface
        /// normal using Vector3.Angle to Vector3.up.
        /// </summary>
        [field: SerializeField] public float maxSlopeNormalAngle { get; private set; } = 45f;

        #endregion

        /// <summary>
        /// Construct a new GroundState value container. Use <see cref="Init"/>
        /// to attach this state to its owning <see cref="PhysicsBody"/>.
        /// </summary>
        /// <param name="input">The initial ground value.</param>
        public GroundState(Values input) => value = input;

        /// <summary>
        /// The possible ground-related states for a body describing whether it is
        /// standing, airborne, in hangtime, etc.
        /// </summary>
        public enum Values
        {
            Grounded = 0,
            Jumping = 1,
            Decelerating = 2,
            Hangtime = 3,
            Falling = 4,
            TerminalVelocity = 5
        }
        /// <summary>
        /// The current ground state value.
        /// </summary>
        public Values value { get; private set; }

        /// <summary>
        /// The anchor point representing the last ground contact (point, normal, collider).
        /// </summary>
        public AnchorPoint anchor { get; private set; }

        /// <summary>
        /// If the current ground collider implements <see cref="IMovablePlatform"/>,
        /// this property will cache that interface for convenient platform-relative
        /// motion handling.
        /// </summary>
        public IMovablePlatform movingAnchor { get; private set; }


        /// <summary>
        /// Transition into the grounded state using <paramref name="newAnchorPoint"/>
        /// as the contact anchor. This updates velocity (vertical component becomes 0),
        /// sets the moving anchor if available and invokes <see cref="PhysicsBody.OnLand"/>.
        /// </summary>
        /// <param name="newAnchorPoint">The Raycast/contact information representing the ground.</param>
        public void Land(AnchorPoint newAnchorPoint)
        {
            if (!HasOwner) return;
            bool wasntGrounded = value != Values.Grounded;
            bool objectChange = anchor.collider != newAnchorPoint.collider;

            if (!wasntGrounded && !objectChange) return;

            value = Values.Grounded;
            anchor = newAnchorPoint;
            Body.Velocity.y = 0;

            if (objectChange)
            {
                movingAnchor?.RemoveBody(Body);
                movingAnchor = newAnchorPoint.collider.GetComponent<IMovablePlatform>();
                movingAnchor?.AddBody(Body);
            }

            if (wasntGrounded)
            {

            }

            Body.OnLand(wasntGrounded, objectChange);
            //OnNavMesh = true;
        }
        /// <summary>
        /// Convenience overload that performs a ground check and Lands on the first
        /// valid detected surface.
        /// </summary>
        public void Land()
        {
            if (!HasOwner) return;
            if (!Check(out AnchorPoint groundHit)) return;
            Land(groundHit);
        }
        /// <summary>
        /// Transitions out of the grounded state into an airborne state specified by
        /// <paramref name="newState"/>. Clears anchor and moving anchor references
        /// and calls <see cref="PhysicsBody.OnUnLand"/>.
        /// </summary>
        /// <param name="newState">The airborne state to transition into. Must be >= Jumping.</param>
        public void UnLand(Values newState = Values.Falling)
        {
            if (!HasOwner) return;
            if (newState < Values.Jumping) return;
            value = newState;
            anchor = AnchorPoint.Null;
            if (movingAnchor != null)
            {
                movingAnchor?.RemoveBody(Body);
                movingAnchor = null;
            }
            Body.OnUnLand(newState);
            //OnNavMesh = false;
        }

        /// <summary>
        /// Checks if the character is grounded and outputs the ground hit information.
        /// </summary>
        /// <param name="groundHit">The anchor point of the ground hit.</param>
        /// <returns>True if grounded, false otherwise.</returns>
        /// <summary>
        /// Performs a sweep downwards to determine whether the body is currently
        /// grounded. Returns the detected anchor point (if any).
        /// </summary>
        /// <param name="groundHit">Outputs the AnchorPoint detected or AnchorPoint.Null if none found.</param>
        /// <param name="dontApply">When true, prevents certain post-processing side-effects in callers (unused here).</param>
        /// <returns>True when a standable surface was detected beneath the body.</returns>
        public bool Check(out AnchorPoint groundHit, bool dontApply = false)
        {
            bool result = Body.Sweep(Vector3.down * groundCheckBuffer, out RaycastHit raycast, groundCheckBuffer) && WithinSlopeAngle(raycast.normal);
            groundHit = AnchorPoint.Null;
            if (!dontApply) groundHit = raycast;
            return result;
        }
        /// <summary>
        /// Checks if the character is grounded and outputs the ground hit information.
        /// </summary>
        /// <param name="groundHit">The anchor point of the ground hit.</param>
        /// <returns>True if grounded, false otherwise.</returns>
        /// <summary>
        /// Performs a sweep downwards to determine whether the body is currently
        /// grounded and returns both an AnchorPoint and the raw RaycastHit.
        /// </summary>
        /// <param name="groundHit">Outputs the AnchorPoint detected or AnchorPoint.Null if none found.</param>
        /// <param name="raycast">Outputs the raw RaycastHit from the internal sweep.</param>
        /// <param name="dontApply">When true, prevents certain post-processing side-effects in callers (unused here).</param>
        /// <returns>True when a standable surface was detected beneath the body.</returns>
        public bool Check(out AnchorPoint groundHit, out RaycastHit raycast, bool dontApply = false)
        {
            bool result = Body.Sweep(Vector3.down * groundCheckBuffer, out raycast, groundCheckBuffer) && WithinSlopeAngle(raycast.normal);
            groundHit = AnchorPoint.Null;
            if (!dontApply) groundHit = raycast;
            return result;
        }

        /// <summary>
        /// Instantly snaps the character to the floor below, if any, and outputs the hit information.
        /// </summary>
        /// <param name="hit">The RaycastHit of the floor.</param>
        /// <returns>True if snapped to floor, false otherwise.</returns>
        /// <summary>
        /// Attempts an immediate snap to the floor by sweeping a long distance downwards
        /// and moving the body to the detected surface when present. Useful for initial
        /// positioning in Awake.
        /// </summary>
        /// <param name="hit">Outputs the RaycastHit that was used for the snap.</param>
        /// <returns>True if a floor was found and the body was moved, otherwise false.</returns>
        public bool InstantSnapToFloor(out RaycastHit hit)
        {
            if (Body.Sweep(Vector3.down * 1000, out hit, .5f))
            {
                Body.Position += Vector3.down * hit.distance;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Determines if the given normal is within the allowed slope angle.
        /// </summary>
        /// <param name="inNormal">The normal to check.</param>
        /// <returns>True if within the slope angle, false otherwise.</returns>
        /// <summary>
        /// Returns true if the supplied normal corresponds to a slope that is less
        /// steep than <see cref="maxSlopeNormalAngle"/>.
        /// </summary>
        /// <param name="inNormal">Surface normal to evaluate.</param>
        /// <returns>True for standable slopes.</returns>
        public bool WithinSlopeAngle(Vector3 inNormal) => Vector3.Angle(Vector3.up, inNormal) < maxSlopeNormalAngle;


        #region Comparison

        public static implicit operator bool(GroundState This) => This.value == Values.Grounded;
        public static implicit operator Values(GroundState This) => This.value;
        public static implicit operator GroundState(Values input) => new(input);

        public static bool operator ==(GroundState This, GroundState other) => This.value == other.value;
        public static bool operator !=(GroundState This, GroundState other) => This.value != other.value;
        public static bool operator ==(GroundState This, Values other) => This.value == other;
        public static bool operator !=(GroundState This, Values other) => This.value != other;

        public const Values Grounded = Values.Grounded;
        public const Values Jumping = Values.Jumping;
        public const Values Decelerating = Values.Decelerating;
        public const Values Hangtime = Values.Hangtime;
        public const Values Falling = Values.Falling;
        public const Values TerminalVelocity = Values.TerminalVelocity;

        public override bool Equals(object obj) => obj is GroundState state && value == state.value && EqualityComparer<AnchorPoint>.Default.Equals(anchor, state.anchor);
        public override int GetHashCode() => HashCode.Combine(value, anchor);

        #endregion
    }
}