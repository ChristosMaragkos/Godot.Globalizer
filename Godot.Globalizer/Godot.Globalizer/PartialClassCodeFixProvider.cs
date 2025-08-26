using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Godot.Globalizer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PartialClassCodeFixProvider))]
[Shared]
public sealed class PartialClassCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("GLOB001");

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(diagnostic.Location.SourceSpan) is not ClassDeclarationSyntax node) return;
        if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))) return; // already fixed

        context.RegisterCodeFix(
            CodeAction.Create(
                "Add partial modifier",
                ct => AddPartialAsync(context.Document, node, ct),
                "AddPartialModifier"),
            diagnostic);
    }

    private static async Task<Document> AddPartialAsync(Document document, ClassDeclarationSyntax classDecl,
        CancellationToken ct)
    {
        var newDecl = classDecl.WithModifiers(classDecl.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword)));
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root is null) return document;
        var newRoot = root.ReplaceNode(classDecl, newDecl);
        return document.WithSyntaxRoot(newRoot);
    }
}