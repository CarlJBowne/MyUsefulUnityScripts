using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PhysicsPro
{
    /*
    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest.
    /// </summary>
    /// <param name="direction">The direction the Rigidbody is going.</param>
    /// <param name="distance">The distance the Rigidbody is set to travel.</param>
    /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
    /// <param name="hit">The resulting Hit.</param>
    /// <returns>Whether anything was Hit.</returns>
    public static bool DirectionCast(this Rigidbody RB, Vector3 direction, float distance, out RaycastHit hit)
    {
        bool result = RB.SweepTest(direction.normalized, out hit, distance, QueryTriggerInteraction.Ignore);
        return result;
    }
    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest. (Returns Multiple.)
    /// </summary>
    /// <param name="direction">The direction the Rigidbody is going.</param>
    /// <param name="distance">The distance the Rigidbody is set to travel.</param>
    /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
    /// <param name="hit">The resulting Hits.</param>
    /// <returns>Whether anything was Hit.</returns>
    public static bool DirectionCast(this Rigidbody RB, Vector3 direction, float distance, out RaycastHit[] hit)
    {
        hit = RB.SweepTestAll(direction.normalized, distance, QueryTriggerInteraction.Ignore);
        return hit.Length > 0;
    }
    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest. (Includes optional buffer)
    /// </summary>
    /// <param name="direction">The direction the Rigidbody is going.</param>
    /// <param name="distance">The distance the Rigidbody is set to travel.</param>
    /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
    /// <param name="hit">The resulting Hit.</param>
    /// <returns>Whether anything was Hit.</returns>
    public static bool DirectionCast(this Rigidbody RB, Vector3 direction, float distance, float buffer, out RaycastHit hit)
    {
        if (buffer > 0) RB.MovePosition(RB.position - direction * buffer);
        bool result = RB.SweepTest(direction.normalized, out hit, distance + buffer, QueryTriggerInteraction.Ignore);
        if (buffer > 0) RB.MovePosition(RB.position + direction * buffer);
        hit.distance -= buffer;
        return result;
    }
    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest. (Includes optional buffer) (Returns Multiple.)
    /// </summary>
    /// <param name="direction">The direction the Rigidbody is going.</param>
    /// <param name="distance">The distance the Rigidbody is set to travel.</param>
    /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
    /// <param name="hit">The resulting Hits.</param>
    /// <returns>Whether anything was Hit.</returns>
    public static bool DirectionCast(this Rigidbody RB, Vector3 direction, float distance, float buffer, out RaycastHit[] hit)
    {
        if (buffer > 0) RB.MovePosition(RB.position - direction * buffer);
        hit = RB.SweepTestAll(direction.normalized, distance + buffer, QueryTriggerInteraction.Ignore);
        if (buffer > 0) RB.MovePosition(RB.position + direction * buffer);
        hit[0].distance -= buffer;
        return hit.Length > 0;
    }
    */

    /* Reference
    static int maxBounces = 5;
    static float skinWidth = 0.015f;
    static float maxSlopeAngle = 55;

    public static Vector3 CollideAndSlide(this Rigidbody rb, Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit)
    {
        if (depth >= maxBounces) return Vector3.zero;

        if (rb.DirectionCast(vel.normalized, vel.magnitude, 0, out RaycastHit hit))
        {
            Vector3 snapToSurface = vel.normalized * (hit.distance);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            //if (snapToSurface.magnitude <= checkBuffer) snapToSurface = Vector3.zero;

            // normal ground / slope
            if (angle <= maxSlopeAngle)
            {
                if (gravityPass) return snapToSurface;
                leftover = leftover.ProjectAndScale(hit.normal);
            }
            else // wall or steep slope
            {
                float scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(velInit.x, 0, velInit.z).normalized
                    );

                leftover = true && !gravityPass
                    ? velInit.XZ().ProjectAndScale(hit.normal.XZ().normalized).normalized * scale
                    : leftover.ProjectAndScale(hit.normal) * scale;
            }
            return snapToSurface + rb.CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass, velInit);
        }

        return vel;
    }

    public static Vector3 ProjectAndScale(Vector3 vec, Vector3 normal)
    {
        float mag = vec.magnitude;
        vec = Vector3.ProjectOnPlane(vec, normal).normalized;
        vec *= mag;
        return vec;
    }
    */

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
     
     
        bool PlatformCheckSteps(float checkDistance, out float stopDistance, out Vector3 stopNormal)
        {
            // Choose a reasonable sampling step: based on collider radius for stable edge detection.
            float sampleStep = Mathf.Max(Collider != null ? Collider.radius * 0.5f : 0.1f, 0.1f);
            // Ensure at least one sample at the destination is checked.
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(distance / sampleStep));
            float step = distance / (float)sampleCount;

            for (int i = 1; i <= sampleCount; i++)
            {
                float traveled = step * i;
                Vector3 samplePos = Position + (direction * traveled);
                if (!SweepBody(Vector3.down * checkDistance, out RaycastHit gHit, 0, samplePos))
                {
                    // No ground under this sample -> stop at last safe sample.
                    float safeTraveled = Mathf.Max(0f, step * (i - 1));
                    stopDistance = safeTraveled;
                    stopNormal = Vector3.down;
                    return false;
                }
            }
            stopDistance = distance;
            stopNormal = -direction;
            return true;
        }
        bool PlatformCheckEarlyCancel(float checkDistance, out float stopDistance, out Vector3 stopNormal)
        {
            if (!SweepBody(Vector3.down * checkDistance, out RaycastHit gHit, 0, Position + velocity))
            {
                stopDistance = 0f;
                stopNormal = Vector3.down;
                return false;
            }
            stopDistance = checkDistance;
            stopNormal = Vector3.up;
            return true;
        }
        bool PlatformCheckReachAround(float initCheckDistance, out float stopDistance, out Vector3 stopNormal)
        {
            if (!SweepBody(Vector3.down * initCheckDistance, out _, 0, Position + velocity))
            {
                if (SweepBody(-velocity, out RaycastHit hitResult, 0, Position + velocity - (Vector3.up * Collider.height / 2)))
                {
                    stopDistance = velocity.magnitude - hitResult.distance - .1f;
                    stopNormal = (-hitResult.normal).XZ();
                    return false;
                }
            }
            stopDistance = distance;
            stopNormal = Vector3.up;
            return true;
        }
        bool PlatformCheckTriangle(out float stopDistance, out Vector3 stopNormal)
        {
            //NOT IMPLEMENTED YET
            stopDistance = distance;
            stopNormal = Vector3.up;
            return true;
        }
        bool PlatformCheckNavMesh(out float stopDistance, out Vector3 stopNormal)
        {
            //NOT IMPLEMENTED YET
            stopDistance = distance;
            stopNormal = Vector3.up;
            return true;
        }
     
     
     */


    /* Old Move Method
     
         /// <summary>
    /// The Collide and Slide Algorithm.
    /// </summary>
    /// <param name="vel">Input Velocity.</param>
    /// <param name="prevNormal">The Normal of the previous Step.</param>
    /// <param name="step">The current step. Starts at 0.</param>
    void MoveOld(Vector3 vel, AnchorPoint anchor = default, int step = 0, bool testString = false)
    {
        if (testString) moveTestString += $"Step {step}: {vel}\n";

        if (step == 0 && vel.y <= 0)
        {
            bool tryGround = GroundCheck(out var groundRes);
            if (Grounded && !tryGround) UnLand();
            else if (!Grounded && tryGround) Land(groundRes);
        }

        if (SweepBody(vel, out RaycastHit hit, groundCheckBuffer))
        {
            if (testString) moveTestString += $"Hit: {hit.normal} at distance {hit.distance}\n";
            Vector3 snapToSurface = vel.normalized * hit.distance;
            Vector3 leftover = vel - snapToSurface;
            Vector3 nextNormal = hit.normal;
            bool scaleByDot = false;

            if (step == movementProjectionSteps) return;

            if (!MoveForward(snapToSurface)) return;

            else if (Grounded)
            {
                if (testString) moveTestString += "Is Grounded.\n";

                if (Mathf.Approximately(hit.normal.y, 0))
                {
                    if (testString) moveTestString += "Hit a wall.\n";
                    scaleByDot = true;
                    leftover.y = 0;
                    if (StopForward(ref nextNormal, hit.normal)) return;
                }
                else if (hit.normal.y > 0 && !WithinSlopeAngle(hit.normal))
                {
                    if (testString) moveTestString += "Hit a steep slope.\n";
                    scaleByDot = true;
                    leftover.y = 0;
                    if (StopForward(ref nextNormal, hit.normal)) return;
                }


                if (Grounded && anchor.normal.y > 0 && hit.normal.y < 0) //Floor to Cieling
                {
                    if (FloorCeilingLock(anchor.normal, hit.normal)) return;
                }
                else if (Grounded && anchor.normal.y < 0 && hit.normal.y > 0) //Ceiling to Floor
                {
                    if (FloorCeilingLock(hit.normal, anchor.normal)) return;
                }

                bool FloorCeilingLock(Vector3 floorNormal, Vector3 ceilingNormal)
                {
                    if (testString) moveTestString += "Encountered Vertical Squish.\n";
                    scaleByDot = true;
                    return StopForward(ref nextNormal, floorNormal.y != floorNormal.magnitude ? floorNormal : ceilingNormal);
                }

            }
            else
            {
                if (testString) moveTestString += "Isnt Grounded.\n";


                if (Mathf.Approximately(hit.normal.y, 0))
                {
                    if (testString) moveTestString += "Hit a Wall.\n";
                    if (StopForward(ref nextNormal, hit.normal)) return;
                }
                else if (hit.normal.y > 0)
                {
                    if (WithinSlopeAngle(hit.normal))
                    {
                        if (testString) moveTestString += "Landed on a standable ground.\n";
                        Land(hit);
                        leftover.y = 0;
                    }
                    else
                    {
                        if (testString) moveTestString += "Hit a steep slope while falling.\n";
                    }
                }
                else
                {
                    if (testString) moveTestString += "Hit a sloped ceiling while jumping.\n";
                }
            }


            Vector3 newDir = leftover.ProjectAndScale(nextNormal);
            if (scaleByDot) newDir *= Vector3.Dot(leftover.normalized, nextNormal) + 1;
            MoveOld(newDir, anchor, step + 1);
        }
        else
        {
            if (testString) moveTestString += "No Hit\n";

            if (step == movementProjectionSteps) return;
            if (!MoveForward(vel)) return;

            //Snap to ground when walking on a downward slope.
            if (Grounded && stepZeroVelocity.y <= 0)
            {
                if (SweepBody(Vector3.down * 0.5f, out RaycastHit groundHit, groundCheckBuffer))
                {
                    // Make sure the hit is under the character's feet (not beside it).
                    // Compute the bottom-center point of the capsule in world space.
                    Vector3 bottomCenter = Position + Collider.center - Vector3.up * (Collider.height * 0.5f - Collider.radius);
                    Vector3 horizontalDelta = new(groundHit.point.x - bottomCenter.x, 0f, groundHit.point.z - bottomCenter.z);

                    // Allow a small tolerance because of floating precision and scale.
                    float allowedRadius = Collider.radius + 0.05f;

                    if (horizontalDelta.sqrMagnitude <= allowedRadius * allowedRadius)
                    {
                        // Ground is under the feet -> snap down.
                        Position += Vector3.down * groundHit.distance;
                    }
                    else
                    {
                        // Hit was off to the side (ledge), so walk off instead of snapping.
                        WalkOff();
                    }
                }
                else
                {
                    WalkOff();
                }
            }
        }
    }

         bool StopForward(ref Vector3 nextNormal, Vector3 newNormal)
    {
        nextNormal = newNormal.XZ().normalized;
        return Machine.SendSignal(new("Bonk", 0, true));
    }
    bool MoveForward(Vector3 offset)
    {
        if (true)
        {
            Position += offset;
            return false;
        }
        else
        {
            velocity = Vector3.zero;
            return true;
        }
    }

     
     
     */
}


