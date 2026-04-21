# Card Pathways — Architecture & Design Rules

**Version:** 1.0 | **Stack:** C# / .NET 9+ / Raylib-cs

> This document is the single source of truth for game rules and architectural decisions.
> It must be kept in sync with the code. When a rule or structure changes, update this file in the same commit.

---

## Game Overview

Card Pathways is a solo spatial puzzle. The player navigates a 5×5 map by playing cards from a hand onto adjacent cells. The goal is to reach the exit at the top-right from the entrance at the bottom-left.

Exactly one card is physically present on the map at all times. All other cells hold only their remembered tile state.

| Property | Value |
|---|---|
| Map | 5×5 grid of cells |
| Deck | 10 fixed-design cards, shuffled at start |
| Card grid | 9×9 sub-cells (3×3 logical sub-cells, each 3 px wide in grid space) |
| Map tile grid | 3×3 sub-cells, displayed through the card's center hole |
| Cards on map | Exactly 1 at all times |

---

## Coordinate System

Map coordinates are `(Col, Row)`, 0-indexed, **origin at top-left**.

```
Col →  0    1    2    3    4
Row
 0                          ← EXIT (4,0)
 1
 2          (2,2) = RESHUFFLE
 3
 4  ← ENTRY (0,4)
```

- Entry cell: `(0, 4)` — player enters from the **left** edge
- Exit cell: `(4, 0)` — player wins by reaching this cell with a path exiting **right** or **top**
- Reshuffle cell: `(2, 2)` — map tile at center triggers a reshuffle when reached

Sub-cell coordinates within a card or tile are `(subCol, subRow)`, 0-indexed, origin top-left.
The entry/exit edge midpoints on a 9×9 card grid are:
- Left: `(0, 4)` | Right: `(8, 4)` | Top: `(4, 0)` | Bottom: `(4, 8)`

---

## Core Types

These are the canonical type definitions. Code must match these exactly.

```csharp
enum SubCell { Blocked, Passable, Hole, Door, Key, Shuffle }

enum Direction { Top, Right, Bottom, Left }

record struct MapCoord(int Col, int Row);
record struct SubCoord(int Col, int Row);  // within a 9×9 or 3×3 grid

// A card is a fixed 9×9 design. Cards are templates — they carry no mutable state.
class CardDefinition
{
    int Id;
    int HandSize;
    SubCell[,] Grid;  // [row, col], 9×9
}

// A map tile holds the persistent mutable state for one cell.
// State is stored here, never on cards.
class MapTile
{
    int DefinitionId;
    SubCell[,] Grid;  // [row, col], 3×3 — mutated as doors open, keys used
}

// Snapshot of a card placed on a tile, after all interactions are resolved.
// Produced by GameLogic.ResolvePlacement — never mutated after creation.
record PlacementResult(
    MapTile ModifiedTile,
    SubCell[,] ModifiedCardGrid,  // 9×9, keys/doors consumed
    HashSet<SubCoord> Reachable,
    bool TriggeredReshuffle
);

enum GameStatus { Playing, Won, Lost }

class GameState
{
    MapTile[,] Map;              // [row, col], 5×5
    List<CardDefinition> Deck;
    List<CardDefinition> Hand;
    List<CardDefinition> Discard;
    MapCoord CurrentCell;
    Direction EntryEdge;
    SubCell[,] ActiveCardGrid;   // 9×9 — current card after interaction
    HashSet<SubCoord> Reachable;
    GameStatus Status;
}
```

---

## Composite Grid

The composite grid is a 9×9 array used for reachability computation only — it is never stored.

Construction:
1. Start with the active card's 9×9 grid.
2. Overwrite cells `[row+3, col+3]` for row/col in 0..2 with the map tile's 3×3 grid.

The center 3×3 region (`rows 3–5, cols 3–5`) always reflects map tile state. Card state fills the surrounding 6×3 border area.

---

## Reachability Algorithm

Input: composite 9×9 grid, entry `Direction`.  
Output: `HashSet<SubCoord>` of reachable sub-cells.

Entry point by direction:
- Left → `(0, 4)` | Right → `(8, 4)` | Top → `(4, 0)` | Bottom → `(4, 8)`

BFS rules:
- A cell is walkable if its value is `Passable`, `Key`, or `Shuffle`.
- `Door` cells are collected but **not** traversed; they mark blocked thresholds.
- `Blocked` and `Hole` cells stop traversal.
- If the entry point itself is not walkable, the result is an empty set.

---

## Interaction Resolution

After a card is placed, interactions are resolved in a fixpoint loop:

```
repeat:
    composite = BuildCompositeGrid(tile, cardGrid)
    result    = ComputeReachability(composite, entryEdge)
    
    if result.Keys.Count > 0 AND result.Doors.Count > 0:
        consume min(keys, doors) pairs — set each sub-cell to Passable
        (update tile or cardGrid depending on which grid the sub-cell belongs to)
        changed = true
    
    if result.Shuffles contains any cell still == Shuffle:
        set that sub-cell to Passable in whichever grid owns it
        didShuffle = true
        changed = true
until not changed
```

