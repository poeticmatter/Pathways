# Card Pathways — Project Instructions

## Stack

| Concern | Choice |
|---|---|
| Language | C# 13, .NET 9+ |
| Rendering & Input | Raylib-cs (NuGet) |
| Build | `dotnet run` / `dotnet build` — no editor, no content pipeline |
| Target platforms | Windows, macOS, Linux (itch.io); Steam if warranted |

**No other frameworks.** Do not add Unity, MonoGame, SFML, or any other game engine. Do not add web frameworks. Do not add ORMs or database libraries — this game has no persistence beyond in-memory state.

---

## DESIGN.md is the Law

`DESIGN.md` is the authoritative specification for all game rules, types, coordinate conventions, and module boundaries.

**When you change anything that DESIGN.md describes, you must update DESIGN.md in the same response.** This includes:
- Adding, removing, or renaming a type, enum value, or field
- Changing a game rule (turn order, win/loss condition, interaction logic)
- Changing coordinate conventions or sub-cell edge positions
- Adding a new module or moving logic between layers
- Adding a new mechanic or data format

If a change makes DESIGN.md out of sync with the code, the code is wrong, not the document — unless you are intentionally revising the design, in which case update both and note what changed in the change log at the bottom of DESIGN.md.

---

## Project Structure

```
/                       ← repo root
├── CardPathways.csproj
├── Program.cs          ← entry point, game loop
├── src/
│   ├── Data/           ← types, enums, CardDefinition, MapTile, JSON loading
│   ├── Logic/          ← pure functions: reachability, resolution, valid moves
│   ├── Game/           ← GameState, turn execution
│   └── Rendering/      ← all Raylib-cs calls, draw functions, input
├── assets/
│   └── data/           ← cards.json, tiles.json
├── CLAUDE.md
└── DESIGN.md
```

Layers may only depend on layers below them (see DESIGN.md §Modules). Any Raylib-cs `using` statement belongs exclusively in `src/Rendering/`.

---

## C# Coding Standards

### General

- Target `net9.0`. Use top-level statements in `Program.cs`.
- Enable nullable reference types (`<Nullable>enable</Nullable>`). Never suppress nullable warnings with `!` unless the nullability is structurally guaranteed and a comment explains why.
- No `dynamic`. No `object` used as a generic container.
- Prefer `record` and `record struct` for value objects and results. Prefer `readonly struct` for small hot-path types.

### Naming

- Types and methods: `PascalCase`
- Parameters, locals, fields: `camelCase`
- Private fields: `_camelCase` prefix
- Constants and static readonly: `PascalCase`
- Enums and enum members: `PascalCase`
- No Hungarian notation. No type suffixes like `cardData`, `tileObj`.

### Functions and Methods

- One responsibility per method. If a method needs "and" to describe what it does, split it.
- Pure functions in `Logic/` must have no side effects. They receive all inputs as parameters and return results — no reading from `GameState`, no static mutable state.
- Max ~4 parameters. Use a named record for option groups beyond that.
- Avoid `bool` parameters that switch behavior. Use two methods or an enum instead.

### State

- `GameState` is the single source of mutable game state. It lives in `Game/` and is owned by the game loop. Nothing else holds a reference to it between frames except the renderer, which receives a read-only view.
- Cards (`CardDefinition`) are immutable templates. Never mutate a `CardDefinition` — clone the grid for interaction resolution.
- Map tiles (`MapTile`) are mutable but only through explicit `GameState` mutations in `Game/`. No layer below `Game/` mutates a `MapTile` in place.

### Error Handling

- Invalid game states (e.g. playing on a non-adjacent cell) are programmer errors. Use `Debug.Assert` or `throw new InvalidOperationException(...)` with a descriptive message. Do not silently swallow them.
- JSON loading failures at startup should throw with a clear message identifying the file and field. The game cannot run without valid data.
- Do not use exceptions for control flow (e.g. detecting a blocked path). Return a value.

### Arrays and Collections

- Card and tile grids are `SubCell[,]` (2D arrays), indexed `[row, col]`.
- For sets of visited or reachable sub-cells, use `HashSet<SubCoord>`.
- Avoid LINQ in hot paths (called every frame). LINQ is fine for one-time initialization code.

### Comments

- Comments explain *why*, not *what*.
- Non-obvious invariants and constraints deserve a comment (e.g. "start card id=0 is never discarded").
- No block comments restating what the method signature already says.
- No TODO comments without a description of what is deferred and why.

---

## Raylib-cs Conventions

- Call `Raylib.InitWindow`, the game loop, and `Raylib.CloseWindow` only in `Program.cs`.
- All draw calls happen inside `Raylib.BeginDrawing()` / `Raylib.EndDrawing()`.
- Never call `Raylib.*` methods from `Logic/` or `Data/`.
- Input polling (`Raylib.IsMouseButtonPressed`, etc.) happens in `Rendering/` and produces input events or commands that are passed to `Game/` — `Rendering/` does not mutate `GameState` directly.
- Target 60 FPS (`Raylib.SetTargetFPS(60)`).
- All magic pixel values (cell size, padding, colors) are named constants in a `Layout` or `Theme` static class in `Rendering/`.

---

## Build & Run

```sh
# Run
dotnet run

# Build release
dotnet build -c Release

# Publish self-contained (Windows example)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

No content pipeline. Assets are embedded or loaded from relative paths at runtime. Paths are constructed with `Path.Combine` — never hardcoded separators.

---

## Publishing Targets

- **itch.io:** Publish self-contained binaries for Windows, macOS, Linux. Bundle assets alongside the executable.
- **Steam:** Greenworks or Steamworks.NET integration is out of scope until post-MVP validation. Do not design for it prematurely.

---

## What Not To Do

- Do not add Unity, Godot, MonoGame, or any game engine.
- Do not add a database, file persistence, or networking.
- Do not use `global using` except for the top-level Raylib namespace if it genuinely reduces noise.
- Do not use source generators or reflection for game logic.
- Do not over-abstract. A helper that is only called once does not need to exist.
- Do not write speculative features. MVP scope is defined in DESIGN.md §Out of Scope.
