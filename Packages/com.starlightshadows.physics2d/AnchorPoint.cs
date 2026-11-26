using UnityEngine;

namespace SLS.Physics2D
{
    /// <summary>
    /// A struct representing a contact point used for grounding and other physics interactions. Contains the contact point, normal, and Collider2D information. Can be implicitly created from RaycastHit2D or ContactPoint2D data.
    /// </summary>
    public struct AnchorPoint
    {
        public Vector2 point;
        public Vector2 normal;
        public Collider2D Collider;

        public AnchorPoint(Vector2 point, Vector2 normal, Collider2D Collider2D)
        {
            this.point = point;
            this.normal = normal;
            this.Collider = Collider2D;
        }
        public AnchorPoint(RaycastHit2D hit)
        {
            point = hit.point;
            normal = hit.normal;
            Collider = hit.collider;
        }
        public AnchorPoint(ContactPoint2D contact)
        {
            point = contact.point;
            normal = contact.normal;
            Collider = contact.otherCollider;
        }

        public static implicit operator AnchorPoint(RaycastHit2D hit) => new(hit);
        public static implicit operator AnchorPoint(ContactPoint2D contact) => new(contact);
        public static implicit operator bool(AnchorPoint anchor) => anchor.normal != Vector2.zero && anchor.Collider != null;
        public static implicit operator Vector2(AnchorPoint anchor) => anchor.normal;

        public static AnchorPoint Null => new()
        {
            point = Vector2.zero,
            normal = Vector2.up,
            Collider = null
        };

        public enum Angle
        {
            Floor,
            Slope,
            SteepSlope,
            Wall,
            InvertedSlope,
            Ceiling,
            Null,
        }
    }
}