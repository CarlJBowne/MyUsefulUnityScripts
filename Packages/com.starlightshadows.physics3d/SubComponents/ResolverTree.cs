using System;
using System.Collections.Generic;
using UnityEngine;

namespace SLS.Physics3D
{
    [System.Serializable]
    public class ResolverTree : PhysicsSubComponent
    {

        [field: SerializeField] public PhysicsResolver rootResolver { get; private set; }

        public PhysicsResolver Active { get; private set; }

        public override void Init(PhysicsBody owner)
        {
            base.Init(owner);
            PhysicsResolver[] resolvers = owner.GetComponents<PhysicsResolver>();
            for (int i = 0; i < resolvers.Length; i++) resolvers[i].OnStart();
        }

        public void Update() => Update(rootResolver);
        public void Update(PhysicsResolver resolver)
        {
            if (resolver == Active) return;
            Active?.Exit();
            Active = resolver;
            Active?.Enter();
        }
    }
}