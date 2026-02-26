using System.Collections;
using UnityEngine;
using StateMachineSLS;

public class StateMachineTestScript : MonoBehaviour
{

    private void Start()
    {
    }


    public class MyStateMachine : StateMachine<MyStateMachine, StateMachineTestScript>
    {

		public enum States { FirstState, SecondState };
		protected override void InitializeStates()
		{
			RegisterState<FirstState>();
			RegisterState<SecondState>();
		}

		public class FirstState : StateBase
		{

		}
		public class SecondState : StateBase
		{

		}







	}
}
