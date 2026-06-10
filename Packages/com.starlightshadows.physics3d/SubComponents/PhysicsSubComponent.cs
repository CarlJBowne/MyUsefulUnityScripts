using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SLS.Physics
{
    [System.Serializable]
    public abstract class PhysicsSubComponent
    {
        #region Relations
        /// <summary>
        /// The owning PhysicsBody instance. This will be set by calling <see cref="Init"/>.
        /// </summary>
        public PhysicsBody body { get; private set; }

        /// <summary>
        /// Whether this instance has been initialized and has an owner set.
        /// </summary>
        public bool HasOwner => body != null;

        /// <summary>
        /// Initializes the Velocity instance with its owning <see cref="PhysicsBody"/>.
        /// </summary>
        /// <param name="owner">The physics body that owns this velocity container.</param>
        public void Init(PhysicsBody owner) => body = owner;

        /// <summary>
        /// Convenience accessor for the owner's transform.
        /// </summary>
        public Transform transform => body.transform;
        #endregion

    }
}
