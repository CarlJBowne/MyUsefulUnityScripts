using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SLS.Physics2D.Helpers
{
    public static class ThrowAt
    {
        public static void WithTimeAndMinVelocity(Vector2 destination, float t, float g, float minVelocity, out float initialVelocity, out float angle)
        {
            // Compute required velocity
            float v_x = destination.x / t;
            float v_y = (destination.y + .5f * g * t * t) / t;
            initialVelocity = Mathf.Sqrt(v_x * v_x + v_y * v_y);
            angle = Mathf.Atan2(v_y, v_x) * (180 / Mathf.PI);

            // Adjust angle if velocity is too low
            if (initialVelocity < minVelocity)
            {
                initialVelocity = minVelocity;
                float v_y_adjusted = Mathf.Sqrt(minVelocity * minVelocity - v_x * v_x);
                angle = Mathf.Atan2(v_y_adjusted, v_x) * (180 / Mathf.PI);
            }
        }
    }
    /* Alternate Reach Check Methods
     
     
        bool PlatformCheckSteps(float checkDistance, out float stopDistance, out Vector2 stopNormal)
        {
            // Choose a reasonable sampling step: based on Collider2D radius for stable edge detection.
            float sampleStep = Mathf.Max(Collider2D != null ? Collider2D.radius * 0.5f : 0.1f, 0.1f);
            // Ensure at least one sample at the destination is checked.
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(distance / sampleStep));
            float step = distance / (float)sampleCount;

            for (int i = 1; i <= sampleCount; i++)
            {
                float traveled = step * i;
                Vector2 samplePos = Position + (direction * traveled);
                if (!SweepBody(Vector2.down * checkDistance, out RaycastHit2D gHit, 0, samplePos))
                {
                    // No ground under this sample -> stop at last safe sample.
                    float safeTraveled = Mathf.Max(0f, step * (i - 1));
                    stopDistance = safeTraveled;
                    stopNormal = Vector2.down;
                    return false;
                }
            }
            stopDistance = distance;
            stopNormal = -direction;
            return true;
        }
        bool PlatformCheckEarlyCancel(float checkDistance, out float stopDistance, out Vector2 stopNormal)
        {
            if (!SweepBody(Vector2.down * checkDistance, out RaycastHit2D gHit, 0, Position + velocity))
            {
                stopDistance = 0f;
                stopNormal = Vector2.down;
                return false;
            }
            stopDistance = checkDistance;
            stopNormal = Vector2.up;
            return true;
        }
        bool PlatformCheckReachAround(float initCheckDistance, out float stopDistance, out Vector2 stopNormal)
        {
            if (!SweepBody(Vector2.down * initCheckDistance, out _, 0, Position + velocity))
            {
                if (SweepBody(-velocity, out RaycastHit2D hitResult, 0, Position + velocity - (Vector2.up * Collider2D.height / 2)))
                {
                    stopDistance = velocity.magnitude - hitResult.distance - .1f;
                    stopNormal = (-hitResult.normal).XZ();
                    return false;
                }
            }
            stopDistance = distance;
            stopNormal = Vector2.up;
            return true;
        }
        bool PlatformCheckTriangle(out float stopDistance, out Vector2 stopNormal)
        {
            //NOT IMPLEMENTED YET
            stopDistance = distance;
            stopNormal = Vector2.up;
            return true;
        }
        bool PlatformCheckNavMesh(out float stopDistance, out Vector2 stopNormal)
        {
            //NOT IMPLEMENTED YET
            stopDistance = distance;
            stopNormal = Vector2.up;
            return true;
        }
     
     
     */
}