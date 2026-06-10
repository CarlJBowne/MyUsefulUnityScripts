using UnityEngine;

namespace SLS.Physics
{
    public abstract partial class PhysicsResolver
    {
        /// <summary>
        /// A resolver based on the <see cref="CollideAndSlide"/> resolver but with all grounded-movement-related logic removed.
        /// </summary>
        [System.Serializable]
        public class Air : PhysicsResolver
        {
            [Tooltip("The distance of the buffer that will be used in sweep checking.")]
            [SerializeField] float checkBuffer = 0.1f;
            [Tooltip("The default gravity value that will be applied to this resolver on Start().")]
            [SerializeField] float defaultGravity = 9.8f;
            [Tooltip("Whether this resolver should automatically apply gravity each frame. If false, gravity must be applied manually by calling ApplyGravity().")]
            [SerializeField] bool autoApplyGravity = false;
            [SerializeField] int landResolverID = -1;

            public override void Move(Vector3 stepVelocity)
            {
                if (stepVelocity == Vector3.zero) return;

                Print(() => $"Physics Step {Body.Step} - Airborne Movement - Velocity {stepVelocity}");

                float stopDistance = -1;
                Vector3 nextNormal = Vector3.zero;
                bool land = false;

                // Sweep for any obstacle in the trajectory (ignore flat-floor hits when moving purely horizontally).
                bool sweepHit = Body.Sweep(stepVelocity, out RaycastHit hit, checkBuffer);

                if (sweepHit) //Hit
                {
                    Print(() => $"Sweep hit: {hit.collider.name} at distance {hit.distance}, normal {hit.normal}");
                    stopDistance = hit.distance;
                    nextNormal = hit.normal;

                    if (Mathf.Approximately(hit.normal.y, 0))
                    {
                        Print(() => $"Hit a wall, normal: {hit.normal}");
                        nextNormal = nextNormal.XZ().normalized;
                    }
                    else if (hit.normal.y > 0)
                    {
                        if (Ground.WithinSlopeAngle(hit.normal))
                        {
                            Print(() => $"Hit landable ground with normal {hit.normal}.");
                            land = true;
                        }
                        else
                        {
                            Print(() => $"Hit steep slope, normal: {hit.normal}.");
                            nextNormal = nextNormal.XZ().normalized;
                            //This ^^^ feels wrong but I'm leaving it in for now. Do Testing Please.
                        }
                    }
                    else
                    {
                        if (Ground.WithinSlopeAngle(-hit.normal))
                        {
                            Print(() => $"Hit a ceiling, normal: {hit.normal}. BONK.");
                            Body.Velocity.y = 0;
                            //BONK (Implement later)
                        }
                        else
                        {
                            Print(() => $"Hit an inward curve, normal: {hit.normal}. Try to Slide up.");
                            nextNormal = nextNormal.XZ().normalized;
                            //This ^^^ feels wrong but I'm leaving it in for now. Do Testing Please.
                        }
                    }

                    // If we hit a valid ground surface and are moving downwards or flat, land on it.
                    if (hit.normal.y > 0 && Ground.WithinSlopeAngle(hit.normal) && stepVelocity.y <= 0) Ground.Land(hit);
                }
                else
                {

                    if (!Body.Sweep(Vector3.down * 5000, out _, checkBuffer, Position + stepVelocity, QueryTriggerInteraction.Collide))
                    {
                        Print(() => "Entered Void Zone. Treating as Horizontal Bonk.");
                        Body.LastChanceStopper(stepVelocity.XZ(), nextNormal.XZ());
                        return;
                        // If going forward will put this body over the void, don't move at all.
                    }
                    Print(() => "No sweep hit. Ending Early.");
                    Body.Position += stepVelocity;
                    return;
                }


                Vector3 snapToSurface = stopDistance != -1 ? stepVelocity.normalized * stopDistance : stepVelocity;

                Body.Position += snapToSurface;

                if (ContinueCheck(stopDistance)) return;
                else if (Body.LastChanceStopper(stepVelocity.XZ(), nextNormal.XZ())) return;

                Vector3 leftover = stepVelocity - snapToSurface;
                Vector3 newDir = leftover.ProjectAndScale(nextNormal);
                newDir *= Vector3.Dot(leftover.normalized, nextNormal) + 1;

                if (land && landResolverID != -1) // Don't do landing logic if no ground-based resolvers exist.
                {
                    leftover.y = 0;
                    Ground.Land(hit);
                    ChooseNext(landResolverID);
                }
                Next.Move(newDir);
            }

            public override void Start() => gravity = defaultGravity;

            public override void FixedUpdateLatter() { if (autoApplyGravity) ApplyGravity(); }

            /// <summary>
            /// Runs the calculations to automatically apply the current gravity to this body.
            /// </summary>
            public void ApplyGravity() => Body.Velocity.u -= gravity * Time.fixedDeltaTime;

            /// <summary>
            /// The active gravity value. (Inverted. y=1 is down.)
            /// </summary>
            private float gravity = 9.8f;

            /* 3D Gravity (Not Necessary for this project.)
            /// <summary>
            /// The active gravity value. (Inverted. y=1 is down.)
            /// </summary>
            [NonSerialized] private Vector3 gravity = new(0, 9.8f, 0);

            /// <summary>
            /// Returns the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
            /// </summary>
            public Vector3 Get3DGravity() => gravity;
            /// <summary>
            /// Returns the current gravity value on the Y axis. (Inverted. 1 is downwards, -1 is upwards.)
            /// </summary>
            public float GetGravity() => gravity.y;

            /// <summary>
            /// Sets the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
            /// </summary>
            /// <param name="newGravity">The new gravity value.</param>
            public void SetGravity(Vector3 newGravity) => gravity = newGravity;
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
}