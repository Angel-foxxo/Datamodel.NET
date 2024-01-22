using System;
using System.Numerics;

namespace Datamodel;

public struct QAngle : IEquatable<QAngle>
{
    public float Pitch;
    public float Yaw;
    public float Roll;

    public QAngle(float pitch, float yaw, float roll)
    {
        Pitch = pitch;
        Yaw = yaw;
        Roll = roll;
    }

    public static implicit operator Vector3(QAngle q) => new(q.Pitch, q.Yaw, q.Roll);
    public static implicit operator QAngle(Vector3 v) => new(v.X, v.Y, v.Z);

    public static bool operator ==(QAngle first, QAngle second)
        => first.Pitch == second.Pitch && first.Yaw == second.Yaw && first.Roll == second.Roll;

    public static bool operator !=(QAngle a, QAngle b)
        => !(a == b);

    public readonly override int GetHashCode()
        => HashCode.Combine(Pitch, Yaw, Roll);

    public readonly bool Equals(QAngle other)
        => this == other;

    public readonly override bool Equals(object obj)
    {
        if (obj is QAngle q)
            return this == q;
        return false;
    }
}
