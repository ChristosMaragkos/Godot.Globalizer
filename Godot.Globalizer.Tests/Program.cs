using System.Reflection;

// Runtime assertion harness for GlobalWrapperGenerator

var asm = Assembly.GetExecutingAssembly();

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

Assert(HasGlobalClass(myThingWrapper!), "MyThingGlobal has [GlobalClass]");
Assert(HasGlobalClass(customWrapper!), "CustomWrapper has [GlobalClass]");
Assert(HasGlobalClass(blankNameWrapper!), "BlankNameGlobal has [GlobalClass]");
Assert(HasGlobalClass(whiteSpaceNameWrapper!), "WhiteSpaceNameGlobal has [GlobalClass]");
Assert(!HasGlobalClass(anotherThingWrapper), "AnotherThingGlobal does NOT have [GlobalClass] (manual, skipped generation)");

Assert(myThingWrapper!.IsSubclassOf(myThing!), "MyThingGlobal derives from MyThing");
Assert(customWrapper!.IsSubclassOf(customBase!), "CustomWrapper derives from CustomBase");
Assert(blankNameWrapper!.IsSubclassOf(blankName!), "BlankNameGlobal derives from BlankName");
Assert(whiteSpaceNameWrapper!.IsSubclassOf(whiteSpaceName!), "WhiteSpaceNameGlobal derives from WhiteSpaceName");

Console.WriteLine();
if (Environment.ExitCode == 0)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("All generator assertions passed.");
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("One or more generator assertions failed.");
}

Console.ResetColor();

return;

Type? GetType(string name) => asm.GetTypes().FirstOrDefault(t => t.FullName == name);

bool HasGlobalClass(Type? t) => t != null && t.GetCustomAttributes().Any(a => a.GetType() == globalClassAttrType);

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
