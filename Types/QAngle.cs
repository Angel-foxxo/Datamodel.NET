using System.Numerics;

namespace Datamodel;

public struct QAngle
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
}
