using UnityEngine;

namespace SLS.Physics3D
{
    /// <summary>
    /// A struct representing a contact point used for grounding and other physics interactions. Contains the contact point, normal, and collider information. Can be implicitly created from RaycastHit or ContactPoint data.
    /// </summary>
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
        public static implicit operator Vector3(AnchorPoint anchor) => anchor.normal;

        public static AnchorPoint Null => new()
        {
            point = Vector3.zero,
            normal = Vector3.up,
            collider = null
        };
    }
}