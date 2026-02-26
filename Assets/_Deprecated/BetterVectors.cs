#if false


using System;
using TrigHelper;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UV3 = UnityEngine.Vector3;

namespace Deprecated.BetterVectors
{


	[Serializable]
	public struct Vector3
	{

#region Basic Data

		public Vector3(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
		public Vector3(float one)
		{
			x = one;
			y = one;
			z = one;
		}

		public float x;
		public float y;
		public float z;

		public UnityEngine.Vector3 unity { get => this; set => this = value; }

		public Vector3 normalized => this / magnitude;
		public void Normalize() => this = normalized;

		public float magnitude => sqrMagnitude.SQRT();
		public float sqrMagnitude => (x * x) + (y * y) + (z * z);

		public void Set(float newX, float newY, float newZ) { x = newX; y = newY; z = newZ; }
		public override bool Equals(object obj) => obj is Vector3 vector && x == vector.x && y == vector.y && z == vector.z;
		public override int GetHashCode() => HashCode.Combine(x, y, z, unity);
		public new string ToString() => "(" + x + ", " + y + ", " + z + ")";

#endregion

#region Operators

		public static implicit operator UnityEngine.Vector3(Vector3 @in) => new(@in.x, @in.y, @in.z);

		public static implicit operator Vector3(UnityEngine.Vector3 @in) => new(@in.x, @in.y, @in.z);
			
		

		public static bool operator ==(Vector3 l, Vector3 r) => l == r;
		public static bool operator !=(Vector3 l, Vector3 r) => l != r;

		public static Vector3 operator +(Vector3 l, Vector3 r)=>new(l.x + r.x, l.y + r.y, l.z + r.z);
		public static Vector3 operator -(Vector3 l, Vector3 r)=>new(l.x - r.x, l.y - r.y, l.z - r.z);
		public static Vector3 operator *(Vector3 l, Vector3 r) => new(l.x * r.x, l.y * r.y, l.z * r.z);
		public static Vector3 operator /(Vector3 l, Vector3 r) => new (l.x / r.x, l.y / r.y, l.z / r.z);
		public static Vector3 operator +(Vector3 l, float r) => new (l.x + r, l.y + r, l.z + r);
		public static Vector3 operator -(Vector3 l, float r) => new (l.x - r, l.y - r, l.z - r);
		public static Vector3 operator *(Vector3 l, float r) => new (l.x * r, l.y * r, l.z * r);
		public static Vector3 operator /(Vector3 l, float r) => new (l.x / r, l.y / r, l.z / r);
		public static Vector3 operator +(float l, Vector3 r) => new (l + r.x, l + r.y, l + r.z);
		public static Vector3 operator -(float l, Vector3 r) => new (l - r.x, l - r.y, l - r.z);
		public static Vector3 operator *(float l, Vector3 r) => new (l * r.x, l * r.y, l * r.z);
		public static Vector3 operator /(float l, Vector3 r) => new (l / r.x, l / r.y, l / r.z);

		public static Vector3 operator -(Vector3 v) => new(-v.x, -v.y, -v.z);
		public static Vector3 operator ++(Vector3 v) => new(v.x + 1, v.y + 1, v.z + 1);
		public static Vector3 operator --(Vector3 v) => new(v.x - 1, v.y - 1, v.z - 1);

		public static explicit operator Vector3Int(Vector3 @in)=>new((int)@in.x, (int)@in.y, (int)@in.z);
		public static implicit operator Vector3(Vector3Int @in)=>new(@in.x, @in.y, @in.z);
		public Vector3Int Int => _Int();
		private Vector3Int _Int()=>new((int)x, (int)y, (int)z);
		

		public static Vector3 operator *(Vector3 l, Quaternion r) => r * new UV3(l.x, l.y, l.z);

#endregion

#region Directions

		public static Vector3 up = new(0, 1, 0);
		public static Vector3 down = new(0, -1, 0);
		public static Vector3 left = new(-1, 0, 0);
		public static Vector3 right = new(1, 0, 0);
		public static Vector3 front = new(0, 0, 1);
		public static Vector3 forwards = new(0, 0, 1);
		public static Vector3 back = new(0, 0, -1);

		public static Vector3 zero = new(0, 0, 0);
		public static Vector3 one = new(1, 1, 1);
		public static Vector3 two = new(2, 2, 2);
		public static Vector3 five = new(5, 5, 5);
		public static Vector3 ten = new(10, 10, 10);
		public static Vector3 nOne = new(-1, -1, -1);

#region Combos

		public static Vector3 upRight = new(1, 1, 0);
		public static Vector3 frontRight = new(1, 0, 1);
		public static Vector3 downRight = new(1, -1, 0);
		public static Vector3 backRight = new(1, 0, -1);

		public static Vector3 upLeft = new(-1, 1, 0);
		public static Vector3 frontLeft = new(-1, 0, 1);
		public static Vector3 downLeft = new(-1, -1, 0);
		public static Vector3 backLeft = new(-1, 0, -1);

		public static Vector3 upFront = new(0, 1, 1);
		public static Vector3 upBack = new(0, 1, -1);
		public static Vector3 downFront = new(0, 1, -1);
		public static Vector3 downBack = new(0, -1, -1);

#endregion

		public static Vector3 inf = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
		public static Vector3 nInf = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

#endregion

#region Squashing

		public Vector3 xz => new(x, 0, z);
		public Vector3 xy => new(x, y, 0);
		public Vector3 yz => new(0, y, z);

		public void Squash(Vector3 v) => this *= one - v.normalized;
		public Vector3 Squashed(Vector3 v) => this * (one - v.normalized);

#endregion

#region Rotation

		public Quaternion ToQuaternion() => Quaternion.LookRotation(((UnityEngine.Vector3)this).normalized);

		public void Rotate(float amount, Vector3 axis) => this = this * Quaternion.AngleAxis(amount, axis);
		public Vector3 Rotated(float amount, Vector3 axis) => Quaternion.AngleAxis(amount, axis) * this;
		public void Rotate(Eular eularAngle) => this = Quaternion.Euler(eularAngle) * this;
		public Vector3 Rotated(Eular eularAngle) => Quaternion.Euler(eularAngle) * this;

		public void RotateTo(Vector3 towards) => this = Quaternion.FromToRotation(this, towards) * this;
		public void RotateTo(Vector3 towards, Vector3 reference) => this = Quaternion.FromToRotation(reference, towards) * this;
		public Vector3 RotatedTo(Vector3 towards) => Quaternion.FromToRotation(this, towards) * this;
		public Vector3 RotatedTo(Vector3 towards, Vector3 reference) => Quaternion.FromToRotation(reference, towards) * this;

		public Eular EularRotation() => Quaternion.LookRotation(normalized).eulerAngles;
		public Eular EularRotation(Vector3 up) => Quaternion.LookRotation(RotatedTo(normalized, up)).eulerAngles;

		public Vector3 rightTurn => Rotated(Eular.rightTurn);
		public Vector3 leftTurn => Rotated(Eular.leftTurn);
		public Vector3 upTurn => Rotated(Eular.upTurn);
		public Vector3 downTurn => Rotated(Eular.downTurn);
		public Vector3 aroundTurn => Rotated(Eular.aroundTurn);

#endregion

#region Randomization

		public static Vector3 Random()
		{
			Vector3 result = new();
			result.x.Random(-1, 1);
			result.y.Random(-1, 1);
			result.z.Random(-1, 1);
			return result;
		}
		public static Vector3 Random(float min, float max)
		{
			Vector3 result = new();
			result.x.Random(min, max);
			result.y.Random(min, max);
			result.z.Random(min, max);
			return result;
		}
		public static Vector3 Random(Vector3 min, Vector3 max)
		{
			Vector3 result = new();
			result.x.Random(min.x, max.x);
			result.y.Random(min.y, max.y);
			result.z.Random(min.z, max.z);
			return result;
		}
		public static Vector3 Random(Vector3 max)
		{
			Vector3 result = new();
			result.x.Random(0, max.x);
			result.y.Random(0, max.y);
			result.z.Random(0, max.z);
			return result;
		}
		public static Vector3 Random(float x, float y, float z)
		{
			Vector3 result = new();
			result.x.Random(0, x);
			result.y.Random(0, y);
			result.z.Random(0, z);
			return result;
		}

		public Vector3 Randomize()
		{
			x.Random(-1, 1);
			y.Random(-1, 1);
			z.Random(-1, 1);
			return this;
		}
		public Vector3 Randomize(float min, float max)
		{
			x.Random(min, max);
			y.Random(min, max);
			z.Random(min, max);
			return this;
		}
		public Vector3 Randomize(Vector3 min, Vector3 max)
		{
			x.Random(min.x, max.x);
			y.Random(min.y, max.y);
			z.Random(min.z, max.z);
			return this;
		}
		public Vector3 Randomize(Vector3 max)
		{
			x.Random(0, max.x);
			y.Random(0, max.y);
			z.Random(0, max.z);
			return this;
		}
		public Vector3 Randomize(float x, float y, float z)
		{
			x.Random(0, x);
			y.Random(0, y);
			z.Random(0, z);
			return this;
		}


#endregion

#region Copied Static Functions

		public static float Angle(Vector3 from, Vector3 to) => UnityEngine.Vector3.Angle(from, to);
		public static Vector3 ClampMagnitude(Vector3 vector, float maxLength) => UnityEngine.Vector3.ClampMagnitude(vector, maxLength);
		public static Vector3 Cross(Vector3 lhs, Vector3 rhs) => UnityEngine.Vector3.Cross(lhs, rhs);
		public static float Distance(Vector3 a, Vector3 b) => UnityEngine.Vector3.Distance(a, b);
		public static float Dot(Vector3 lhs, Vector3 rhs) => UnityEngine.Vector3.Dot(lhs, rhs);
		public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => UnityEngine.Vector3.Lerp(a, b, t);
		public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => UnityEngine.Vector3.LerpUnclamped(a, b, t);
		public static Vector3 Max(Vector3 lhs, Vector3 rhs) => UnityEngine.Vector3.Max(lhs, rhs);
		public static Vector3 Min(Vector3 lhs, Vector3 rhs) => UnityEngine.Vector3.Min(lhs, rhs);
		public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta) => UnityEngine.Vector3.MoveTowards(current, target, maxDistanceDelta);
		public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent)
		{
			UnityEngine.Vector3 normalU = normal.unity, tangentU = tangent.unity;
			UnityEngine.Vector3.OrthoNormalize(ref normalU, ref tangentU);
			normal = normalU; tangent = tangentU;
		}
		public static Vector3 Project(Vector3 vector, Vector3 onNormal) => UnityEngine.Vector3.Project(vector, onNormal);
		public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal) => UnityEngine.Vector3.ProjectOnPlane(vector, planeNormal);
		public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal) => UnityEngine.Vector3.Reflect(inDirection, inNormal);
		public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis) => UnityEngine.Vector3.SignedAngle(from, to, axis);
		public static Vector3 Slerp(Vector3 a, Vector3 b, float t) => UnityEngine.Vector3.Slerp(a, b, t);
		public static Vector3 SlerpUnclamped(Vector3 a, Vector3 b, float t) => UnityEngine.Vector3.SlerpUnclamped(a, b, t);
		public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed = Mathf.Infinity)
		{
			UnityEngine.Vector3 currentVelocityU = currentVelocity.unity;
			Vector3 result = UnityEngine.Vector3.SmoothDamp(current, target, ref currentVelocityU, smoothTime, maxSpeed, maxSpeed);
			currentVelocity = currentVelocityU;
			return result;
		}


