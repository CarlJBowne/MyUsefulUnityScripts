using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace SLS.Physics3D
{
    [System.Serializable]
    public abstract class PhysicsSubComponent
    {
        #region Relations
        /// <summary>
        /// The owning PhysicsBody instance. This will be set by calling <see cref="Init"/>.
        /// </summary>
        public MovingBody Body { get; private set; }

        /// <summary>
        /// Whether this instance has been initialized and has an owner set.
        /// </summary>
        public bool HasOwner => Body != null;

        /// <summary>
        /// Initializes the Velocity instance with its owning <see cref="MovingBody"/>.
        /// </summary>
        /// <param name="owner">The physics body that owns this velocity container.</param>
        public virtual void Init(MovingBody owner) => Body = owner;

        /// <summary>
        /// Convenience accessor for the owner's transform.
        /// </summary>
        public Transform transform => Body.transform;
        public Rigidbody RB => Body.RB;

        public AnchorState Anchor => Body.Anchor;
        public Direction Direction => Body.Direction;
        public Velocity Velocity => Body.Velocity;
        public AnchorPoint AnchorPoint => Body.Anchor.AnchorPoint;
        public PhysicsBodyDebug Debug => Body.Debug;
        public NavMeshAgent NavAgent => Body.NavAgent;
        public Vector3 Position
        {
            get => Body.Position;
            set => Body.Position = value;
        }
        public Collider Collider => Body.Collider;
        #endregion

        protected void Print(Func<string> value)
        {
#if UNITY_EDITOR
            if (!Body.Debug.DisplayDebugString) return;
            Body.Debug.AppendLine(value?.Invoke());
#endif
        }
    }
}
