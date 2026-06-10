using UnityEngine;

namespace SLS.Physics
{
    public abstract partial class PhysicsResolver
    {
        /// <summary>
        /// A resolver representing the famed "Collide and Slide" algorithm. This resolver performs a single collision sweep for the proposed movement vector, moves the body to the point of impact (or full distance if no collision), and then delegates remaining movement along the surface normal of the collision.
        /// </summary>
        [System.Serializable]
        public class CollideAndSlide : PhysicsResolver
        {
            [Tooltip("The distance of the buffer that will be used in sweep checking.")]
            [SerializeField] float checkBuffer = 0.1f;
            [Tooltip("The distance the player will snap downwards when walking.")]
            [SerializeField] float downSnap = 0.08f;
            [Tooltip("A Layermask for solid ground.")]
            [SerializeField] LayerMask validGroundMask;

            public override void Move(Vector3 stepVelocity)
            {
                if (stepVelocity.sqrMagnitude < float.Epsilon) return;

                Print(() => $"Physics Step {Body.Step} - Collide and Slide - Velocity {stepVelocity}");

                stepVelocity = stepVelocity.ProjectAndScale(anchor.normal);

                float stopDistance = -1;
                Vector3 nextNormal = Vector3.zero;
                bool scaleByDot = false;
                bool negateVerticalLefover = false;

                // Sweep for any obstacle in the trajectory (ignore flat-floor hits when moving purely horizontally).
                bool sweepHit = Body.Sweep(stepVelocity, out RaycastHit hit, checkBuffer);

                if (!sweepHit) //No Hit
                {
                    /*
                    // Keep platform lock behavior for grounded movement (attempt to detect unreachable edges and snap behavior).
                    if (lockToNavMesh)
                    {
                        Vector3 platformCheckDistance = stepVelocity.normalized * platformDetectionFactor;

                        if (!SweepBody(Vector3.down * checkBuffer, out RaycastHit platformCheckHit,
                            checkBuffer, Position + platformCheckDistance))
                        {
                            Vector3 reachAroundPos = Position + (platformCheckDistance * 1.01f) - (Vector3.up * Collider.height / 2);
                            if (SweepBody(platformCheckDistance.XZ() * -2f, out RaycastHit reachAroundResult, 0, reachAroundPos))
                            {
                                nextNormal = -reachAroundResult.normal.XZ();
                                Plane P = new(nextNormal, reachAroundResult.point + (nextNormal * .6f));
                                P.Raycast(new(Position, stepVelocity), out float hitDistance);
                                if (hitDistance <= stepVelocity.magnitude) stopDistance = hitDistance;

                                scaleByDot = true;
                                AddDebugText($"Platform Locked onto non-NavMesh Platform, nextNormal: {nextNormal}");
                            }
                            else AddDebugText("Walking off platform when not allowed but reach around check failed. Failsafe situation, report to CJ.");
                        }
                    }*/

                    Print(() => $"Didn't hit anything.");

                    if (stopDistance == -1)
                    {
                        // Snap down to a slightly lower ground if detected (small ledge correction).
                        if (Body.Sweep(Vector3.down, out RaycastHit downHit, Body.Ground.groundCheckBuffer) && downHit.distance < downSnap)
                        {
                            if (!CheckCorner(downHit))
                            {
                                Print(() => $"Snapping down at near platform or slope {downHit.distance}");
                                Body.Position += Vector3.down * downHit.distance;
                                Ground.Land(downHit);
                            }
                        }
                        else Body.WalkOff();

                        bool CheckCorner(RaycastHit downHit)
                        {
                            Ray cornerCheckRay = new(downHit.barycentricCoordinate + new Vector3(0, .1f, 0), Vector3.down);
                            bool different = downHit.collider.Raycast(cornerCheckRay, out RaycastHit baryHit, .11f)
                                && baryHit.normal != downHit.normal;
                            return different;
                        }
                    }
                }
                else // Hit
                {
                    stopDistance = hit.distance;
                    nextNormal = hit.normal;

                    if (Mathf.Approximately(hit.normal.y, 0)) // Hit a Wall
                    {
                        Print(() => $"Hit a wall, normal: {hit.normal}");
                        scaleByDot = true;
                        negateVerticalLefover = true;
                        nextNormal = nextNormal.XZ().normalized;
                    }
                    else if (hit.normal.y > 0 && !Ground.WithinSlopeAngle(hit.normal)) // Hit a steep slope
                    {
                        Print(() => $"Hit a steep slope, normal: {hit.normal}");
                        scaleByDot = true;
                        negateVerticalLefover = true;
                        nextNormal = nextNormal.XZ().normalized;
                    }

                    if (anchor.normal.y > 0 && hit.normal.y < 0) FloorCeilingLock(anchor, hit.normal);
                    else if (anchor.normal.y < 0 && hit.normal.y > 0) FloorCeilingLock(hit.normal, anchor);

                    void FloorCeilingLock(Vector3 floorNormal, Vector3 ceilingNormal)
                    {
                        Print(() => $"Floor/Ceiling Lock Triggered. Floor Normal: {floorNormal}, Ceiling Normal: {ceilingNormal}");
                        scaleByDot = true;
                        nextNormal = floorNormal.y != floorNormal.magnitude ? floorNormal : ceilingNormal;
                    }

                    // If we hit a valid ground surface and are moving downwards or flat, land on it.
                    if (hit.normal.y > 0 && Ground.WithinSlopeAngle(hit.normal))
                    {
                        Print(() => $"Found Landable ground, normal: {nextNormal}");
                        Ground.Land(hit);
                    }
                }

                Vector3 snapToSurface = stopDistance != -1 ? stepVelocity.normalized * stopDistance : stepVelocity;

                // Make sure we aren't moving off into the void at the destination
                if (!Body.Sweep(Vector3.down * 5000, out _, checkBuffer, snapToSurface, QueryTriggerInteraction.Collide)) return;

                Body.Position += snapToSurface;

                if (ContinueCheck(stopDistance)) return;
                else if (Body.LastChanceStopper(stepVelocity.XZ(), nextNormal.XZ())) return;

                Vector3 leftover = stepVelocity - snapToSurface;
                Print(() => $"Beginning next step. Leftover: {leftover}");
                if (negateVerticalLefover)
                {
                    leftover.y = 0;
                    Ground.Land(hit);
                }
                Vector3 newDir = leftover.ProjectAndScale(nextNormal);
                if (scaleByDot) newDir *= Vector3.Dot(leftover.normalized, nextNormal) + 1;

                ChooseNext();
                Next.Move(newDir);
            }
        }
    }
}