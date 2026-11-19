using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR;

namespace SLS.Physics3D
{
    /// <summary> 
    /// A resolver representing the famed "Collide and Slide" algorithm. This resolver performs a single collision sweep for the proposed movement vector, moves the body to the point of impact (or full distance if no collision), and then delegates remaining movement along the surface normal of the collision.
    /// </summary>
    [System.Serializable]
    public class CollideAndSlidePhysResolver : PhysicsResolver.GroundType
    {
        [Tooltip("The distance of the buffer that will be used in sweep checking.")]
        [SerializeField] float checkBuffer = 0.1f;
        [Tooltip("The distance the player will snap downwards when walking.")]
        [SerializeField] float downSnap = 0.08f;
        [Tooltip("A Layermask for solid ground.")]
        [SerializeField] LayerMask validGroundMask;
        [field: SerializeField] public PhysicsResolver.GroundType altGroundResolver { get; private set; }



        public override bool TryEnter(ref PhysicsResolver result)
        {
            if(!Body.Anchor.SweepStandable(out SweepPayload hit))
            {
                if (altGroundResolver && altGroundResolver.TryEnter(ref result)) return true;
                else if (result && result.TryEnter(ref result)) return false;
                else if (Body.airResolver && Body.airResolver.TryEnter(ref result)) return true;

            }
            return base.TryEnter(ref result);
        }

        public override void Move(Vector3 stepVelocity)
        {
            if (stepVelocity.sqrMagnitude < float.Epsilon) return;

            Print(() => $"Physics Step {Body.Step} - Collide and Slide - Velocity {stepVelocity}");

            stepVelocity = stepVelocity.ProjectAndScale(CurrentAnchor.normal);

            float stopDistance = -1;
            Vector3 nextNormal = Vector3.zero;

            // Sweep for any obstacle in the trajectory (ignore flat-floor hits when moving purely horizontally).
            var hit = Body.Sweep(stepVelocity, false, checkBuffer);

            if (!hit)
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

                // Make sure we aren't moving off into the void at the destination
                //if (!Body.Sweep(Vector3.down * 5000, out RaycastHit _, checkBuffer, Position + stepVelocity, QueryTriggerInteraction.Collide)) return;

                // Snap down to a slightly lower ground if detected (small ledge correction).
                if (Body.Sweep(-Body.Direction.Up, out SweepPayload downHit)
                    && downHit.distance < downSnap)
                {
                    Ray cornerCheckRay = new(downHit.barycentricCoordinate + new Vector3(0, .1f, 0), Vector3.down);
                    if (downHit.anchorPoint.collider.Raycast(cornerCheckRay, out RaycastHit baryHit, .11f)
                        && baryHit.normal != downHit.normal)
                    {
                        Print(() => $"Snapping down at near platform or slope {downHit.distance}");
                        Body.Position += stepVelocity;
                        Body.Position += -Body.Direction.Up * downHit.distance;
                        Anchor.Anchor(downHit.anchorPoint);
                    }
                }
                else
                {
                    Body.Position += stepVelocity;
                    Body.WalkOff();
                }
                return;
            }

            stopDistance = hit.distance;
            nextNormal = hit.normal;

            if (ContinueCheck(stopDistance)) return;

            Vector3 move = stepVelocity.normalized * stopDistance;
            Body.Position += move;
            Vector3 leftover = stepVelocity - move;

            onCollision?.Collide(hit);

            if (ContinueCheck(stopDistance) || Body.CancelResolverContinuance()) return;
            switch (hit.angle)
            {
                case AnchorPoint.Angle.Floor:
                    Print(() => $"Hit flatish ground with normal {hit.normal}.");
                    Anchor.Anchor(hit.anchorPoint);
                    AlterLeftover(nextNormal, false, false);
                    break;
                case AnchorPoint.Angle.Slope:
                    Print(() => $"Hit sloped ground with normal {hit.normal}.");
                    Anchor.Anchor(hit.anchorPoint);
                    AlterLeftover(nextNormal, false, false);
                    break;
                case AnchorPoint.Angle.SteepSlope:
                    Print(() => $"Hit steep slope, normal: {hit.normal}.");
                    AlterLeftover(nextNormal.XZ().normalized, true, true);
                    break;
                case AnchorPoint.Angle.Wall:
                    Print(() => $"Hit a wall, normal: {hit.normal}");
                    AlterLeftover(nextNormal.XZ().normalized, true, true);
                    break;
                case AnchorPoint.Angle.InvertedSlope:
                    Print(() => $"Hit an inward curve, normal: {hit.normal}. Try to Slide up.");
                    AlterLeftover(nextNormal.XZ().normalized, true, true);
                    break;
                case AnchorPoint.Angle.Ceiling:
                    AlterLeftover(nextNormal.XZ().normalized, true, true);
                    break;
                default: throw new System.Exception($"Unknown AnchorPoint.Type {hit.angle} for normal {hit.normal}");
            }
            void AlterLeftover(Vector3 nextNormal, bool flatten, bool scaleByDot)
            {
                leftover = leftover.ProjectAndScale(nextNormal);
                if (flatten) leftover.y = 0;
                if (scaleByDot) leftover *= Vector3.Dot(leftover.normalized, nextNormal) + 1;
            }

            Print(() => $"Beginning next step. Leftover: {leftover}");
            ChooseNext(Body.groundResolver);
            if (Body.CancelResolverContinuance()) return;
            Next.Move(leftover);
        }
    }

}