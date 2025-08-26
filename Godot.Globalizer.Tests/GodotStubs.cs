using System;

namespace Godot;

public class GodotObject { }
public class Node : GodotObject { }

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GlobalClassAttribute : Attribute { }
