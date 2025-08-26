## Unshipped

### New Rules

| Rule ID | Category   | Severity | Description                    | Notes                                            |
|---------|------------|----------|--------------------------------|--------------------------------------------------|
| GLOB001 | Globalizer | Error    | Class must be partial          | Add 'partial' modifier                           |
| GLOB002 | Globalizer | Error    | Class must inherit GodotObject | Inherit from Godot.GodotObject (direct/indirect) |
| GLOB003 | Globalizer | Error    | Class must be non generic      | Remove generic parameters                        |
| GLOB004 | Globalizer | Warning  | Wrapper name collision         | Rename class or wrapper                          |
| GLOB005 | Globalizer | Info     | Wrapper name fallback applied  | Provided name empty/whitespace                   |

