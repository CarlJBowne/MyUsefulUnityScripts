using UnityEngine;

public static class XtensionsVector3
{

	public static Vector3 Scaled(this Vector3 v, Vector3 other)
		=> new(v.x * other.x, v.y * other.y, v.z * other.z);
	public static Vector3 Scaled(this Vector3 v, float x = 1f, float y = 1f, float z = 1f)
		=> new(v.x * x, v.y * y, v.z * z);
	public static Vector3 Scaled(this Vector3 v, float all)
		=> new(v.x * all, v.y * all, v.z * all);

	public static Vector3 Divided(this Vector3 v, Vector3 other)
		=> new(v.x / other.x, v.y / other.y, v.z / other.z);
	public static Vector3 Divided(this Vector3 v, float x = 1f, float y = 1f, float z = 1f)
		=> new(v.x / x, v.y / y, v.z / z);
	public static Vector3 Divided(this Vector3 v, float all)
		=> new(v.x / all, v.y / all, v.z / all);

	public static Vector3 XY(this Vector3 v) => new(v.x, v.y, 0f);
	public static Vector3 XZ(this Vector3 v) => new(v.x, 0f, v.z);
	public static Vector3 YZ(this Vector3 v) => new(0f, v.y, v.z);

	public static Vector3 Squashed(this Vector3 v, Vector3 direction)
		=> new (v.x * (1-direction.normalized.x), v.y * (1 - direction.normalized.y), v.z * (1 - direction.normalized.z));

	public static Vector3 ToXZ(this Vector2 v) => new(v.x, 0, v.y);
	public static Vector2 ZtoY(this Vector3 v) => new(v.x, v.z);
	public static Vector3 To3(this Vector2 v) => new(v.x, v.y, 0);
	public static Vector2 To2(this Vector3 v) => new(v.x, v.y);

	public static Vector3 Swizzle(this Vector3 v)
		=> new(v.x, v.z, v.y);

	public static Vector3 Rotated(this Vector3 v, float amount, Vector3 axis)
		=> Quaternion.AngleAxis(amount, axis) * v;
	public static Vector3 Rotated(this Vector3 v, Vector3 eularAngle)
		=> Quaternion.Euler(eularAngle) * v;

	public static Vector3 RotatedTo(this Vector3 v, Vector3 towards)
		=> Quaternion.FromToRotation(v, towards) * v;
	public static Vector3 RotatedTo(this Vector3 v, Vector3 towards, Vector3 reference)
		=> Quaternion.FromToRotation(reference, towards) * v;

	public static Vector3 EularRotation(this Vector3 v)
		=> Quaternion.LookRotation(v.normalized).eulerAngles;
	public static Vector3 EularRotation(this Vector3 v, Vector3 up)
		=> Quaternion.LookRotation(Quaternion.FromToRotation(v.normalized, up) * v).eulerAngles;

	public static Vector3 TurnRight(this Vector3 v) => Quaternion.Euler(Vector3.up * 90) * v;
	public static Vector3 TurnLeft(this Vector3 v) => Quaternion.Euler(Vector3.up * -90) * v;
	public static Vector3 TurnUp(this Vector3 v) => Quaternion.Euler(Vector3.up * 180) * v;
	public static Vector3 TurnDown(this Vector3 v) => Quaternion.Euler(Vector3.right * 90) * v;
	public static Vector3 TurnAround(this Vector3 v) => Quaternion.Euler(Vector3.right * -90) * v;

