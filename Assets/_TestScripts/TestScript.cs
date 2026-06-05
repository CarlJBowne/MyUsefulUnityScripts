using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using SLS.EditorUtilities.ComponentHeaders;

public class TestScript : MonoBehaviour
{
    [HeaderItem]
    public Rigidbody body;

    public GameObject buffer;

    [HeaderItem(true)]
    public Collider col;
    [HeaderItem(true, "child1/child2")]
    public Collider col2;

    private void Awake()
    {
        HeaderItemAttribute.Reset(this);
    }

}

