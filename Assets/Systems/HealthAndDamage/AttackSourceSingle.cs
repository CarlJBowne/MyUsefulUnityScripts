using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Timer;
using UnityEditor;
using UnityEngine;

public class AttackSourceSingle : AttackSourceBase
{
    public Attack attack;
    public MonoBehaviour sourceEntity;
    public new bool enabled = true;

    public override Attack GetAttack()
    {
        Attack result = attack;
        result.velocity = transform.TransformDirection(result.velocity);
        return result;
    }

    public override void Contact(GameObject target)
    {
        if (enabled && target.TryGetComponent(out Health health)) health.Damage(GetAttack());
    }

}


public abstract class AttackSourceBase : MonoBehaviour
{
    public abstract Attack GetAttack();

    void OnTriggerEnter(Collider other) => Contact(other.gameObject);
    void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);

    public abstract void Contact(GameObject target);
}




[System.Serializable]
public struct Attack
{
    public int amount;

    public Vector3 velocity;
    public Tag[] tags;

    public Attack(int damage, params Tag[] tags)
    {
        this.amount = damage;
        velocity = Vector3.zero;
        this.tags = tags;
    }
    public Attack(int damage, Vector3 velocity, params Tag[] tags)
    {
        this.amount = damage;
        this.velocity = velocity;
        this.tags = tags;
    }



    public static Attack operator +(Attack a, Tag[] tags) => new(a.amount, a.velocity, a.tags.Concat(tags).ToArray());

    [System.Serializable]
    public struct Tag
    {
        public string name;

        public Tag(string name) => this.name = name;

        public override bool Equals(object obj) => obj is Tag tag && name == tag.name;
        public override int GetHashCode() => HashCode.Combine(name);

        public static bool operator ==(Tag a, Tag b) => a.name == b.name;
        public static bool operator !=(Tag a, Tag b) => a.name != b.name;

        public static implicit operator string(Tag a) => a.name;
        public static implicit operator Tag(string a) => new(a);

        public static Tag FromPlayer => new("FromPlayer");
        public static Tag FromEnemy => new("FromEnemy");
        public static Tag Wham => new("Wham");
        public static Tag FriendlyFire => new("FriendlyFire");
        public static Tag Punch => new("Punch");
        public static Tag Egg => new("Egg");
        public static Tag Thrown => new("Thrown");
        public static Tag ThrownEnemy => new("ThrownEnemy");
        public static Tag Pit => new("Pit");

    }

    public bool HasTag(Tag tag) => tags.Includes(tag);
}

public static class _AttackTagOverrides
{
    public static bool Includes(this Attack.Tag[] List, Attack.Tag Tag) => List.Contains(Tag);
    public static bool IncludedBy(this Attack.Tag Tag, Attack.Tag[] List) => List.Contains(Tag);
    public static bool IncludesAny(this Attack.Tag[] destList, Attack.Tag[] checkList) => destList.Intersect(checkList).Count() > 0;
    public static bool IncludedAny(this Attack.Tag[] checkList, Attack.Tag[] destList) => destList.Intersect(checkList).Count() > 0;

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Attack.Tag))]
public class AttackTagPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Retrieve the serialized fields
        SerializedProperty nameProperty = property.FindPropertyRelative("name");
        EditorGUI.PropertyField(position, nameProperty, GUIContent.none);

        EditorGUI.EndProperty();
    }
}
#endif