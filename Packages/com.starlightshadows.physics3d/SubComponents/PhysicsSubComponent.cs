using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SLS.Physics3D
{
    [System.Serializable]
    public abstract class PhysicsSubComponent
    {
        #region Relations
        /// <summary>
        /// The owning PhysicsBody instance. This will be set by calling <see cref="Init"/>.
        /// </summary>
        public PhysicsBody Body { get; private set; }

        /// <summary>
        /// Whether this instance has been initialized and has an owner set.
        /// </summary>
        public bool HasOwner => Body != null;

        /// <summary>
        /// Initializes the Velocity instance with its owning <see cref="PhysicsBody"/>.
        /// </summary>
        /// <param name="owner">The physics body that owns this velocity container.</param>
        public virtual void Init(PhysicsBody owner) => Body = owner;

        /// <summary>
        /// Convenience accessor for the owner's transform.
        /// </summary>
        public Transform transform => Body.transform;
        #endregion

    }
}
