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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonGenericGlobalClassCodeFixProvider))]
[Shared]
public sealed class NonGenericGlobalClassCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("GLOB003");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root?.FindNode(diagnostic.Location.SourceSpan) is not ClassDeclarationSyntax classDecl) return;
        if (classDecl.TypeParameterList is null) return; // already non-generic

        context.RegisterCodeFix(
            CodeAction.Create(
                "Remove generic type parameters",
                ct => RemoveGenericsAsync(context.Document, classDecl, ct),
                equivalenceKey: "RemoveGenericParameters"),
            diagnostic);
    }

    private static async Task<Document> RemoveGenericsAsync(Document document, ClassDeclarationSyntax classDecl, CancellationToken ct)
    {
        var newDecl = classDecl
            .WithTypeParameterList(null)
            .WithConstraintClauses([]);

        //Optionally could add a comment hint.
        newDecl = newDecl.WithLeadingTrivia(newDecl.GetLeadingTrivia()
            .Add(SyntaxFactory.Comment("// TODO: Replace type parameter usages manually")));

        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root is null) return document;
        var newRoot = root.ReplaceNode(classDecl, newDecl);
        return document.WithSyntaxRoot(newRoot);
    }
}