using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLS.Physics3D
{
    public interface IMovablePlatform
    {
        public List<PhysicsBody> bodies { get; }

        public void AddBody(PhysicsBody body) => bodies.Add(body);
        public void RemoveBody(PhysicsBody body) => bodies.Remove(body);

        protected static void DoMove(IMovablePlatform This, Vector3 offset)
        {
            for (int i = 0; i < This.bodies.Count; i++)
                This.bodies[i].Position += offset;
        }
    }
}