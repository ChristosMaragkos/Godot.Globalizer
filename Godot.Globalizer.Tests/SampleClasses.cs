using Godot;
using Godot.Globalizer.Attributes;

namespace Samples;

[GlobalizerWrap]
public partial class MyThing : Node { }

[GlobalizerWrap("CustomWrapper")]
public partial class CustomBase : Node { }

[GlobalizerWrap]
public partial class AnotherThing : Node { }

// Manual wrapper that should cause generator to skip creating a GlobalClass wrapper for AnotherThing
public class AnotherThingGlobal : AnotherThing { }

// Should NOT produce a wrapper because it doesn't derive from Godot.Node
//[GlobalizerWrap]
//public class Plain { }

// Empty custom wrapper name should fallback to BlankNameGlobal
[GlobalizerWrap("")]
public partial class BlankName : Node { }

// Whitespace custom wrapper name should fallback to WhiteSpaceNameGlobal
[GlobalizerWrap("   ")]
public partial class WhiteSpaceName : Node { }

// Generic classes should be skipped
[GlobalizerWrap]
public partial class GenericThing: Node { }
