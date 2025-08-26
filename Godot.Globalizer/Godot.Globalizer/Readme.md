# Godot.Globalizer

Source generator that produces `[GlobalClass]` Godot wrapper classes for your custom `Godot.Node` subclasses annotated with `[GlobalizerWrap]`.

## Packages
- `Godot.Globalizer` (analyzer / source generator)
- `Godot.Globalizer.Abstractions` (only the attribute, pulled transitively when you add the generator)

You normally only install `Godot.Globalizer`.

## What It Does
For any partial class:
```csharp
using Godot;
using Godot.Globalizer.Attributes;

[GlobalizerWrap(null)]
public partial class Enemy : Node { }
```
Generator adds (conceptually):
```csharp
[GlobalClass]
public partial class EnemyGlobal : Enemy {
    public static EnemyGlobal Create() => new();
}
```
If you provide a non‑empty name: `[GlobalizerWrap("EnemyRuntime")]` it generates that name instead of `EnemyGlobal`.

Skips generation when:
- Class is generic
- Class does not derive (directly or indirectly) from `Godot.Node`
- A type with the intended wrapper name already exists in the same namespace

Empty or whitespace custom wrapper names fall back to `OriginalNameGlobal`.

## Installation
NuGet (after published):
```
dotnet add package Godot.Globalizer
```
No extra props needed; analyzers auto-load.

Local test (before publishing):
```
dotnet build -c Release
# Find the .nupkg under Godot.Globalizer/bin/Release/
# Then: (example)
dotnet nuget add source ./local-packages -n Local
copy .\Godot.Globalizer\Godot.Globalizer\bin\Release\Godot.Globalizer.*.nupkg .\local-packages\
# In consuming project:
dotnet nuget add source ./local-packages -n Local (if not already)
dotnet add package Godot.Globalizer -v 0.1.0
```

## Usage Checklist
1. Ensure your Godot C# project references the Godot assemblies (normal Godot .NET setup).
2. Mark your base game classes as `partial` and add `[GlobalizerWrap(null)]` or a specific name.
3. Build the project. Generated wrappers appear under Analyzers → Generated Files.
4. Use the generated wrapper (e.g. `EnemyGlobal`) where a globally registered class is desired.

## Attribute Reference
```csharp
[GlobalizerWrap(string? wrapperName)]
// wrapperName: null/empty/whitespace -> OriginalNameGlobal
```

## Versioning
`0.1.x` – experimental, shape may evolve. Pin exact version if stability is critical.

## Development
- Build tests project (`Godot.Globalizer.Tests`) to validate generator behavior.
- Packages are produced automatically (`GeneratePackageOnBuild=true`).
- Separate Abstractions project keeps runtime footprint minimal if only the attribute is needed.

## Roadmap Ideas
- Option to emit additional factory helpers
- Support for filtering by accessibility
- Diagnostic reporting (why a class was skipped)

## License
MIT
