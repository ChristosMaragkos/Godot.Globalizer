using System;

namespace Godot.Globalizer.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GlobalizerWrapAttribute : Attribute
{
    public string? WrapperName { get; }
    public GlobalizerWrapAttribute(string? wrapperName = null) => WrapperName = wrapperName;
}

