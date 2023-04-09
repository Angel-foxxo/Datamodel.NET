using System;

namespace Datamodel;

public struct Color : IEquatable<Color>
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;

    public Color(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static bool operator ==(Color first, Color second)
        => first.R == second.R && first.G == second.G && first.B == second.B && first.A == second.A;

    public static bool operator !=(Color a, Color b)
        => !(a == b);

    public static Color FromBytes(byte[] bytes)
        => new(bytes[0], bytes[1], bytes[2], bytes[3]);

    public byte[] ToBytes()
        => new byte[] { R, G, B, A };

    public override int GetHashCode()
        => HashCode.Combine(R, G, B, A);

    public bool Equals(Color other)
        => this == other;

    public override bool Equals(object obj)
    {
        if (obj is Color color)
            return this == color;
        return false;
    }
}
