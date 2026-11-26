using SLS.ObjectUtilities;
using UnityEngine;

public class TestScript : MonoBehaviour, ITestable
{
    public Color color;
    public InterfaceList<ITestable, MonoBehaviour> Int;
}

public interface ITestable { }