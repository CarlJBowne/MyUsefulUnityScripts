using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public abstract class MonoInterface<T> : MonoBehaviour where T : IMonoInferface
{
    public abstract Type Type { get; }
    [field: SerializeField] public List<Component> interfaces { get; private set; }

    protected virtual void Reset() => interfaces = GetComponents<T>().Cast<Component>().ToList();
}

public interface IMonoInferface
{

}