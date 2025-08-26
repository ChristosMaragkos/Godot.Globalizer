// This file is intentionally disabled because the attribute now lives in the Godot.Globalizer.Abstractions project.
// Kept only to avoid accidental recreation; safe to delete in a future cleanup.
#if false
using System;

namespace Godot.Globalizer.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class GlobalizerWrapAttribute : Attribute
{
    public string? WrapperName { get; }

    public GlobalizerWrapAttribute(string? wrapperName)
    {
        WrapperName = wrapperName;
    }
}
#endif
