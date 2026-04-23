using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Utilities.Xtensions.Unity;

public interface IMonoCore<Tthis, Tother>
    where Tthis : MonoBehaviour, IMonoCore<Tthis, Tother>
    where Tother : MonoBehaviour, IMonoComponent<Tother, Tthis>
{
    public static List<MonoBehaviour> InitInterfaces(MonoBehaviour This) => This.GetComponents<Tother>().Cast<MonoBehaviour>().ToList();
    public List<MonoBehaviour> Interfaces { get; }
}

public interface IMonoComponent<Tthis, Tother>
    where Tthis : MonoBehaviour, IMonoComponent<Tthis, Tother>
    where Tother : MonoBehaviour, IMonoCore<Tother, Tthis>
{
    public Tother Interface { get; }
}

public class ExampleInterfaceCore : MonoBehaviour, IMonoCore<ExampleInterfaceCore, ExampleInterfaceComponent>
{
    [field: SerializeField] public List<MonoBehaviour> Interfaces { get; set; }

    private void Reset() => Interfaces = IMonoCore<ExampleInterfaceCore, ExampleInterfaceComponent>.InitInterfaces(this);
}
public class ExampleInterfaceComponent : MonoBehaviour, IMonoComponent<ExampleInterfaceComponent, ExampleInterfaceCore>
{
    [field: SerializeField] public ExampleInterfaceCore Interface { get; set; }

    private void Reset() => Interface = this.GetOrAddComponent<ExampleInterfaceCore>();
}