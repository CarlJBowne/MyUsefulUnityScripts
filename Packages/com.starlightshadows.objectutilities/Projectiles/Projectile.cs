using SLS.EditorUtilities.ComponentHeaders;
using UnityEngine;

namespace SLS.ObjectUtilities.Projectiles
{
    [RequireComponent(typeof(Spawnable))]
    public class Projectile : MonoBehaviour
    {
        [field: SerializeField, HeaderItem(true)] public Spawnable spawnable { get; private set; }
        [field: SerializeField, HeaderItem(true)] public Rigidbody rb { get; private set; }

        public Vector3 initVelocity { get; protected set; }
        public Vector3 initPosition { get; protected set; }
        public Placement target { get; protected set; }
        public virtual Vector3 velocity
        {
            get => rb.linearVelocity;
            set => rb.linearVelocity = value;
        }

        private void Reset()
        {
            HeaderItemAttribute.Reset(this);
        }

        public virtual Projectile Place(Placement placement)
        {
            transform.CopyFrom(placement);
            initPosition = transform.position;
            return this;
        }
        public virtual Projectile Send(Vector3 velocity)
        {
            initVelocity = velocity;
            this.velocity = initVelocity;
            return this;
        }
        public virtual Projectile SendAt(Placement target, float force = -1)
        {
            this.target = target;
            if(force > 0)
            {
                initVelocity = (target.Position - transform.position).normalized * force;
                this.velocity = initVelocity;
            }
            return this;
        }
    }
}
