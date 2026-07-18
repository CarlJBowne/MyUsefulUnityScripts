using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct Placement
{
    public Vector3 positionData { get; private set; }
    public Quaternion rotationData { get; private set; }
    public Transform Transform { get; private set; }

    public readonly Vector3 Position => Transform != null ? Transform.position : positionData;
    public readonly Quaternion Rotation => Transform != null ? Transform.rotation : rotationData;
    public readonly Vector3 EularAngles => Transform != null ? Transform.eulerAngles : rotationData.eulerAngles;


    public Placement(Vector3 position, Quaternion rotation)
    {
        Transform = null;
        positionData = position;
        rotationData = rotation;
    }
    public Placement(Transform transform)
    {
        Transform = transform;
        positionData = transform.position;
        rotationData = transform.rotation;
    }
    public Placement(Vector3 position, Vector3 eularAngles)
    {
        Transform = null;
        positionData = position;
        rotationData = Quaternion.Euler(eularAngles);
    }
    public Placement(Vector3 positionOnly)
    {
        Transform = null;
        positionData = positionOnly;
        rotationData = Quaternion.identity;
    }

    public static implicit operator Placement(Transform transform) => new(transform);
    public static implicit operator Placement(Vector3 position) => new(position);
    public static implicit operator Placement((Vector3 position, Quaternion rotation) data) => new(data.position, data.rotation);
    public static implicit operator Placement((Vector3 position, Vector3 eularAngles) data) => new(data.position, data.eularAngles);
    public static implicit operator Vector3(Placement source) => source.Position;
    public static implicit operator Quaternion(Placement source) => source.Rotation;
    public static implicit operator Transform(Placement source) => source.Transform;
}
