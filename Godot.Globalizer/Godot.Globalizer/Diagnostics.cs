using Microsoft.CodeAnalysis;

namespace Godot.Globalizer;

internal static class Diagnostics
{
    
    public static readonly DiagnosticDescriptor MustBePartial =
        new("GLOB001",
            "Class must be partial",
            "Class '{0}' marked with [GlobalizerWrap] must be declared partial",
            "Globalizer",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MustInheritGodotObject =
        new("GLOB002",
            "Class must inherit GodotObject",
            "Class '{0}' marked with [GlobalizerWrap] must inherit from Godot.GodotObject",
            "Globalizer",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MustBeNonGeneric =
        new("GLOB003",
            "Class must be non generic",
            "Class '{0}' marked with [GlobalizerWrap] cannot be generic",
            "Globalizer",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WrapperNameCollision =
        new("GLOB004",
            "Wrapper name collision",
            "Wrapper name '{0}' for class '{1}' already exists; wrapper generation skipped",
            "Globalizer",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WrapperNameSanitized =
        new("GLOB005",
            "Wrapper name fallback applied",
            "Provided wrapper name was empty or whitespace; using fallback name '{0}'",
            "Globalizer",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
}