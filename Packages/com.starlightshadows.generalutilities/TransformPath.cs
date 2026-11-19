using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
public class TransformPath
{
    int goUpXTimes = 0;
    List<string> findBelow;

    public TransformPath(int goUpXTimes, List<string> findBelow)
    {
        this.goUpXTimes = goUpXTimes;
        this.findBelow = findBelow;
    }
    public TransformPath(string firstFindBelow, params string[] findBelow)
    {
        this.findBelow = findBelow.ToList();
        this.findBelow.Insert(0, firstFindBelow);
    }
    public TransformPath(int goUpXTimes, params string[] findBelow)
    {
        this.goUpXTimes = goUpXTimes;
        this.findBelow = findBelow.ToList();
    }
    public TransformPath(string fullString, bool includeForwardSlash = false)
    {
        List<string> strings = (!includeForwardSlash ? fullString.Split('\\') : fullString.Split('\\', '/')).ToList();
        while (strings[0] == "^")
        {
            strings.RemoveAt(0);
            goUpXTimes++;
        }
        findBelow = strings;
    }

    public Transform GetFrom(Transform target)
    {
        Transform pointer = target;

    }
}
*/

public static class Xtensions_TransformPath
{
    public static Transform GetTransformPath(this Transform Init, string path)
    {
        List<string> strings = path.Split('\\').ToList();
        Transform pointer = Init;
        for (int i = 0; i < strings.Count; i++)
        {
            if (strings[i] == "^")
            {
                Transform parentTry = pointer.parent;
                if (parentTry == null) return null;
                pointer = parentTry;
            }
            else
            {
                Transform childTry = pointer.Find(strings[i]);
                if (childTry == null) return null;
                pointer = childTry;
            }
        }
        return pointer;
    }
    public static Transform GetTransformPath(this Transform Init, params string[] strings)
    {
        Transform pointer = Init;
        for (int i = 0; i < strings.Length; i++)
        {
            if (strings[i] == "^")
            {
                Transform parentTry = pointer.parent;
                if (parentTry == null) return null;
                pointer = parentTry;
            }
            else
            {
                Transform childTry = pointer.Find(strings[i]);
                if (childTry == null) return null;
                pointer = childTry;
            }
        }
        return pointer;
    }
}