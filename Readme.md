# Godot.Globalizer

Source generator that produces `[GlobalClass]` Godot wrapper classes for your custom `Godot.Node` subclasses annotated with `[GlobalizerWrap]`.

## Packages
- `Godot.Globalizer` (analyzer / source generator)
- `Godot.Globalizer.Abstractions` (only the attribute, pulled transitively when you add the generator)

You normally only install `Godot.Globalizer`.

## What It Does
For any partial, non-generic class that inherits from GodotObject:
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

## Usage Checklist
1. Create your class library, adding classes that you'd like to expose as global normally.
2. Mark your external classes as `partial` and add `[GlobalizerWrap(null)]` or a specific name.
3. Build the project. Generated wrappers appear under Analyzers → Generated Files.
4. Use the generated wrapper (e.g. `EnemyGlobal`) where a globally registered class is desired.

## Attribute Reference
```csharp
[GlobalizerWrap(string? wrapperName)]
// wrapperName: null/empty/whitespace -> OriginalNameGlobal
```

## License
MIT