#endregion Copied Static Functions

	}
	[Serializable]
	public struct Vector3Int
	{

#region Basic Data

		public Vector3Int(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
		public Vector3Int(int one)
		{
			x = one;
			y = one;
			z = one;
		}

		public int x;
		public int y;
		public int z;

		public UnityEngine.Vector3Int unity { get => this; set => this = value; }

		public Vector3Int normalized => this / magnitude;
		public void Normalize() => this = normalized;

		public int magnitude => sqrMagnitude.Float().SQRT().Int();
		public int sqrMagnitude => (x * x) + (y * y) + (z * z);

		public void Set(int newX, int newY, int newZ) { x = newX; y = newY; z = newZ; }
		public override bool Equals(object obj) => obj is Vector3Int vector && x == vector.x && y == vector.y && z == vector.z;
		public override int GetHashCode() => HashCode.Combine(x, y, z, unity);
		public new string ToString() => "(" + x + ", " + y + ", " + z + ")";

#endregion

#region Operators

		public static implicit operator UnityEngine.Vector3Int(Vector3Int @in)
		{
			UnityEngine.Vector3Int result = UnityEngine.Vector3Int.zero;
			result.x = @in.x;
			result.y = @in.y;
			result.z = @in.z;
			return result;
		}
		public static implicit operator Vector3Int(UnityEngine.Vector3Int @in)
		{
			Vector3Int result;
			result.x = @in.x;
			result.y = @in.y;
			result.z = @in.z;
			return result;
		}

		public static bool operator ==(Vector3Int l, Vector3Int r) => l == r;
		public static bool operator !=(Vector3Int l, Vector3Int r) => l != r;

		public static Vector3Int operator +(Vector3Int l, Vector3Int r)
		{
			Vector3Int result;
			result.x = l.x + r.x;
			result.y = l.y + r.y;
			result.z = l.z + r.z;
			return result;
		}
		public static Vector3Int operator -(Vector3Int l, Vector3Int r)
		{
			Vector3Int result;
			result.x = l.x - r.x;
			result.y = l.y - r.y;
			result.z = l.z - r.z;
			return result;
		}
		public static Vector3Int operator *(Vector3Int l, Vector3Int r)
		{
			Vector3Int result;
			result.x = l.x * r.x;
			result.y = l.y * r.y;
			result.z = l.z * r.z;
			return result;
		}
		public static Vector3Int operator /(Vector3Int l, Vector3Int r)
		{
			Vector3Int result;
			result.x = l.x / r.x;
			result.y = l.y / r.y;
			result.z = l.z / r.z;
			return result;
		}
		public static Vector3Int operator +(Vector3Int l, int r)
		{
			Vector3Int result;
			result.x = l.x + r;
			result.y = l.y + r;
			result.z = l.z + r;
			return result;
		}
		public static Vector3Int operator -(Vector3Int l, int r)
		{
			Vector3Int result;
			result.x = l.x - r;
			result.y = l.y - r;
			result.z = l.z - r;
			return result;
		}
		public static Vector3Int operator *(Vector3Int l, int r)
		{
			Vector3Int result;
			result.x = l.x * r;
			result.y = l.y * r;
			result.z = l.z * r;
			return result;
		}
		public static Vector3Int operator /(Vector3Int l, int r)
		{
			Vector3Int result;
			result.x = l.x / r;
			result.y = l.y / r;
			result.z = l.z / r;
			return result;
		}
		public static Vector3Int operator +(int l, Vector3Int r)
		{
			Vector3Int result;
			result.x = l + r.x;
			result.y = l + r.y;
			result.z = l + r.z;
			return result;
		}
		public static Vector3Int operator -(int l, Vector3Int r)
		{
			Vector3Int result;
			result.x = l - r.x;
			result.y = l - r.y;
			result.z = l - r.z;
			return result;
		}
		public static Vector3Int operator *(int l, Vector3Int r)
		{
			Vector3Int result;
			result.x = l * r.x;
			result.y = l * r.y;
			result.z = l * r.z;
			return result;
		}
		public static Vector3Int operator /(int l, Vector3Int r)
		{
			Vector3Int result;
			result.x = l / r.x;
			result.y = l / r.y;
			result.z = l / r.z;
			return result;
		}

		public static Vector3Int operator -(Vector3Int v)
		{
			Vector3Int result;
			result.x = -v.x;
			result.y = -v.y;
			result.z = -v.z;
			return result;
		}
		public static Vector3Int operator ++(Vector3Int v)
		{
			Vector3Int result;
			result.x = v.x + 1;
			result.y = v.y + 1;
			result.z = v.z + 1;
			return result;
		}
		public static Vector3Int operator --(Vector3Int v)
		{
			Vector3Int result;
			result.x = v.x - 1;
			result.y = v.y - 1;
			result.z = v.z - 1;
			return result;
		}

		public Vector3 Float => _Float();
		private Vector3 _Float()
		{
			Vector3 result;
			result.x = x;
			result.y = y;
			result.z = z;
			return result;
		}

#endregion

#region Directions

		public static Vector3Int up = new(0, 1, 0);
		public static Vector3Int down = new(0, -1, 0);
		public static Vector3Int left = new(-1, 0, 0);
		public static Vector3Int right = new(1, 0, 0);
		public static Vector3Int front = new(0, 0, 1);
		public static Vector3Int forwards = new(0, 0, 1);
		public static Vector3Int back = new(0, 0, -1);

		public static Vector3Int zero = new(0, 0, 0);
		public static Vector3Int one = new(1, 1, 1);
		public static Vector3Int two = new(2, 2, 2);
		public static Vector3Int five = new(5, 5, 5);
		public static Vector3Int ten = new(10, 10, 10);
		public static Vector3Int nOne = new(-1, -1, -1);

#region Combos

		public static Vector3Int upRight = new(1, 1, 0);
		public static Vector3Int frontRight = new(1, 0, 1);
		public static Vector3Int downRight = new(1, -1, 0);
		public static Vector3Int backRight = new(1, 0, -1);

		public static Vector3Int upLeft = new(-1, 1, 0);
		public static Vector3Int frontLeft = new(-1, 0, 1);
		public static Vector3Int downLeft = new(-1, -1, 0);
		public static Vector3Int backLeft = new(-1, 0, -1);

		public static Vector3Int upFront = new(0, 1, 1);
		public static Vector3Int upBack = new(0, 1, -1);
		public static Vector3Int downFront = new(0, 1, -1);
		public static Vector3Int downBack = new(0, -1, -1);

#endregion

		public static Vector3Int inf = new(int.MaxValue, int.MaxValue, int.MaxValue);
		public static Vector3Int nInf = new(int.MinValue, int.MinValue, int.MinValue);

#endregion

#region Squashing

		public Vector3Int xz => new(x, 0, z);
		public Vector3Int xy => new(x, y, 0);
		public Vector3Int yz => new(0, y, z);

		public void Squash(Vector3Int v) => this *= one - v.normalized;
		public Vector3Int Squashed(Vector3Int v) => this * (one - v.normalized);

#endregion

#region Rotation

		public Quaternion ToQuaternion() => Quaternion.LookRotation(((UnityEngine.Vector3)Float).normalized);

		public void Rotate(float amount, Vector3 axis) => this = (Quaternion.AngleAxis(amount, axis) * Float).Better().Int;
		public Vector3Int Rotated(float amount, Vector3 axis) => (Quaternion.AngleAxis(amount, axis) * Float).Better().Int;
		public void Rotate(Eular eularAngle) => this = (Quaternion.Euler(eularAngle) * Float).Better().Int;
		public Vector3Int Rotated(Eular eularAngle) => (Quaternion.Euler(eularAngle) * Float).Better().Int;

		public void RotateTo(Vector3Int towards) => this = (Quaternion.FromToRotation(Float.unity, towards.Float.unity) * Float).Better().Int;
		public void RotateTo(Vector3Int towards, Vector3Int reference) => this = (Quaternion.FromToRotation(reference.Float.unity, towards.Float.unity) * Float).Better().Int;
		public Vector3Int RotatedTo(Vector3Int towards) => (Quaternion.FromToRotation(Float.unity, towards.Float.unity) * Float).Better().Int;
		public Vector3Int RotatedTo(Vector3Int towards, Vector3Int reference) => (Quaternion.FromToRotation(reference.Float.unity, towards.Float.unity) * Float).Better().Int;

		public Eular EularRotation() => Quaternion.LookRotation(normalized.Float).eulerAngles;
		public Eular EularRotation(Vector3Int up) => Quaternion.LookRotation(RotatedTo(normalized, up).Float).eulerAngles;

		public Vector3Int rightTurn => Rotated(Eular.rightTurn);
		public Vector3Int leftTurn => Rotated(Eular.leftTurn);
		public Vector3Int upTurn => Rotated(Eular.upTurn);
		public Vector3Int downTurn => Rotated(Eular.downTurn);
		public Vector3Int aroundTurn => Rotated(Eular.aroundTurn);

#endregion

#region Randomization

		public static Vector3Int Random()
		{
			Vector3Int result = new();
			result.x.Random(-1, 1);
			result.y.Random(-1, 1);
			result.z.Random(-1, 1);
			return result;
		}
		public static Vector3Int Random(int min, int max)
		{
			Vector3Int result = new();
			result.x.Random(min, max);
			result.y.Random(min, max);
			result.z.Random(min, max);
			return result;
		}
		public static Vector3Int Random(Vector3Int min, Vector3Int max)
		{
			Vector3Int result = new();
			result.x.Random(min.x, max.x);
			result.y.Random(min.y, max.y);
			result.z.Random(min.z, max.z);
			return result;
		}
		public static Vector3Int Random(Vector3Int max)
		{
			Vector3Int result = new();
			result.x.Random(0, max.x);
			result.y.Random(0, max.y);
			result.z.Random(0, max.z);
			return result;
		}
		public static Vector3Int Random(int x, int y, int z)
		{
			Vector3Int result = new();
			result.x.Random(0, x);
			result.y.Random(0, y);
			result.z.Random(0, z);
			return result;
		}

		public Vector3Int Randomize()
		{
			x.Random(-1, 1);
			y.Random(-1, 1);
			z.Random(-1, 1);
			return this;
		}
		public Vector3Int Randomize(int min, int max)
		{
			x.Random(min, max);
			y.Random(min, max);
			z.Random(min, max);
			return this;
		}
		public Vector3Int Randomize(Vector3Int min, Vector3Int max)
		{
			x.Random(min.x, max.x);
			y.Random(min.y, max.y);
			z.Random(min.z, max.z);
			return this;
		}
		public Vector3Int Randomize(Vector3Int max)
		{
			x.Random(0, max.x);
			y.Random(0, max.y);
			z.Random(0, max.z);
			return this;
		}
		public Vector3Int Randomize(int x, int y, int z)
		{
			x.Random(0, x);
			y.Random(0, y);
			z.Random(0, z);
			return this;
		}


#endregion

#region Copied Static Functions

		public static int Distance(Vector3Int a, Vector3Int b) => (int)UnityEngine.Vector3Int.Distance(a, b);
		public static Vector3Int Max(Vector3Int lhs, Vector3Int rhs) => UnityEngine.Vector3Int.Max(lhs, rhs);
		public static Vector3Int Min(Vector3Int lhs, Vector3Int rhs) => UnityEngine.Vector3Int.Min(lhs, rhs);


#endregion Copied Static Functions

	}

	[Serializable]
	public struct Eular
	{
		public Eular(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public float x;
		public float y;
		public float z;

		public UnityEngine.Vector3 unity => (UnityEngine.Vector3)this;

		public static implicit operator UnityEngine.Vector3(Eular @in) => new(@in.x, @in.y, @in.z);
		public static implicit operator Eular(UnityEngine.Vector3 @in) => new(@in.x, @in.y, @in.z);

		public static bool operator ==(Eular l, Eular r) => l == r;
		public static bool operator !=(Eular l, Eular r) => l != r;

		public static Eular operator +(Eular l, Eular r)
		{
			Eular result;
			result.x = l.x + r.x;
			result.y = l.y + r.y;
			result.z = l.z + r.z;
			return result;
		}
		public static Eular operator -(Eular l, Eular r)
		{
			Eular result;
			result.x = l.x - r.x;
			result.y = l.y - r.y;
			result.z = l.z - r.z;
			return result;
		}
		public static Eular operator *(Eular l, Eular r)
		{
			Eular result;
			result.x = l.x * r.x;
			result.y = l.y * r.y;
			result.z = l.z * r.z;
			return result;
		}
		public static Eular operator /(Eular l, Eular r)
		{
			Eular result;
			result.x = l.x / r.x;
			result.y = l.y / r.y;
			result.z = l.z / r.z;
			return result;
		}
		public static Eular operator +(Eular l, float r)
		{
			Eular result;
			result.x = l.x + r;
			result.y = l.y + r;
			result.z = l.z + r;
			return result;
		}
		public static Eular operator -(Eular l, float r)
		{
			Eular result;
			result.x = l.x - r;
			result.y = l.y - r;
			result.z = l.z - r;
			return result;
		}
		public static Eular operator *(Eular l, float r)
		{
			Eular result;
			result.x = l.x * r;
			result.y = l.y * r;
			result.z = l.z * r;
			return result;
		}
		public static Eular operator /(Eular l, float r)
		{
			Eular result;
			result.x = l.x / r;
			result.y = l.y / r;
			result.z = l.z / r;
			return result;
		}
		public static Eular operator +(float l, Eular r)
		{
			Eular result;
			result.x = l + r.x;
			result.y = l + r.y;
			result.z = l + r.z;
			return result;
		}
		public static Eular operator -(float l, Eular r)
		{
			Eular result;
			result.x = l - r.x;
			result.y = l - r.y;
			result.z = l - r.z;
			return result;
		}
		public static Eular operator *(float l, Eular r)
		{
			Eular result;
			result.x = l * r.x;
			result.y = l * r.y;
			result.z = l * r.z;
			return result;
		}
		public static Eular operator /(float l, Eular r)
		{
			Eular result;
			result.x = l / r.x;
			result.y = l / r.y;
			result.z = l / r.z;
			return result;
		}

		public static Eular operator -(Eular @in)
		{
			Eular result;
			result.x = -@in.x;
			result.y = -@in.y;
			result.z = -@in.z;
			return result;
		}
		public static Eular operator ++(Eular @in)
		{
			Eular result;
			result.x = @in.x + 1;
			result.y = @in.y + 1;
			result.z = @in.z + 1;
			return result;
		}
		public static Eular operator --(Eular @in)
		{
			Eular result;
			result.x = @in.x - 1;
			result.y = @in.y - 1;
			result.z = @in.z - 1;
			return result;
		}

		public override bool Equals(object obj) => obj is Eular eular && x == eular.x && y == eular.y && z == eular.z;
		public override int GetHashCode() => HashCode.Combine(x, y, z);

		public Eular ClampedToCircle()
		{
			Eular result;
			result.x = x % FullCircle;
			result.y = y % FullCircle;
			result.z = z % FullCircle;
			return result;
		}
		public Eular ClampedToCircleMirrored()
		{
			Eular result;
			result.x = x % (FullCircle * (x >= 0 ? 1 : -1));
			result.y = y % (FullCircle * (y >= 0 ? 1 : -1));
			result.z = z % (FullCircle * (z >= 0 ? 1 : -1));
			return result;
		}
		public Eular ClampedToHalfCircleMirrored()
		{
			Eular result;
			result.x = ((x + HalfCircle) % FullCircle) - HalfCircle;
			result.y = ((y + HalfCircle) % FullCircle) - HalfCircle;
			result.z = ((z + HalfCircle) % FullCircle) - HalfCircle;
			return result;
		}
		public void ClampToCircle()
		{
			x %= FullCircle;
			y %= FullCircle;
			z %= FullCircle;
		}
		public void ClampToCircleMirrored()
		{
			x %= FullCircle * (x >= 0 ? 1 : -1);
			y %= FullCircle * (y >= 0 ? 1 : -1);
			z %= FullCircle * (z >= 0 ? 1 : -1);
		}
		public void ClampToHalfCircleMirrored()
		{
			x = ((x + HalfCircle) % FullCircle) - HalfCircle;
			y = ((y + HalfCircle) % FullCircle) - HalfCircle;
			z = ((z + HalfCircle) % FullCircle) - HalfCircle;
		}

		public static Eular rightTurn = new(0, 90, 0);
		public static Eular leftTurn = new(0, -90, 0);
		public static Eular aroundTurn = new(0, 180, 0);
		public static Eular upTurn = new(90, 0, 0);
		public static Eular downTurn = new(-90, 0, 0);

		public const float FullCircle = 360;
		public const float HalfCircle = 180;
		public const float QuarterCircle = 90;

	}

	[Serializable]
	public struct Vector2
	{
#region Basic Data

#endregion

#region Operators

#endregion

#region Directions

#endregion

#region Squashing

#endregion

#region Rotation

#endregion

#region Randomization

#endregion


	}

















	public static class Vector3ExtensionMethods
	{
		public static Vector3 Better(this UV3 v) => v;

		public static void Rotate(this UV3 v, float amount, UV3 axis) => v = Quaternion.AngleAxis(amount, axis) * v;
	}







	//Drawers

	[CustomPropertyDrawer(typeof(Vector3))]
	public class Vector3Drawer : PropertyDrawer
	{

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			VisualElement root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Vector3Drawer.uxml").CloneTree(property.propertyPath);

			root.Q<Vector3Field>("Vector").label = property.displayName;

			FloatField xField = root.Q<FloatField>("unity-x-input");
			FloatField yField = root.Q<FloatField>("unity-y-input");
			FloatField zField = root.Q<FloatField>("unity-z-input");

			xField.bindingPath = "x";
			yField.bindingPath = "y";
			zField.bindingPath = "z";

			xField.BindProperty(property.FindPropertyRelative("x").serializedObject);
			yField.BindProperty(property.FindPropertyRelative("y").serializedObject);
			zField.BindProperty(property.FindPropertyRelative("z").serializedObject);

			return root;
		}

	}

	[CustomPropertyDrawer(typeof(Vector3Int))]
	public class Vector3IntDrawer : PropertyDrawer
	{

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			VisualElement root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Vector3IntDrawer.uxml").CloneTree(property.propertyPath);

			root.Q<Vector3IntField>("Vector").label = property.displayName;

			IntegerField xField = root.Q<IntegerField>("unity-x-input");
			IntegerField yField = root.Q<IntegerField>("unity-y-input");
			IntegerField zField = root.Q<IntegerField>("unity-z-input");

			xField.bindingPath = "x";
			yField.bindingPath = "y";
			zField.bindingPath = "z";

			xField.BindProperty(property.FindPropertyRelative("x").serializedObject);
			yField.BindProperty(property.FindPropertyRelative("y").serializedObject);
			zField.BindProperty(property.FindPropertyRelative("z").serializedObject);

			return root;
		}

	}

	[CustomPropertyDrawer(typeof(Vector2))]
	public class Vector2Drawer : PropertyDrawer
	{

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			VisualElement root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Vector2Drawer.uxml").CloneTree(property.propertyPath);

			root.Q<Vector2Field>("Vector").label = property.displayName;

			FloatField xField = root.Q<FloatField>("unity-x-input");
			FloatField yField = root.Q<FloatField>("unity-y-input");

			xField.bindingPath = "x";
			yField.bindingPath = "y";

			xField.BindProperty(property.FindPropertyRelative("x").serializedObject);
			yField.BindProperty(property.FindPropertyRelative("y").serializedObject);

			return root;
		}

	}

	[CustomPropertyDrawer(typeof(Vector2Int))]
	public class Vector2IntDrawer : PropertyDrawer
	{

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			VisualElement root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Vector2IntDrawer.uxml").CloneTree(property.propertyPath);

			root.Q<Vector2IntField>("Vector").label = property.displayName;

			IntegerField xField = root.Q<IntegerField>("unity-x-input");
			IntegerField yField = root.Q<IntegerField>("unity-y-input");

			xField.bindingPath = "x";
			yField.bindingPath = "y";

			xField.BindProperty(property.FindPropertyRelative("x").serializedObject);
			yField.BindProperty(property.FindPropertyRelative("y").serializedObject);

			return root;
		}

	}

	[CustomPropertyDrawer(typeof(Eular))]
	public class EularDrawer : PropertyDrawer
	{
		private VisualElement root;

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/EularDrawer.uxml").CloneTree(property.propertyPath);

			root.Q<Vector3Field>("Vector").label = property.displayName;

			FloatField xField = root.Q<FloatField>("unity-x-input");
			FloatField yField = root.Q<FloatField>("unity-y-input");
			FloatField zField = root.Q<FloatField>("unity-z-input");

			xField.bindingPath = "x";
			yField.bindingPath = "y";
			zField.bindingPath = "z";

			xField.BindProperty(property.FindPropertyRelative("x").serializedObject);
			yField.BindProperty(property.FindPropertyRelative("y").serializedObject);
			zField.BindProperty(property.FindPropertyRelative("z").serializedObject);

			return root;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty xField = property.FindPropertyRelative("x");
			SerializedProperty yField = property.FindPropertyRelative("y");
			SerializedProperty zField = property.FindPropertyRelative("z");

			if (xField.floatValue < 0) xField.floatValue = 360f - xField.floatValue;
			if (yField.floatValue < 0) yField.floatValue = 360f - yField.floatValue;
			if (zField.floatValue < 0) zField.floatValue = 360f - zField.floatValue;
			if (xField.floatValue > 360) xField.floatValue = 0 + xField.floatValue;
			if (yField.floatValue > 360) yField.floatValue = 0 + yField.floatValue;
			if (zField.floatValue > 360) zField.floatValue = 0 + zField.floatValue;
		}
	}


}










#endif