	public static Vector3 Randomize(this Vector3 v)
	{
		v.x = Random.Range(-1, 1);
		v.y = Random.Range(-1, 1);
		v.z = Random.Range(-1, 1);
		return v;
	}
	public static Vector3 Randomize(this Vector3 v, float min, float max)
	{
		v.x = Random.Range(min, max);
		v.y = Random.Range(min, max);
		v.z = Random.Range(min, max);
		return v;
	}
	public static Vector3 Randomize(this Vector3 v, Vector3 min, Vector3 max)
	{
		v.x = Random.Range(min.x, max.x);
		v.y = Random.Range(min.y, max.y);
		v.z = Random.Range(min.z, max.z);
		return v;
	}
	public static Vector3 Randomize(this Vector3 v, Vector3 max)
	{
		v.x = Random.Range(0, max.x);
		v.y = Random.Range(0, max.y);
		v.z = Random.Range(0, max.z);
		return v;
	}
	public static Vector3 Randomize(this Vector3 v, float x, float y, float z)
	{
		v.x = Random.Range(0, x);
		v.y = Random.Range(0, y);
		v.z = Random.Range(0, z);
		return v;
	}

	public static Vector3Int Randomize(this Vector3Int v)
	{
		v.x = Random.Range(-1, 1);
		v.y = Random.Range(-1, 1);
		v.z = Random.Range(-1, 1);
		return v;
	}
	public static Vector3Int Randomize(this Vector3Int v, int min, int max)
	{
		v.x = Random.Range(min, max);
		v.y = Random.Range(min, max);
		v.z = Random.Range(min, max);
		return v;
	}
	public static Vector3Int Randomize(this Vector3Int v, Vector3Int min, Vector3Int max)
	{
		v.x = Random.Range(min.x, max.x);
		v.y = Random.Range(min.y, max.y);
		v.z = Random.Range(min.z, max.z);
		return v;
	}
	public static Vector3Int Randomize(this Vector3Int v, Vector3Int max)
	{
		v.x = Random.Range(0, max.x);
		v.y = Random.Range(0, max.y);
		v.z = Random.Range(0, max.z);
		return v;
	}
	public static Vector3Int Randomize(this Vector3Int v, int x, int y, int z)
	{
		v.x = Random.Range(0, x);
		v.y = Random.Range(0, y);
		v.z = Random.Range(0, z);
		return v;
	}


	public static Vector3 DirToRot(this Vector3 value) => Quaternion.LookRotation(value, Vector3.up).eulerAngles;
	public static Vector3 RotToDir(this Vector3 value) => Quaternion.Euler(value) * Vector3.forward; 

	public static Vector3 ProjectAndScale(this Vector3 value, Vector3 normal) => Vector3.ProjectOnPlane(value, normal).normalized * value.magnitude;


}

public static class Direction
{
	public static Vector3 up = new(0, 1, 0);
	public static Vector3 down = new(0, -1, 0);
    public static Vector3 left = new(-1, 0, 0);
    public static Vector3 right = new(1, 0, 0);
    public static Vector3 forward = new(0, 0, 1);
    public static Vector3 front = new(0, 0, 1);
    public static Vector3 back = new(0, 0, -1);

    public static Vector3 one = new(1, 1, 1);
    public static Vector3 zero = new(0, 0, 0);

    public static Vector3 two = new(2,2,2);
    public static Vector3 five = new(5,5,5);
    public static Vector3 ten = new(10,10,10);
    public static Vector3 nOne = new(-1,-1,-1);

    public static Vector3 inf = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
	public static Vector3 nInf = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

    public static Vector3 upRight =		new(1, 1, 0);
    public static Vector3 frontRight =	new(1, 0, 1);
    public static Vector3 downRight =	new(1, -1, 0);
    public static Vector3 backRight =	new(1, 0, -1);

    public static Vector3 upLeft = new(-1, 1, 0);
    public static Vector3 frontLeft = new(-1, 0, 1);
	public static Vector3 downLeft = new(-1, -1, 0);
	public static Vector3 backLeft = new(-1, 0, -1);

	public static Vector3 upFront = new(0, 1, 1);
    public static Vector3 upBack = new(0, 1, -1);
    public static Vector3 downFront = new(0, -1, 1);
    public static Vector3 downBack = new(0, -1, -1);

}

