using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SLS.Physics2D;
using UnityEngine;

namespace SLS.Physics2D
{
    [RequireComponent(typeof(MovementBody))]
    public class PushableBody : MonoBehaviour, IPushable
    {
        public MovementBody Body { get; private set; }
        [field: SerializeField] public int PushPriority { get; set; } = 0;
        [field: SerializeField] public float PushResistance { get; set; } = .5f;

        public IPushable active { get; private set; }
        private bool blockedThisFrame = false;

        private void Reset() => Body = GetComponent<MovementBody>();
        private void Awake()
        {
            Body.collisionOverrides.Add(this);
            active = this;
        }
        public bool RecievePush(IPushable source, Vector2 direction, float distance, out float acceptedDistance)
        {
            if (blockedThisFrame || !Body)
            {
                acceptedDistance = 0f;
                return false;
            }

            float targetShare = 0.5f;

            if (source.PushPriority > PushPriority || PushPriority == 0f) targetShare = 1f;
            else if (source.PushPriority < PushPriority || source.PushPriority == 0f) targetShare = 0f;
            else
            {
                float sourceWeight = 1f / source.PushResistance;
                float targetWeight = 1f / PushResistance;
                targetShare = sourceWeight / (sourceWeight + targetWeight);
            }

            acceptedDistance = Mathf.Max(0f, distance * Mathf.Clamp01(targetShare));

            if (acceptedDistance < 0.001f) return false;

            Vector2 push = direction.normalized * acceptedDistance;

            // Do not push the target through an ordinary Collider2D.
            if (Body.Sweep(push, out PhysicsSweep chainHit) && chainHit.distance >= 0f)
            {
                acceptedDistance = Mathf.Min(acceptedDistance, chainHit.distance);
                push = direction.normalized * acceptedDistance;
            }

            if (acceptedDistance < 0.001f) return false;

            blockedThisFrame = true;
            Body.Position += push;
            StartCoroutine(ResetPushBlock());
            IEnumerator ResetPushBlock()
            {
                yield return new WaitForFixedUpdate();
                blockedThisFrame = false;
            }

            return true;
        }

        public void CollisionOverride(ref PhysicsSweep collision)
        {
            if (!Body || blockedThisFrame || !collision.anchorPoint.Collider2D) return;

            Vector2 direction = collision.input.sqrMagnitude > 0.001f
                    ? collision.input.normalized
                    : -collision.normal.normalized;

            float distance = Mathf.Max(collision.distance, collision.input.magnitude);

            if (collision.anchorPoint.Collider2D.TryGetComponent(out IPushable target))
            {
                target = target.active;

                if (target.RecievePush(this, direction, distance, out float acceptedDistance))
                {
                    collision.hit = false;
                    collision.distance = acceptedDistance;
                }

                return;
            }

            if (!collision.anchorPoint.Collider2D.TryGetComponent(out Rigidbody2D rb)) return;

            Vector2 relativeVelocity = Body.Velocity.Global - rb.linearVelocity;

            float impact = Mathf.Max(0f, Vector2.Dot(relativeVelocity, direction));

            if (rb.bodyType is not RigidbodyType2D.Kinematic) 
                rb.AddForceAtPosition(direction * impact * PushResistance, collision.anchorPoint.point, ForceMode2D.Impulse);
            else
            {
                
                rb.MovePosition(rb.position + direction * distance);
                collision.hit = false;
            }
        }
        public void Override(IPushable Override, bool remove = false)
        {
            IPushable oldTarget = remove ? Override : this;
            IPushable newTarget = remove ? this : Override;
            active = newTarget;

            int id = Body.collisionOverrides.IndexOf(oldTarget);
            Body.collisionOverrides[id] = newTarget;
        }
    }
}
