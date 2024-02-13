﻿using System.Collections;
using Vector3System = System.Numerics.Vector3;
using Vector3OpenTK = OpenTK.Mathematics.Vector3;

namespace Echidna2.Mathematics;

public struct Vector3(double x, double y, double z) : IEquatable<Vector3>, IEnumerable<double>
{
	public double X = x;
	public double Y = y;
	public double Z = z;
	
	public static Vector3 FromXY(Vector2 xy, double z = 0) => new(xy.X, xy.Y, z);
	public Vector2 XY => new(X, Y);
	
	public double Length => Math.Sqrt(LengthSquared);
	public double LengthSquared => X * X + Y * Y + Z * Z;
	public Vector3 Normalized => this / Length;
	
	public override int GetHashCode() => HashCode.Combine(X, Y, Z);
	public override bool Equals(object? obj) => obj is Vector3 other && other == this;
	public bool Equals(Vector3 other) => other == this;
	public override string ToString() => $"<{X}, {Y}, {Z}>";
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public IEnumerator<double> GetEnumerator()
	{
		yield return X;
		yield return Y;
		yield return Z;
	}
	
	public static readonly Vector3 Right = new(1, 0, 0);
	public static readonly Vector3 East = new(1, 0, 0);
	public static readonly Vector3 Left = new(-1, 0, 0);
	public static readonly Vector3 West = new(-1, 0, 0);
	public static readonly Vector3 Forward = new(0, 1, 0);
	public static readonly Vector3 North = new(0, 1, 0);
	public static readonly Vector3 Back = new(0, -1, 0);
	public static readonly Vector3 South = new(0, -1, 0);
	public static readonly Vector3 Up = new(0, 0, 1);
	public static readonly Vector3 Out = new(0, 0, 1);
	public static readonly Vector3 Down = new(0, 0, -1);
	public static readonly Vector3 In = new(0, 0, -1);
	public static readonly Vector3 One = new(1, 1, 1);
	public static readonly Vector3 Zero = new(0, 0, 0);
	
	public Vector3 Cross(Vector3 other) => Cross(this, other);
	public double Dot(Vector3 other) => Dot(this, other);
	
	public static Vector3 operator +(Vector3 a) => a;
	public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	public static Vector3 operator -(Vector3 a) => new(-a.X, -a.Y, -a.Z);
	public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	public static Vector3 operator *(Vector3 vector, double scalar) => new(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
	public static Vector3 operator *(double scalar, Vector3 vector) => new(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
	public static Vector3 operator /(Vector3 vector, double scalar) => new(vector.X / scalar, vector.Y / scalar, vector.Z / scalar);
	public static bool operator ==(Vector3 a, Vector3 b) => Math.Abs(a.X - b.X) < double.Epsilon && Math.Abs(a.Y - b.Y) < double.Epsilon && Math.Abs(a.Z - b.Z) < double.Epsilon;
	public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
	
	public static implicit operator Vector3System(Vector3 vector) => new((float)vector.X, (float)vector.Y, (float)vector.Z);
	public static implicit operator Vector3(Vector3System vector) => new(vector.X, vector.Y, vector.Z);
	public static implicit operator Vector3OpenTK(Vector3 vector) => new((float)vector.X, (float)vector.Y, (float)vector.Z);
	public static implicit operator Vector3(Vector3OpenTK vector) => new(vector.X, vector.Y, vector.Z);
	public static implicit operator Vector3((double X, double Y, double Z) vector) => new(vector.X, vector.Y, vector.Z);
	
	public static double DistanceTo(Vector3 a, Vector3 b) => (a - b).Length;
	public static double DistanceSquared(Vector3 a, Vector3 b) => (a - b).LengthSquared;
	public static Vector3 UnitFromTo(Vector3 a, Vector3 b) => (b - a).Normalized;
	public static Vector3 Cross(Vector3 a, Vector3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
	public static double Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
	
	public static Vector3 Sum<T>(IEnumerable<T> source, Func<T, Vector3> selector) => source.Aggregate(Zero, (current, item) => current + selector(item));
}