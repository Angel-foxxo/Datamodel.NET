using System;
using System.Numerics;

namespace Datamodel.Format;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class DMProperty : System.Attribute
{
    /// <param name="name">The name to use for serialization.</param>
    /// <param name="optional">Ignore serialization if property is on the default value.</param>
    public DMProperty(string? name = null, bool optional = false)
    {
        Name = name;
        Optional = optional;
    }

    public string? Name { get; }
    public bool Optional { get; }
}