A sub-cell belongs to the tile if `subRow` ∈ [3,5] and `subCol` ∈ [3,5]; otherwise it belongs to the card.

Key-door pairing is positional: pair `keys[0]` with `doors[0]`, etc. Order within the BFS result is insertion order (queue order).

---

## Turn Structure

Each turn executes these steps exactly, in order:

1. **Play card** — player selects a card from hand and a valid adjacent cell.
2. **Resolve placement** — call `ResolvePlacement(tile, card, entryEdge)` → `PlacementResult`.
3. **Update map** — write `PlacementResult.ModifiedTile` into `Map[row, col]`.
4. **Discard old card** — add the previously active card to `Discard` (skip if it is the start card, id = 0).
5. **Apply reshuffle** — if `PlacementResult.TriggeredReshuffle`, shuffle `Discard` into `Deck`, clear `Discard`.
6. **Normalize hand** — remove played card from hand, then draw from `Deck` until `Hand.Count == newCard.HandSize`. If `Hand.Count` exceeds `HandSize`, discard the excess.
7. **Check win** — if new cell is `(4, 0)` and `Reachable` contains `(4, 0)` (top exit) or `(8, 4)` (right exit) → `Status = Won`.
8. **Check loss** — if `Hand.Count == 0` and `Deck.Count == 0` → `Status = Lost`.

**Hand invariant:** After every turn, `Hand.Count == ActiveCard.HandSize`, unless the deck ran dry.

---

## Valid Move Rules

A cell `(targetCol, targetRow)` is a valid destination for a selected card if all of:
1. It is orthogonally adjacent to `CurrentCell`.
2. The current card's reachable set includes the exit sub-cell facing `targetCell`.
3. The candidate card's grid has `Passable` at the entry sub-cell facing back toward `CurrentCell`.
4. The target cell is within map bounds (0–4 for both axes).

Exit sub-cells (on the current 9×9 card) facing each direction:
- Right → `(8, 4)` | Left → `(0, 4)` | Up (row-1) → `(4, 0)` | Down (row+1) → `(4, 8)`

Entry sub-cells (on the candidate card) are the mirror:
- Moving right → candidate must have `Passable` at `(0, 4)` (left edge)
- Moving left → `(8, 4)` | Moving up → `(4, 8)` | Moving down → `(4, 0)`

---

## Card Data

Cards are fixed templates loaded from JSON at startup. Card grids are 9×9 with the center 3×3 set to `Hole`. They carry no mutable state — mutations during interaction resolution are applied to a local copy, never to the definition.

The **start card** (id 0) is a special template placed at `(0, 4)` at game start. It is never shuffled into the deck and never discarded (skip step 4 when the id is 0).

---

## Map Tile Data

Map tiles are 3×3 grids. At game start:
- 24 standard tiles are shuffled and assigned to all cells except `(2, 2)`.
- The reshuffle tile (the one with a `Shuffle` sub-cell at its center) is always placed at `(2, 2)`.

Each tile's mutable state is a deep copy — tiles share no data between cells.

---

## Modules

The codebase is organized into these layers. Each layer may only depend on layers below it.

```
Rendering   (Raylib-cs calls, input, draw calls)
    ↓
Game        (GameState, turn execution, input dispatch)
    ↓
Logic       (pure functions: ReachabilityMap, ResolvePlacement, ValidMoves)
    ↓
Data        (CardDefinition, MapTile, enums — no behavior)
```

**Rules:**
- `Logic` has zero Raylib dependencies and zero side effects. All functions are pure and testable in isolation.
- `Game` owns the single mutable `GameState`. No other layer mutates it.
- `Rendering` reads `GameState` (read-only view) and produces draw calls. It does not modify game state.
- `Data` defines types and loads/parses static JSON definitions. No game logic here.

---

## Rendering Conventions

- Window resolution: 1280×720 (16:9), resizable with layout reflow.
- Map panel: left side. Hand panel: right side.
- Map cells scale uniformly; sub-cells render as small squares.
- Current cell highlights the composite grid with reachability tint.
- Non-current cells show only their map tile's center 3×3 region.
- Valid move targets are highlighted when a card is selected.
- Colors and visual style are thematic (dark background, glowing paths).

---

## Out of Scope (MVP)

- Card rotation
- Multiple players
- Scoring / move counter
- Traversal-based rules
- Card special abilities beyond door/key/shuffle
- Procedural map/card generation
- Undo / redo
- Visible discard pile
- Timer
- Networking
- Save/load

---

## Change Log

| Date | Change | Author |
|---|---|---|
| 2026-04-21 | Initial architecture document | Aurore |
