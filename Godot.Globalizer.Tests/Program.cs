using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis; // added
using Microsoft.CodeAnalysis.CSharp; // added
using Microsoft.CodeAnalysis.CodeFixes; // added
using Microsoft.CodeAnalysis.Text; // added
using Microsoft.CodeAnalysis.CodeActions; // for operations
using Microsoft.CodeAnalysis.Editing; // potential future use
using Microsoft.CodeAnalysis.CSharp.Syntax; // for ClassDeclarationSyntax

// Runtime assertion harness for GlobalWrapperGenerator plus code fix validation

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("FAIL: " + message);
        Console.ResetColor();
        Environment.ExitCode = 1;
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("PASS: " + message);
        Console.ResetColor();
    }
}

var asm = Assembly.GetExecutingAssembly();

Type? GetType(string name) => asm.GetTypes().FirstOrDefault(t => t.FullName == name);

var myThing = GetType("Samples.MyThing");
var myThingWrapper = GetType("Samples.MyThingGlobal");
var customBase = GetType("Samples.CustomBase");
var customWrapper = GetType("Samples.CustomWrapper");
var anotherThing = GetType("Samples.AnotherThing");
var anotherThingWrapper = GetType("Samples.AnotherThingGlobal");
var plain = GetType("Samples.Plain");
var plainWrapper = GetType("Samples.PlainGlobal");
var blankName = GetType("Samples.BlankName");
var blankNameWrapper = GetType("Samples.BlankNameGlobal");
var whiteSpaceName = GetType("Samples.WhiteSpaceName");
var whiteSpaceNameWrapper = GetType("Samples.WhiteSpaceNameGlobal");
var genericThing = GetType("Samples.GenericThing`1");
var genericThingWrapper = GetType("Samples.GenericThingGlobal");

Assert(myThing != null, "MyThing exists");
Assert(customBase != null, "CustomBase exists");
Assert(anotherThing != null, "AnotherThing exists");
Assert(plain != null, "Plain exists");
Assert(blankName != null, "BlankName exists");
Assert(whiteSpaceName != null, "WhiteSpaceName exists");
Assert(genericThing != null, "GenericThing<T> exists");

// Wrapper existence expectations
Assert(myThingWrapper != null, "MyThingGlobal wrapper generated");
Assert(customWrapper != null, "CustomWrapper wrapper generated (custom name)");
Assert(blankNameWrapper != null, "BlankNameGlobal wrapper generated (empty string argument fallback)");
Assert(whiteSpaceNameWrapper != null, "WhiteSpaceNameGlobal wrapper generated (whitespace argument fallback)");
// AnotherThingGlobal exists but is manual and should NOT have GlobalClass attribute
Assert(anotherThingWrapper != null, "AnotherThingGlobal manual wrapper present (should not be generated)");
Assert(plainWrapper == null, "PlainGlobal wrapper NOT generated for non-Node type");
Assert(genericThingWrapper == null, "GenericThingGlobal wrapper NOT generated for generic type");

// Attribute + inheritance checks
var globalClassAttrType = asm.GetTypes().FirstOrDefault(t => t.Name == "GlobalClassAttribute");
Assert(globalClassAttrType != null, "GlobalClassAttribute stub present");

bool HasGlobalClass(Type? t) => t != null && t.GetCustomAttributes().Any(a => a.GetType() == globalClassAttrType);

Assert(HasGlobalClass(myThingWrapper!), "MyThingGlobal has [GlobalClass]");
Assert(HasGlobalClass(customWrapper!), "CustomWrapper has [GlobalClass]");
Assert(HasGlobalClass(blankNameWrapper!), "BlankNameGlobal has [GlobalClass]");
Assert(HasGlobalClass(whiteSpaceNameWrapper!), "WhiteSpaceNameGlobal has [GlobalClass]");
Assert(!HasGlobalClass(anotherThingWrapper), "AnotherThingGlobal does NOT have [GlobalClass] (manual, skipped generation)");

