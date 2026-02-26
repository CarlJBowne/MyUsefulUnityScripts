#if false


using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class LoudHoldInteraction : IInputInteraction
{
	public float minDuration = 0.05f;
	private bool active;

	public void Process(ref InputInteractionContext context)
	{
		//if (!context.isStarted) return;
		if (context.ControlIsActuated()) 
		{
			if (!active && context.time - context.startTime > minDuration)
			{
				active = true;
				context.Started();
			}
			if (active) context.Performed();
		}
		else
		{
			if (active)
			{
				context.Canceled();
				active = false;
			}
		}

	}


	void IInputInteraction.Reset() { }

	static LoudHoldInteraction()
	{
		InputSystem.RegisterInteraction<LoudHoldInteraction>();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Initialize() {}
}
#endif