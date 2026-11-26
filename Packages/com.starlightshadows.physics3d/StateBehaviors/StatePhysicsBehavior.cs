using UnityEngine;
using SLS.StateMachineUltimate;

namespace SLS.Physics3D.States
{
    public abstract class StatePhysicsBehavior : StateBehavior<StateMachinePhysicsCaller>
    {
        public StateMachinePhysicsCaller PhysCaller => CoreComp;

        protected override void OnEnter(State prev, bool isFinal)
        {
            PhysCaller.CollisionEnter += CollisionEnter;
            PhysCaller.CollisionExit += CollisionExit;
            PhysCaller.TriggerEnter += TriggerEnter;
            PhysCaller.TriggerExit += TriggerExit;
        }
        protected override void OnExit(State next)
        {
            PhysCaller.CollisionEnter -= CollisionEnter;
            PhysCaller.CollisionExit -= CollisionExit;
            PhysCaller.TriggerEnter -= TriggerEnter;
            PhysCaller.TriggerExit -= TriggerExit;
        }

        protected virtual void CollisionEnter(Collision collision) { }
        protected virtual void CollisionExit(Collision collision) { }
        protected virtual void TriggerEnter(Collider other) { }
        protected virtual void TriggerExit(Collider other) { }
    }
    public interface IStatePhysicsBehavior
    {

        public StateMachinePhysicsCaller PhysCaller { get; }

        protected void OnEnter(State prev, bool isFinal);
        protected void OnExit(State next);

        protected static void Enter(IStatePhysicsBehavior This, StateMachinePhysicsCaller PhysCaller)
        {
            PhysCaller.CollisionEnter += This.CollisionEnter;
            PhysCaller.CollisionExit += This.CollisionExit;
            PhysCaller.TriggerEnter += This.TriggerEnter;
            PhysCaller.TriggerExit += This.TriggerExit;
        }
        protected static void Exit(IStatePhysicsBehavior This, StateMachinePhysicsCaller PhysCaller)
        {
            PhysCaller.CollisionEnter -= This.CollisionEnter;
            PhysCaller.CollisionExit -= This.CollisionExit;
            PhysCaller.TriggerEnter -= This.TriggerEnter;
            PhysCaller.TriggerExit -= This.TriggerExit;
        }

        protected virtual void CollisionEnter(Collision collision) { }
        protected virtual void CollisionExit(Collision collision) { }
        protected virtual void TriggerEnter(Collider other) { }
        protected virtual void TriggerExit(Collider other) { }
    }
}
