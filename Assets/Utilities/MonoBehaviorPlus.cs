using System;
using UnityEngine;


public class MonoBehaviorPlus : MonoBehaviour
{

	public void Awake()
	{
		WhenGetComponents();
		WhenAwake();
	}

	public void Start() => WhenLateAwake();

	public void OnEnable()
	{
		EventSubscription(true);
		WhenEnable();
	}

	public void OnDisable()
	{
		EventSubscription(false);
		WhenDisable();
	}

	public void Update()
	{
		WhenUpdate();
		if (!Paused) WhenPausedUpdate();
	}

	public void LateUpdate()
	{
		WhenLateUpdate();
		if (!Paused) WhenPausedLateUpdate();

	}

	public void OnDestroy()
	{
		WhenDestroyOrUnload();
		if (gameObject.scene.isLoaded) WhenDestroy();
		else WhenUnload();

	}



	protected virtual void WhenAwake() { }
	protected virtual void WhenGetComponents() { }
	protected virtual void WhenLateAwake() { }
	protected virtual void WhenEnable() { }
	protected virtual void WhenDisable() { }
	protected virtual void WhenUpdate() { }
	protected virtual void WhenPausedUpdate() { }
	protected virtual void WhenLateUpdate() { }
	protected virtual void WhenPausedLateUpdate() { }
	protected virtual void WhenDestroy() { }
	protected virtual void WhenUnload() { }
	protected virtual void WhenDestroyOrUnload() { }

	//I actually have zero clue if this works. Research later.
	protected virtual Action[] GetEventListenerList() => null;
	protected virtual Action[] GetEventCallerList() => null;

	protected virtual void EventSubscription(bool subscribing)
	{
		for (int i = 0; i < GetEventListenerList().Length; i++)
		{
			if (subscribing) GetEventCallerList()[i] += GetEventListenerList()[i];
			else GetEventCallerList()[i] -= GetEventListenerList()[i];
		}
	}

	public static bool Paused
	{
		get => !(Time.timeScale > 0);
		set
		{
			if (value == Paused) return;
			if (value) _savedTimeScale = Time.timeScale;
			Time.timeScale = value ? 0 : _savedTimeScale;
		}
	}
	public static float _savedTimeScale = 1;

	public bool objectEnabled => gameObject.activeSelf;

	public void Enable() => enabled = true;
	public void Disable() => enabled = false;
	public void EnableObject() => gameObject.SetActive(true);
	public void DisableObject() => gameObject.SetActive(false);




}

