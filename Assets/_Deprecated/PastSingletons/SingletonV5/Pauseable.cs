using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Pauseable : MonoBehaviour
{
    public bool paused { get; private set; }
	public bool manualPaused { get; private set; }
	private IPauseableBehavior[] behaviors;

    private void Awake()
    {
		behaviors = GetComponentsInChildren<IPauseableBehavior>();

		rb = new(this);
		nav = new(this);
		anim = new(this);
    }

    public void SetPause(bool value, bool manual = false)
    {
        if (paused == value) return;
        paused = value;
		if (manual && value) manualPaused = true;
		if(!value) manualPaused = false;

		rb.Handle(value);
		nav.Handle(value);
		anim.Handle(value);

		for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnPause(value);
    }
    public void Pause() => SetPause(true, true);
    public void UnPause() => SetPause(false, true);
    public void TogglePause() => SetPause(!paused);



    private void OnEnable() => Register();

    private void OnDisable() => UnRegister();
    private void OnDestroy() => UnRegister();


    [HideInInspector] public bool registered;
    public void Register()
    {
        if (registered) return;
        if (!GameplayPauseManager.Get()) return;

        GameplayPauseManager.RegisterPausable(this);
    }
    public void UnRegister()
    {
        if (!registered) return;
        if (GameplayPauseManager.Get()) return;

        GameplayPauseManager.UnRegisterPausable(this);
    }


	private class PausableComponent<T> where T : Component
	{
		public PausableComponent(Pauseable @this) => @this.TryGetComponent(out comp);

		public T comp;
		public virtual void Handle(bool value) { }
	}

	private P_Rigidbody rb;
    class P_Rigidbody : PausableComponent<Rigidbody>
    {
		public P_Rigidbody(Pauseable @this) : base(@this){}

		public override void Handle(bool value)
        {
			if (!comp) return;

			if (value)
			{
				s_rb_velocity = comp.linearVelocity;
				s_rb_angularVelocity = comp.angularVelocity;
				comp.linearVelocity = Vector3.zero;
				comp.angularVelocity = Vector3.zero;
				comp.Sleep();
			}
			else
			{
				comp.WakeUp();
				comp.linearVelocity = s_rb_velocity;
				comp.angularVelocity = s_rb_angularVelocity;
			}
		}

		private Vector3 s_rb_velocity;
		private Vector3 s_rb_angularVelocity;

	}

	private P_Animator anim;
    class P_Animator : PausableComponent<Animator>
    {
		public P_Animator(Pauseable @this) : base(@this) => comp = @this.GetComponentInChildren<Animator>();

		public override void Handle(bool value)
        {
			if (!comp) return;

			if (value)
			{
				s_anim_enabled = comp.enabled;
				comp.enabled = false;
			}
			else
			{
				comp.enabled = s_anim_enabled;
			}
		}

		private bool s_anim_enabled;
	}

	private P_NavMeshAgent nav;
	class P_NavMeshAgent : PausableComponent<NavMeshAgent>
	{
		public P_NavMeshAgent(Pauseable @this) : base(@this) { }

		public override void Handle(bool value)
        {
			if (!comp) return;

			if (value)
			{
				s_nav_destination = comp.destination;
				s_nav_velocity = comp.velocity;
				comp.isStopped = true;
			}

			comp.enabled = !value;

			if (!value)
			{
				comp.isStopped = false;
				comp.destination = s_nav_destination;
				comp.velocity = s_nav_velocity;
			}
		}

		private Vector3 s_nav_destination;
		private Vector3 s_nav_velocity;

	}

}

public interface IPauseableBehavior
{
    abstract void OnPause(bool value);

    public sealed void SetPause(bool value) => ((MonoBehaviour)this).GetComponent<Pauseable>().SetPause(value);
    public sealed void Pause() => ((MonoBehaviour)this).GetComponent<Pauseable>().Pause();
    public sealed void UnPause() => ((MonoBehaviour)this).GetComponent<Pauseable>().UnPause();

}