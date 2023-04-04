using System;

namespace Datamodel.Format;

/// <summary>
/// Subclass this attribute to define a custom attribute name convention.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public abstract class AttributeNameConventionAttribute : System.Attribute
{
    public abstract string GetAttributeName(string propertyName);
}

public class AttributeNameLowercaseAttribute : AttributeNameConventionAttribute
{
    public override string GetAttributeName(string propertyName)
        => propertyName.ToLower();
}

public class AttributeNameCamelCaseAttribute : AttributeNameConventionAttribute
{
    public override string GetAttributeName(string propertyName)
        => char.ToLowerInvariant(propertyName.AsSpan()[0]) + propertyName[1..];
}

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class Attribute : System.Attribute
{
    public Attribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
