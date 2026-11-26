using UnityEngine;

namespace SLS.Physics2D
{
    /// <summary>
    /// A resolver based on the <see cref="CollideAndSlide"/> resolver but with all grounded-movement-related logic removed.
    /// </summary>
    [System.Serializable]
    public class AirPhysResolver : PhysicsResolver.AirType
    {
        [Tooltip("The distance of the buffer that will be used in sweep checking.")]
        [SerializeField] float checkBuffer = 0.1f;
        [Tooltip("The default gravity value that will be applied to this resolver on Start().")]
        [SerializeField] float defaultGravity = 9.8f;
        [Tooltip("Whether this resolver should automatically apply gravity each frame. If false, gravity must be applied manually by calling ApplyGravity().")]
        [SerializeField] bool autoApplyGravity = false;

        public override void Move(Vector2 stepVelocity, Vector2 normal)
        {
            if (stepVelocity.sqrMagnitude < float.Epsilon) return;

            Print(() => $"Physics Step {Body.Step} - Airborne Movement - Velocity {stepVelocity}");

            float stopDistance = -1;
            bool land = false;

            // Sweep for any obstacle in the trajectory (ignore flat-floor hits when moving purely horizontally).
            PhysicsSweep hit = Body.Sweep(stepVelocity);

            if (hit)
            {
                //If Collision Override manually disables the hit flag, recalculate the Collision.
                //This should make it fairly easy to implement Metroid Speed Booster/Screw Attack style functionality.
                Body.CollisionOverride(ref hit);
                if (!hit) hit = Body.Sweep(stepVelocity);
            }

            if (!hit)
            {
                //if (!Body.Sweep(Vector2.down * 5000, checkBuffer, Position + stepVelocity, QueryTriggerInteraction.Collide))
                //{
                //    Print(() => "Entered Void Zone. Treating as Horizontal Bonk.");
                //    return;
                //    // If going forward will put this body over the void, don't move at all.
                //}
                Print(() => "No sweep hit. Ending Early.");
                Body.Position += stepVelocity;
                return;
            }

            Print(() => $"Sweep hit: {hit.anchorPoint.Collider.gameObject.name} at distance {hit.distance}, normal {hit.normal}");
            stopDistance = hit.distance;

            Vector2 snapToSurface = stepVelocity.normalized * stopDistance;
            Body.Position += snapToSurface;

            Vector2 leftover = stepVelocity - snapToSurface;
            Vector2 newDir;

            if (ContinueCheck(stopDistance) || Body.CancelResolverContinuance()) return;
            switch (hit.angle)
            {
                case AnchorPoint.Angle.Floor:
                    Print(() => $"Hit flatish ground with normal {hit.normal}.");
                    land = true;
                    newDir = ProjectOntoSurface(leftover, hit.normal);
                    break;
                case AnchorPoint.Angle.Slope:
                    Print(() => $"Hit sloped ground with normal {hit.normal}.");
                    land = true;
                    newDir = ProjectOntoSurface(leftover, hit.normal);
                    break;
                case AnchorPoint.Angle.SteepSlope:
                    Print(() => $"Hit steep slope, normal: {hit.normal}.");
                    newDir = ProjectOntoSurface(new(0, leftover.y), hit.normal);
                    break;
                case AnchorPoint.Angle.Wall:
                    Print(() => $"Hit a wall, normal: {hit.normal}");
                    newDir = ProjectOntoSurface(new(0, leftover.y), hit.normal);
                    break;
                case AnchorPoint.Angle.InvertedSlope:
                    Print(() => $"Hit an inward curve, normal: {hit.normal}. Try to Slide up.");
                    newDir = ProjectOntoSurface(leftover, hit.normal);
                    break;
                case AnchorPoint.Angle.Ceiling:
                    Print(() => $"Hit a ceiling, normal: {hit.normal}. BONK.");
                    newDir = ProjectOntoSurface(new(leftover.x, 0), hit.normal);
                    Body.Velocity.y = 0;
                    break;
                default: throw new System.Exception($"Unknown AnchorPoint.Type {hit.angle} for normal {hit.normal}");
            }

            if (land && Body.groundResolver) // Don't do landing logic if no ground-based resolvers exist.
            {
                Anchor.Land(hit);
                if (Next == this) ChooseNext(Body.groundResolver);
            }
            Next.Move(newDir, hit.normal);
        }

        public override void OnStart()
        {
            base.OnStart();
            gravity = defaultGravity;
        }

        public override void FixedUpdateLatter() { if (autoApplyGravity) ApplyGravity(); }

        /// <summary>
        /// Runs the calculations to automatically apply the current gravity to this body.
        /// </summary>
        public void ApplyGravity() => Body.Velocity.u -= gravity * Time.fixedDeltaTime;

        /// <summary>
        /// The active gravity value. (Inverted. y=1 is down.)
        /// </summary>
        private float gravity = 9.8f;

        /* 2D Gravity (Not Necessary for this project.)
        /// <summary>
        /// The active gravity value. (Inverted. y=1 is down.)
        /// </summary>
        [NonSerialized] private Vector2 gravity = new(0, 9.8f, 0);

        /// <summary>
        /// Returns the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
        /// </summary>
        public Vector2 Get2DGravity() => gravity;
        /// <summary>
        /// Returns the current gravity value on the Y axis. (Inverted. 1 is downwards, -1 is upwards.)
        /// </summary>
        public float GetGravity() => gravity.y;

        /// <summary>
        /// Sets the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
        /// </summary>
        /// <param name="newGravity">The new gravity value.</param>
        public void SetGravity(Vector2 newGravity) => gravity = newGravity;
        /// <summary>
        /// Sets the current gravity value on the Y axis. (Inverted. 1 is downwards, -1 is upwards.)
        /// </summary>
        /// <param name="newGravity">The new gravity value.</param>
        public void SetGravity(float newGravity) => gravity = new(0, newGravity, 0);
        /// <summary>
        /// Sets the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
        /// </summary>
        /// <param name="newX"> The new gravity value on the x axis. (1 = left.) </param>
        /// <param name="newY"> The new gravity value on the y axis. (1 = down.) </param>
        /// <param name="newZ"> The new gravity value on the z axis. (1 = back.) </param>
        public void SetGravity(float newX, float newY, float newZ) => gravity = new(newX, newY, newZ);
        */

    }
}