Assert(myThingWrapper!.IsSubclassOf(myThing!), "MyThingGlobal derives from MyThing");
Assert(customWrapper!.IsSubclassOf(customBase!), "CustomWrapper derives from CustomBase");
Assert(blankNameWrapper!.IsSubclassOf(blankName!), "BlankNameGlobal derives from BlankName");
Assert(whiteSpaceNameWrapper!.IsSubclassOf(whiteSpaceName!), "WhiteSpaceNameGlobal derives from WhiteSpaceName");

// Code fix test for GLOB001 (add partial)
try
{
    var code = @"using Godot;\nusing Godot.Globalizer.Attributes;\n[GlobalizerWrap] public class NeedsFix : Node { }";
    var tree = CSharpSyntaxTree.ParseText(code);
    var root = tree.GetRoot();
    var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(n => n.Identifier.Text == "NeedsFix");
    var descriptor = new DiagnosticDescriptor("GLOB001", "Class must be partial", "stub", "Globalizer", DiagnosticSeverity.Error, true);
    var diagnostic = Diagnostic.Create(descriptor, classNode.Identifier.GetLocation());
    var workspace = new AdhocWorkspace();
    var proj = workspace.AddProject("CodeFixProj", LanguageNames.CSharp)
        .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
        .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview));
    proj = proj.AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
    proj = proj.AddDocument("NeedsFix.cs", code).Project;
    var document = proj.Documents.First();
    var provider = new Godot.Globalizer.PartialClassCodeFixProvider();
    var actions = new System.Collections.Generic.List<CodeAction>();
    var context = new CodeFixContext(document, diagnostic, (a, d) => actions.Add(a), default);
    provider.RegisterCodeFixesAsync(context).GetAwaiter().GetResult();
    Assert(actions.Count > 0, "Code fix registered for GLOB001");
    var ops = actions[0].GetOperationsAsync(default).GetAwaiter().GetResult();
    foreach (var op in ops)
        op.Apply(workspace, default);
    var newDoc = workspace.CurrentSolution.Projects.First().Documents.First();
    var newText = newDoc.GetTextAsync().GetAwaiter().GetResult().ToString();
    Assert(newText.Contains("partial class NeedsFix"), "Code fix added partial modifier");
}
catch (Exception ex)
{
    Assert(false, "Code fix test failed: " + ex.GetType().Name + ": " + ex.Message);
}

// --- Generator diagnostic tests ---
try
{
    var badSources = @"using Godot; using Godot.Globalizer.Attributes;\npublic class GodotObject { }\npublic class Node : GodotObject { }\n[GlobalizerWrap] class BadNotPartial : Node { }\n[GlobalizerWrap] public partial class BadGeneric<T> : Node { }\n[GlobalizerWrap] public partial class BadNoInheritance { }\n[GlobalizerWrap] public partial class GoodOne : Node { }";
    var syntaxTree = CSharpSyntaxTree.ParseText(badSources, new CSharpParseOptions(LanguageVersion.Preview));
    var refs = new[]
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Godot.Globalizer.Attributes.GlobalizerWrapAttribute).Assembly.Location)
    };
    var compilation = CSharpCompilation.Create("DiagTest", new[] { syntaxTree }, refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    var generator = new Godot.Globalizer.GlobalWrapperGenerator();
    CSharpGeneratorDriver.Create(generator).RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var diagnostics);
    var allDiags = diagnostics.Concat(updated.GetDiagnostics()).ToArray();

    bool Has(string id, string nameFragment) => allDiags.Any(d => d.Id == id && d.GetMessage().Contains(nameFragment));

    Assert(Has("GLOB001", "BadNotPartial"), "GLOB001 reported for non-partial class");
    Assert(Has("GLOB003", "BadGeneric"), "GLOB003 reported for generic class");
    Assert(Has("GLOB002", "BadNoInheritance"), "GLOB002 reported for non-GodotObject class");
    Assert(!Has("GLOB001", "GoodOne"), "No GLOB001 for valid partial class");
}
catch (Exception ex)
{
    Assert(false, "Generator diagnostic test failed: " + ex.GetType().Name + ": " + ex.Message);
}

Console.WriteLine();
if (Environment.ExitCode == 0)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("All generator + code fix assertions passed.");
    Console.ResetColor();
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("One or more assertions failed.");
    Console.ResetColor();
}
