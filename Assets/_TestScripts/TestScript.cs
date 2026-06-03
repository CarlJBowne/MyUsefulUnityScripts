using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

public class TestScript : MonoBehaviour
{
    [HeaderItem]
    public Rigidbody body;

    public GameObject buffer;

    [HeaderItem(true)]
    public Collider col;
    [HeaderItem(true)]
    public Collider col2;

    private void Awake()
    {
        HeaderItemAttribute.Reset(this);
    }

}

