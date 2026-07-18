using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace SLS.Physics3D
{
    /// <summary>
    /// A resolver specifically for use in NavMeshes. This resolver uses a NavMeshAgent to perform movement and pathfinding, and is designed to be used as the final resolver in the chain for characters that should be fully NavMesh-driven. It includes logic to attempt to snap to the NavMesh if the agent becomes ungrounded, and can optionally lock movement to the NavMesh surface when navigating off ledges or small platforms.
    /// </summary>
    [System.Serializable]
    public class NavMeshPhysResolver : PhysicsResolver
    {
        [Tooltip("Whether this resolver should attempt to lock movement to the NavMesh surface when navigating off ledges or small platforms. This can help prevent characters from unintentionally walking off of small platforms, but may cause unwanted snapping behavior in some cases.")]
        public bool lockToNavMesh = true;
        [Tooltip("The distance within which the resolver will attempt to snap to the NavMesh if the agent becomes ungrounded. This should generally be set to a value slightly larger than the expected maximum step height of the character.")]
        [field: SerializeField] public float detectionRange { get; private set; } = .35f;
        [field: SerializeField] public PhysicsResolver nonNavResolver { get; private set; }
        [field: SerializeField] public PhysicsResolver airborneResolver { get; private set; }

        NavMeshAgent NavAgent => Body.NavAgent;

        /// <summary>
        /// Moves body via Nav Mesh.
        /// </summary>
        public override void Move(Vector3 stepVelocity)
        {
            if (stepVelocity == Vector3.zero) return;
            Print(() => $"Physics Step {Body.Step} - NavMesh Movement - Velocity {stepVelocity}");

            if (!NavAgent.Raycast(Position + stepVelocity, out NavMeshHit hit))
            {
                Print(() => $"Nothing hit. Moving against Nav Mesh and Ending Early.");
                NavAgent.Move(stepVelocity);
                return;
            }
            Print(() => $"Hit Mesh Edge, normal:{hit.normal}.");
            Vector3 snapToSurface = stepVelocity.normalized * hit.distance;

            NavAgent.Move(snapToSurface);

            if (ContinueCheck(hit.distance)) return;

            Vector3 leftover = stepVelocity - snapToSurface;
            if (lockToNavMesh || nonNavResolver == null)
            {
                leftover = leftover.ProjectAndScale(hit.normal);
                leftover *= Vector3.Dot(leftover.normalized, hit.normal) + 1;
                Next.Move(leftover);
            }
            else
            {
                ChooseNext(nonNavResolver);
                Next.Move(leftover);
            }
        }

        public override void Enter()
        {

            if (NavAgent == null
                || !NavMesh.SamplePosition(Position, out NavMeshHit sampleHit, detectionRange, NavAgent.areaMask)
                || Vector3.Dot(Body.Velocity.Global.normalized, (Position - sampleHit.position).normalized) < -.3f)
            {
                if (!nonNavResolver && !airborneResolver)
                {
                    lockToNavMesh = true;
                    if(!NavMesh.SamplePosition(Position, out sampleHit, float.PositiveInfinity, NavAgent.areaMask))
                    {
                        Body.enabled = false;
                        return;
                    }
                }
                else
                {
                    if (nonNavResolver && Ground.Check(out _, false)) ChooseNext(nonNavResolver);
                    else
                    {
                        if (airborneResolver) ChooseNext(airborneResolver);
                        else
                        {
                            if (nonNavResolver && Ground.InstantSnapToFloor(out _)) ChooseNext(nonNavResolver);
                            else Body.enabled = false;
                        }
                    }

                    return;
                }
            }
            NavAgent.enabled = true;
            destinationDriven = false;
            // Place agent internal position on the navmesh
            NavAgent.Warp(sampleHit.position);
            NavAgent.nextPosition = sampleHit.position;
            // Place the RB at the same surface + baseOffset so visuals/physics line up
            Body.Position = sampleHit.position + Vector3.up * NavAgent.baseOffset;

            // We will manage character position ourselves (RB) and use NavAgent for pathfinding only.
            NavAgent.enabled = true;
        }
        public override void Exit()
        {
            destinationDriven = false;
            NavAgent.enabled = false;
        }

        /// <summary>
        /// Whether this resolver is currently controlling movement via NavAgent destination. This is used to track whether the resolver should be outputting the NavAgent's desired velocity and actively moving towards the destination, or if it should be idle and allow other resolvers to control movement until a new destination is set. This is necessary because NavMeshAgents will continue to output a desired velocity even when they are not actively navigating towards a destination, which can cause unwanted movement if not properly managed.
        /// </summary>
        private bool destinationDriven = false;

        /// <summary>
        /// Getter Variant, just returns current Destination.
        /// </summary>
        /// <returns>The current NavDestination, will be zero if there is none.</returns>
        public Vector3 NavDestination() => destinationDriven ? NavAgent.destination : Vector3.zero;

        /// <summary>
        /// Setter Value Variant. Sets Destination and activates Destination-driven behavior, if possible.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Success.</returns>
        public bool NavDestination(Vector3 value)
        {
            destinationDriven = true;
            NavAgent.destination = value;
            return true;
        }
        /// <summary>
        /// Setter Activation Variant. Activates/Deactivates Destination-driven Behavior. Destination value is optional to allow False Setting.
        /// </summary>
        public bool NavDestination(bool value, Vector3 destinationValue = default)
        {
            if (value)
            {
                destinationDriven = true;
                NavAgent.destination = destinationValue;
                return true;
            }
            else
            {
                destinationDriven = false;
                NavAgent.ResetPath();
                // keep agent disabled? existing code leaves NavAgent.enabled as-is; we keep existing behavior
                return false;
            }
        }
        /// <summary>
        /// Getter Bool with Output Variant. Returns whether Destination-driven Behavior is active and outs the destination value.
        /// </summary>
        public bool NavDestination(out Vector3 result)
        {
            result = NavAgent.destination;
            return destinationDriven;
        }

        public override void FixedUpdateFormer()
        {
            if (destinationDriven)
            {
                Body.Direction.Set(NavAgent.desiredVelocity, NavAgent.angularSpeed * Time.fixedDeltaTime);
                NavAgent.velocity = Vector3.zero;

                stepZeroVelocity.Global = (Vector3.Dot(NavAgent.desiredVelocity, direction) + 1) * NavAgent.desiredVelocity.magnitude * (Vector3)direction;
                if (NavAgent.remainingDistance < 0.1f) NavDestination(false);
            }
        }

        public override void Reset()
        {
            base.Reset();
            if (Body.NavAgent == null) Body.NavAgent = GetComponent<NavMeshAgent>();
            if (Body.NavAgent == null) Body.NavAgent = gameObject.AddComponent<NavMeshAgent>();
        }
    }

}