public struct AnchorPoint
{
    public Vector3 point;
    public Vector3 normal;
    public Collider collider;

    public AnchorPoint(Vector3 point, Vector3 normal, Collider collider)
    {
        this.point = point;
        this.normal = normal;
        this.collider = collider;
    }
    public AnchorPoint(RaycastHit hit)
    {
        point = hit.point;
        normal = hit.normal;
        collider = hit.collider;
    }
    public AnchorPoint(ContactPoint contact)
    {
        point = contact.point;
        normal = contact.normal;
        collider = contact.otherCollider;
    }

    public static implicit operator AnchorPoint(RaycastHit hit) => new(hit);
    public static implicit operator AnchorPoint(ContactPoint contact) => new(contact);
    public static implicit operator bool(AnchorPoint anchor) => anchor.point != Vector3.zero || anchor.normal != Vector3.zero || anchor.collider != null;

    public static AnchorPoint Null => new()
    {
        point = Vector3.zero,
        normal = Vector3.up,
        collider = null
    };
}
/*
public struct BodyAnchor
{
    public Vector3 point;
    public Vector3 normal;
    public Transform transform;
    public IMovablePlatform Movable
    {
        readonly get => _movable;
        set
        {
            if (value == _movable) return;
            _movable?.RemoveBody(body);
            _movable = value;
            _movable?.AddBody(body);
        }
    }
    private IMovablePlatform _movable;
    public readonly CharacterMovementBody body;

    public BodyAnchor(Vector3 point, Vector3 normal, Transform transform, CharacterMovementBody body = null)
    {
        this.point = point;
        this.normal = normal;
        this.transform = transform;

        this.body = body;
        _movable = null;
        if (transform != null && body != null)
            Movable = transform.GetComponent<IMovablePlatform>();
    }
    

    public void Update(Vector3 point, Vector3 normal, Transform transform)
    {
        this.point = point;
        this.normal = normal;
        this.transform = transform;

        if (body != null)
            Movable = transform != null
                ? transform.GetComponent<IMovablePlatform>()
                : null;
    }
    public void Update(AnchorPoint other)
    {
        point = other.point;
        normal = other.normal;
        transform = other.transform;

        if (body != null)
            Movable = transform != null
                ? transform.GetComponent<IMovablePlatform>()
                : null;
    }
    public void Update(RaycastHit hit)
    {
        point = hit.point;
        normal = hit.normal;
        transform = hit.transform != null ? hit.transform : null;

        if (body != null)
            Movable = transform != null
                ? transform.GetComponent<IMovablePlatform>()
                : null;
    }
    public void Update(ContactPoint contact)
    {
        point = contact.point;
        normal = contact.normal;
        transform = contact.otherCollider != null ? contact.otherCollider.transform : null;

        if (body != null)
            Movable = transform != null
                ? transform.GetComponent<IMovablePlatform>()
                : null;
    }
    public void Update(object NULL)
    {
        point = body.Position;
        normal = body.Get3DGravity();
        transform = null;
        Movable = null;
    }

}
*/