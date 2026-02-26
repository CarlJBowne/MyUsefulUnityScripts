using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A Component managing the Health of an entity, whether that be Player, Enemy, or Destructible Object.
/// </summary>

public class Health : MonoBehaviour
{
	//Config
	[SerializeField] protected int maxHealth;
	[SerializeField] protected UnityEvent<int> damageEvent = new();
	[SerializeField] protected UnityEvent depleteEvent = new();
	[SerializeField] protected Attack.Tag[] immuneTags;

	//Data
	[SerializeField, HideInInspector] protected int health;
	protected bool damagable = true;

	//Getters
	public int GetCurrentHealth() => health;
	public int GetMaxHealth() => maxHealth;
	public float GetHealthPercentage() => health / maxHealth;

	protected virtual void Awake() => health = maxHealth;


    public bool Damage(Attack attack)
    {
        if (!damagable || attack.amount < 1 || immuneTags.IncludesAny(attack.tags) || OverrideDamage(attack)) return false;

        health -= attack.amount;

		if(health < 0) health = 0;

        OnDamage(attack);
        if (health == 0) OnDeplete(attack);

        return true;
    }

    protected virtual void OnDamage(Attack attack) => damageEvent?.Invoke(attack.amount);
    protected virtual void OnDeplete(Attack attack) => depleteEvent?.Invoke();

	protected virtual bool OverrideDamage(Attack attack) { return false; }
}
