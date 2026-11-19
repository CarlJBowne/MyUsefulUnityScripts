using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace SLS.Physics3D
{
    /// <summary>
    /// A resolver specifically for use in NavMeshes. This resolver uses a NavMeshAgent to perform movement and pathfinding, and is designed to be used as the final resolver in the chain for characters that should be fully NavMesh-driven. It includes logic to attempt to snap to the NavMesh if the agent becomes ungrounded, and can optionally lock movement to the NavMesh surface when navigating off ledges or small platforms.
    /// </summary>
    [System.Serializable, RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshPhysResolver : PhysicsResolver.GroundType
    {
        [Tooltip("Whether this resolver should attempt to lock movement to the NavMesh surface when navigating off ledges or small platforms. This can help prevent characters from unintentionally walking off of small platforms, but may cause unwanted snapping behavior in some cases.")]
        public bool lockToNavMesh = true;
        [Tooltip("The distance within which the resolver will attempt to snap to the NavMesh if the agent becomes ungrounded. This should generally be set to a value slightly larger than the expected maximum step height of the character.")]
        [field: SerializeField] public float detectionRange { get; private set; } = .35f;
        [field: SerializeField] public PhysicsResolver.GroundType altGroundResolver { get; private set; }

        NavMeshAgent Agent => Body.NavAgent;

        /// <summary>
        /// Moves body via Nav Mesh.
        /// </summary>
        public override void Move(Vector3 stepVelocity)
        {
            if (stepVelocity == Vector3.zero) return;
            Print(() => $"Physics Step {Body.Step} - NavMesh Movement - Velocity {stepVelocity}");

            if (!Agent.Raycast(Position + stepVelocity, out NavMeshHit hit))
            {
                Print(() => $"Nothing hit. Moving against Nav Mesh and Ending Early.");
                Agent.Move(stepVelocity);
                return;
            }
            Print(() => $"Hit Mesh Edge, normal:{hit.normal}.");
            Vector3 snapToSurface = stepVelocity.normalized * hit.distance;

            Agent.Move(snapToSurface);

            if (ContinueCheck(hit.distance)) return;

            Vector3 leftover = stepVelocity - snapToSurface;
            if (lockToNavMesh || altGroundResolver == null)
            {
                leftover = leftover.ProjectAndScale(hit.normal);
                leftover *= Vector3.Dot(leftover.normalized, hit.normal) + 1;
                if (Body.CancelResolverContinuance()) return;
                Next.Move(leftover);
            }
            else
            {
                ChooseNext(altGroundResolver);
                if (Body.CancelResolverContinuance()) return;
                Next.Move(leftover);
            }
        }

        public override bool TryEnter(ref PhysicsResolver result)
        {
            bool navFound = NavMesh.SamplePosition(Position, out NavMeshHit sampleHit, detectionRange, Agent.areaMask);
            Vector3 distance = Position - sampleHit.position;

            if (!navFound || (distance.sqrMagnitude > .01f && Vector3.Dot(Body.Velocity.Global.normalized, distance.normalized) < -.3f))
            {

                if (altGroundResolver && altGroundResolver.TryEnter(ref result)) return true;
                else if (result && result.TryEnter(ref result)) return false;
                else if (Body.airResolver && Body.airResolver.TryEnter(ref result)) return true;
                else
                {
                    if (!NavMesh.SamplePosition(Position, out sampleHit, float.PositiveInfinity, Agent.areaMask))
                    {
                        Body.enabled = false;
                        result = null;
                        return false;
                    }
                }
            }
            Agent.enabled = true;
            destinationDriven = false;

            Agent.Warp(sampleHit.position);
            Agent.nextPosition = sampleHit.position;

            Body.Position = sampleHit.position + (Vector3.up * Agent.baseOffset);

            // We will manage character position ourselves (RB) and use NavAgent for pathfinding only.
            Agent.enabled = true;

            return base.TryEnter(ref result);
        }
        public override void OnExit()
        {
            destinationDriven = false;
            Agent.enabled = false;
        }

        /// <summary>
        /// Whether this resolver is currently controlling movement via NavAgent destination. This is used to track whether the resolver should be outputting the NavAgent's desired velocity and actively moving towards the destination, or if it should be idle and allow other resolvers to control movement until a new destination is set. This is necessary because NavMeshAgents will continue to output a desired velocity even when they are not actively navigating towards a destination, which can cause unwanted movement if not properly managed.
        /// </summary>
        private bool destinationDriven = false;

        /// <summary>
        /// Getter Variant, just returns current Destination.
        /// </summary>
        /// <returns>The current NavDestination, will be zero if there is none.</returns>
        public Vector3 NavDestination() => destinationDriven ? Agent.destination : Vector3.zero;

        /// <summary>
        /// Setter Value Variant. Sets Destination and activates Destination-driven behavior, if possible.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Success.</returns>
        public bool NavDestination(Vector3 value)
        {
            destinationDriven = true;
            Agent.destination = value;
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
                Agent.destination = destinationValue;
                return true;
            }
            else
            {
                destinationDriven = false;
                Agent.ResetPath();
                // keep agent disabled? existing code leaves NavAgent.enabled as-is; we keep existing behavior
                return false;
            }
        }
        /// <summary>
        /// Getter Bool with Output Variant. Returns whether Destination-driven Behavior is active and outs the destination value.
        /// </summary>
        public bool NavDestination(out Vector3 result)
        {
            result = Agent.destination;
            return destinationDriven;
        }

        public override void FixedUpdateFormer()
        {
            if (destinationDriven)
            {
                Body.Direction.Set(Agent.desiredVelocity, Agent.angularSpeed * Time.fixedDeltaTime);
                Agent.velocity = Vector3.zero;

                stepZeroVelocity.Global = (Vector3.Dot(Agent.desiredVelocity, Direction) + 1) * Agent.desiredVelocity.magnitude * (Vector3)Direction;
                if (Agent.remainingDistance < 0.1f) NavDestination(false);
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