public static class XtensionsVector2
{
	public static Vector2 Randomize(this Vector2 v)
	{
		v.x = Random.Range(-1, 1);
		v.y = Random.Range(-1, 1);
		return v;
	}
	public static Vector2 Randomize(this Vector2 v, float min, float max)
	{
		v.x = Random.Range(min, max);
		v.y = Random.Range(min, max);
		return v;
	}
	public static Vector2 Randomize(this Vector2 v, Vector2 min, Vector2 max)
	{
		v.x = Random.Range(min.x, max.x);
		v.y = Random.Range(min.y, max.y);
		return v;
	}
	public static Vector2 Randomize(this Vector2 v, Vector2 max)
	{
		v.x = Random.Range(0, max.x);
		v.y = Random.Range(0, max.y);
		return v;
	}
	public static Vector2 Randomize(this Vector2 v, float x, float y, float z)
	{
		v.x = Random.Range(0, x);
		v.y = Random.Range(0, y);
		return v;
	}

	public static Vector2Int Randomize(this Vector2Int v)
	{
		v.x = Random.Range(-1, 1);
		v.y = Random.Range(-1, 1);
		return v;
	}
	public static Vector2Int Randomize(this Vector2Int v, int min, int max)
	{
		v.x = Random.Range(min, max);
		v.y = Random.Range(min, max);
		return v;
	}
	public static Vector2Int Randomize(this Vector2Int v, Vector2Int min, Vector2Int max)
	{
		v.x = Random.Range(min.x, max.x);
		v.y = Random.Range(min.y, max.y);
		return v;
	}
	public static Vector2Int Randomize(this Vector2Int v, Vector2Int max)
	{
		v.x = Random.Range(0, max.x);
		v.y = Random.Range(0, max.y);
		return v;
	}
	public static Vector2Int Randomize(this Vector2Int v, int x, int y, int z)
	{
		v.x = Random.Range(0, x);
		v.y = Random.Range(0, y);
		return v;
	}
	
	public static Vector2 Sign(this Vector2 v)
	{
        Vector2 result = v;
		v.x = Mathf.Sign(v.x);
		v.y = Mathf.Sign(v.y);
		return result;
	}

}

public static class Eular
{

	public static Vector3 rightTurn = Vector3.up * 90;
	public static Vector3 leftTurn = Vector3.up * -90;
	public static Vector3 aroundTurn = Vector3.up * 180;
	public static Vector3 upTurn = Vector3.right * 90;
	public static Vector3 downTurn = Vector3.right * -90;

	public const float FullCircle = 360;
	public const float HalfCircle = 180;
	public const float QuarterCircle = 90;

	public static void EularClamp(this Vector3 v, bool mirrored = false)
	{
		v.x = (!mirrored) ? v.x % FullCircle : (((v.x + HalfCircle) % FullCircle) - HalfCircle);
		v.y = (!mirrored) ? v.y % FullCircle : (((v.y + HalfCircle) % FullCircle) - HalfCircle);
		v.z = (!mirrored) ? v.z % FullCircle : (((v.z + HalfCircle) % FullCircle) - HalfCircle);
	}
	public static Vector3 EularClamped(this Vector3 v, bool mirrored = false)
	{
		return new(
			(!mirrored) ? v.x % FullCircle : (((v.x + HalfCircle) % FullCircle) - HalfCircle),
			(!mirrored) ? v.y % FullCircle : (((v.y + HalfCircle) % FullCircle) - HalfCircle),
			(!mirrored) ? v.z % FullCircle : (((v.z + HalfCircle) % FullCircle) - HalfCircle)
			);
	}
	public static void EularClamp(this float v, bool mirrored = false)
		=> v = (!mirrored) ? v % FullCircle : (((v + HalfCircle) % FullCircle) - HalfCircle);
	public static float EularClamped(this float v, bool mirrored = false)
		=> (!mirrored) ? v % FullCircle : (((v + HalfCircle) % FullCircle) - HalfCircle);
}
