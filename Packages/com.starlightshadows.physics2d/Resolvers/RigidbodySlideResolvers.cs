using System.Collections.Generic;
using UnityEngine;

namespace SLS.Physics2D
{
    internal static class RigidbodySlideResolverUtility
    {
        public static Rigidbody2D.SlideResults Move(
            PhysicsResolver resolver,
            MovementBody body,
            Vector2 stepDisplacement,
            float surfaceSlideAngle,
            float surfaceAnchorDistance,
            int maxIterations,
            LayerMask layerMask,
            bool useLayerMask)
        {
            float deltaTime = Time.fixedDeltaTime;
            if (deltaTime <= 0f)
                return default;

            Vector2 surfaceUp = body.Direction.Up.sqrMagnitude > Mathf.Epsilon
                ? body.Direction.Up.normalized
                : Vector2.up;

            Rigidbody2D.SlideMovement movement = new()
            {
                surfaceUp = surfaceUp,
                surfaceSlideAngle = surfaceSlideAngle,
                surfaceAnchor = body.Anchor.Anchored
                    ? -surfaceUp * Mathf.Max(0f, surfaceAnchorDistance)
                    : Vector2.zero,
                gravity = Vector2.zero,
                maxIterations = Mathf.Max(1, maxIterations),
                useNoMove = false,
                useSimulationMove = true,
                useAttachedTriggers = false
            };

            if (useLayerMask)
                movement.SetLayerMask(layerMask);

            Rigidbody2D.SlideResults results = body.RB.Slide(
                stepDisplacement / deltaTime,
                deltaTime,
                movement);

            body.EnforceSkinDistance();

            List<RaycastHit2D> contacts = new(2);
            AddContact(results.surfaceHit);
            AddContact(results.slideHit);

            for (int i = 0; i < contacts.Count; i++)
            {
                RaycastHit2D contact = contacts[i];
                if (!contact.collider)
                    continue;

                PhysicsSweep callbackPayload = new()
                {
                    hit = true,
                    distance = contact.distance,
                    input = stepDisplacement,
                    leftover = results.remainingVelocity * deltaTime,
                    normal = contact.normal,
                    anchorPoint = new AnchorPoint(contact.point, contact.normal, contact.collider),
                    angle = body.Direction.Angle(contact.normal),
                    collisions = new List<RaycastHit2D> { contact }
                };

                body.CollisionOverride(ref callbackPayload);
            }

            bool hasStandableSurface = IsStandable(body, results.surfaceHit)
                || IsStandable(body, results.slideHit);

            if (hasStandableSurface)
            {
                RaycastHit2D groundHit = IsStandable(body, results.surfaceHit)
                    ? results.surfaceHit
                    : results.slideHit;
                body.Anchor.Anchor(new AnchorPoint(
                    groundHit.point,
                    groundHit.normal,
                    groundHit.collider));
            }
            else if (body.Anchor.Anchored && !body.Anchor.Standable(body.Anchor.Normal))
            {
                body.Anchor.DeAnchor();
            }
            else if (body.Anchor.Anchored && body.Resolver == resolver
                && resolver is PhysicsResolver.GroundType
                && !results.surfaceHit.collider)
            {
                body.WalkOff();
            }

            return results;

            void AddContact(RaycastHit2D contact)
            {
                if (!contact.collider)
                    return;

                for (int i = 0; i < contacts.Count; i++)
                    if (contacts[i].collider == contact.collider
                        && (contacts[i].point - contact.point).sqrMagnitude < 0.0001f)
                        return;

                contacts.Add(contact);
            }
        }

        private static bool IsStandable(MovementBody body, RaycastHit2D hit) =>
            hit.collider && body.Anchor.Standable(hit.normal);

    }

    /// <summary>
    /// Grounded resolver that delegates collision response and surface sliding to
    /// Unity's Rigidbody2D.Slide implementation.
    /// </summary>
    [System.Serializable]
    public sealed class RigidbodySlideGroundResolver : PhysicsResolver.GroundType
    {
        [SerializeField, Range(0f, 180f)] private float surfaceSlideAngle = 90f;
        [SerializeField, Min(0f)] private float surfaceAnchorDistance = 0.1f;
        [SerializeField, Min(1)] private int maxIterations = 5;
        [SerializeField] private bool useLayerMask;
        [SerializeField] private LayerMask layerMask = -1;

        public override void Move(Vector2 stepVelocity, Vector2 normal)
        {
            RigidbodySlideResolverUtility.Move(
                this,
                Body,
                stepVelocity,
                surfaceSlideAngle,
                surfaceAnchorDistance,
                maxIterations,
                layerMask,
                useLayerMask);
        }
    }

    /// <summary>
    /// Airborne resolver that delegates collision response and sliding to Unity's
    /// Rigidbody2D.Slide implementation. Gravity remains controlled by the existing
    /// airborne resolver hooks or by the caller's velocity.
    /// </summary>
    [System.Serializable]
    public sealed class RigidbodySlideAirResolver : PhysicsResolver.AirType
    {
        [SerializeField, Range(0f, 180f)] private float surfaceSlideAngle = 90f;
        [SerializeField, Min(1)] private int maxIterations = 5;
        [SerializeField] private bool useLayerMask;
        [SerializeField] private LayerMask layerMask = -1;

        public override void Move(Vector2 stepVelocity, Vector2 normal)
        {
            RigidbodySlideResolverUtility.Move(
                this,
                Body,
                stepVelocity,
                surfaceSlideAngle,
                0f,
                maxIterations,
                layerMask,
                useLayerMask);
        }
    }
}
