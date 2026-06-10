using System;
using UnityEngine;

namespace RageRooster.Physics
{
    /// <summary>
    /// Abstract base class for movement resolvers. A resolver is responsible for translating a proposed movement vector into collisions, sliding, landing and other movement effects for its owning <see cref="PhysicsBody"/>.
    /// </summary>
    [System.Serializable]
    public abstract partial class PhysicsResolver : Polymorph
    {
        #region Relations

        /// <summary>
        /// Initialize this resolver with its owning <see cref="PhysicsBody"/>.
        /// This must be called by the owner during Awake/Start before using the resolver.
        /// </summary>
        /// <param name="body">The owning PhysicsBody.</param>
        public void Init(PhysicsBody body) => this.Body = body;

        /// <summary>
        /// The owning PhysicsBody instance. Available after <see cref="Init"/> is called.
        /// </summary>
        public PhysicsBody Body { get; private set; }

        /// <summary>
        /// Convenience properties that forward to the owning body. These provide quick
        /// access to common state frequently used by resolvers.
        /// </summary>
        protected Vector3 Position => Body.Position;
        protected Velocity stepZeroVelocity => Body.Velocity;
        protected GroundState Ground => Body.Ground;
        protected AnchorPoint anchor => Body.Ground.anchor;
        protected Direction direction => Body.Direction;
        protected PhysicsResolver Next => Body.Resolvers.Active;

        #endregion

        /// <summary>
        /// Lifecycle hooks and the main Move contract for resolvers.
        /// </summary>
        public virtual void Start()
        { }
        /// <summary>
        /// Called when this resolver becomes the active resolver for a PhysicsBody.
        /// </summary>
        public virtual void Enter() { }
        /// <summary>
        /// Called when this resolver is no longer active for a PhysicsBody.
        /// </summary>
        public virtual void Exit() { }
        /// <summary>
        /// Called before the main resolver Move invocation for per-frame setup.
        /// </summary>
        public virtual void FixedUpdateFormer() { }
        /// <summary>
        /// Called after the main resolver Move invocation for per-frame teardown.
        /// </summary>
        public virtual void FixedUpdateLatter() { }
        /// <summary>
        /// Process the supplied movement vector (<paramref name="stepVelocity"/>)
        /// for this resolver's domain. The implementation is responsible for
        /// performing collision sweeps, updating body position and optionally
        /// delegating remaining movement to the next resolver via the owning
        /// body's resolver selection.
        /// </summary>
        /// <param name="stepVelocity">The movement vector to process, typically velocity * deltaTime.</param>
        public abstract void Move(Vector3 stepVelocity);

        public bool ContinueCheck(Vector3 vel) => 
            vel.sqrMagnitude < float.Epsilon || ++Body.Step >= Body.maxPhysicsSteps;
        public bool ContinueCheck(float hitDistance) =>
            hitDistance == -1 || ++Body.Step >= Body.maxPhysicsSteps;

        public void ChooseNext() => Body.Resolvers.Update();
        public void ChooseNext(int target) => Body.Resolvers.Update(target);
        public void ChooseNext(PhysicsResolver target) => Body.Resolvers.Update(target);

        protected void Print(Func<string> value)
        {
            if (!Body.Debug.DisplayDebugString) return;
            Body.Debug.AppendLine(value?.Invoke());
        }
    }
}