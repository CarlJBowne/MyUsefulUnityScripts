using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//Edit this part to add new tags.
public enum Tags
{
	Player,
	Terrain,
	Enemy,
	Projectile,
	Item
}




//The Functionality, do not touch.
public class TagSet : MonoBehaviour
{
	[SerializeField, InspectorName("Tags")] private Tags[] _tags;
	public HashSet<Tags> tags;
	void Awake() => tags = _tags.ToHashSet();
	public void AddTag(Tags tag) => tags.Add(tag);
	public void RemoveTag(Tags tag) => tags.Remove(tag);
	public bool HasTag(Tags tag) => tags.Contains(tag);
	public bool HasTags(params Tags[] tags) => tags.All(s => this.tags.Contains(s));
}
public static class _Ext_TagChecker
{
	public static bool HasTag(this GameObject m, Tags tag)
	{
		TagSet T = m.GetComponent<TagSet>();
		return T != null && T.tags.Contains(tag);
	}
	public static bool HasTags(this GameObject m, params Tags[] tags)
	{
		TagSet T = m.GetComponent<TagSet>();
		return T != null && tags.All(s => T.tags.Contains(s));
	}
	public static bool HasTag(this Component c, Tags tag)
	{
		TagSet T = c.GetComponent<TagSet>();
		return T != null && T.tags.Contains(tag);
	}
	public static bool HasTags(this Component c, params Tags[] tags)
	{
		TagSet T = c.GetComponent<TagSet>();
		return T != null && tags.All(s => T.tags.Contains(s));
	}

}