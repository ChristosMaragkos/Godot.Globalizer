using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Godot.Globalizer;

[Generator]
public sealed class GlobalWrapperGenerator : IIncrementalGenerator
{
    private const string AttributeName = "GlobalizerWrap";
    private const string AttributeFullName = "Godot.Globalizer.Attributes.GlobalizerWrapAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
            static (n, _) => n is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
            static (ctx, _) =>
            {
                var cds = (ClassDeclarationSyntax)ctx.Node;
                return cds.AttributeLists.Any(l => l.Attributes.Any(a => a.Name.ToString().Contains(AttributeName)))
                    ? cds
                    : null;
            }).Where(static c => c is not null);

        var combo = context.CompilationProvider.Combine(candidates.Collect());

        context.RegisterSourceOutput(combo,
            static (spc, pair) =>
            {
                var (compilation, list) = pair;
                if (list.Length == 0) return;

                var attrSymbol = compilation.GetTypeByMetadataName(AttributeFullName);
                var nodeSymbol = compilation.GetTypeByMetadataName("Godot.Node");
                var godotObjectSymbol = compilation.GetTypeByMetadataName("Godot.GodotObject");
                var globalClassAttr = compilation.GetTypeByMetadataName("Godot.GlobalClassAttribute");

                foreach (var cds in list)
                {
                    spc.CancellationToken.ThrowIfCancellationRequested();
                    Generate(spc, compilation, cds!, attrSymbol, nodeSymbol, godotObjectSymbol, globalClassAttr);
                }
            });
    }

    private static void Generate(SourceProductionContext ctx,
        Compilation compilation,
        ClassDeclarationSyntax cds,
        INamedTypeSymbol? attrSymbol,
        INamedTypeSymbol? nodeSymbol,
        INamedTypeSymbol? godotObjectSymbol,
        INamedTypeSymbol? globalClassAttr)
    {
        if (attrSymbol is null) return;
        var model = compilation.GetSemanticModel(cds.SyntaxTree);
        if (model.GetDeclaredSymbol(cds) is not INamedTypeSymbol sym) return;

        var attrData = sym.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSymbol));
        if (attrData is null) return; // Not actually annotated (name match false positive)

        // Enforce: class must be partial
        if (!cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.MustBePartial, cds.Identifier.GetLocation(), sym.Name));
            return;
        }

        // Enforce: class must be non-generic
        if (sym.IsGenericType)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.MustBeNonGeneric, cds.Identifier.GetLocation(), sym.Name));
            return;
        }

        // Enforce: must inherit from Godot.GodotObject (directly or indirectly)
        if (godotObjectSymbol is not null && !DerivesOrEquals(sym, godotObjectSymbol))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.MustInheritGodotObject, cds.Identifier.GetLocation(), sym.Name));
            return;
        }

        // Accept only classes that (optionally) derive from Node (for wrapper emission). If not Node-derived but still GodotObject-derived, we can still wrap? Keep existing Node constraint.
        if (nodeSymbol is not null && !DerivesOrEquals(sym, nodeSymbol))
        {
            // Currently we choose not to emit a wrapper for non-Node GodotObjects.
            return;
        }

        string? custom = null;
        var providedNameValid = false;
        if (attrData.ConstructorArguments.Length == 1 && attrData.ConstructorArguments[0].Value is string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                custom = s.Trim();
                providedNameValid = true;
            }
        }

        var wrapperName = custom ?? sym.Name + "Global";
        if (!providedNameValid && attrData.ConstructorArguments.Length == 1)
        {
            // Provided but empty/whitespace -> sanitized fallback
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.WrapperNameSanitized, cds.Identifier.GetLocation(), wrapperName));
        }

        // Name collision check within namespace
        if (sym.ContainingNamespace.GetTypeMembers(wrapperName).Any())
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.WrapperNameCollision, cds.Identifier.GetLocation(), wrapperName, sym.Name));
            return;
        }

        var ns = sym.ContainingNamespace.IsGlobalNamespace ? null : sym.ContainingNamespace.ToDisplayString();
        var baseType = sym.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Godot;");
        if (ns is not null) sb.Append("namespace ").Append(ns).AppendLine(";").AppendLine();
        if (globalClassAttr is not null) sb.AppendLine("[GlobalClass]");
        sb.Append("public partial class ").Append(wrapperName).Append(" : ").Append(baseType).AppendLine();
        sb.AppendLine("{");
        sb.AppendLine();
        sb.AppendLine("}");
        ctx.AddSource(wrapperName + ".g.cs", sb.ToString());
    }

    private static bool DerivesOrEquals(INamedTypeSymbol t, INamedTypeSymbol baseType)
    {
        if (SymbolEqualityComparer.Default.Equals(t, baseType)) return true;
        for (var cur = t.BaseType; cur is not null; cur = cur.BaseType)
            if (SymbolEqualityComparer.Default.Equals(cur, baseType)) return true;
        return false;
    }
}