# Blast Game — API Documentation

Welcome to the API documentation for the Blast Game, built for the Dream Games Software Engineering Case Study.

## Architecture

The codebase is organized into three layers:

- **Data** — Pure DTOs and enums (Coordinate, BlastResult, TurnResult, etc.)
- **Logic** — Pure C# game rules, fully NUnit testable (GameSession, Board, RocketProcessor, etc.)
- **View** — Unity MonoBehaviours for rendering and animation (not in API docs — requires Unity runtime)

## Key Entry Points

| Class | Purpose |
|-------|---------|
| `GameSession` | Pure C# state machine for one level. `ProcessTap(coord)` → `TurnResult` |
| `TurnResult` | The single contract between Logic and View layers |
| `Board` | Grid state container |
| `RocketProcessor` | Rocket creation, explosion, and combo logic |
| `DamageResolver` | Single source of truth for all damage rules |

## Additional Documents

- [Requirements Traceability](docs/REQUIREMENTS.md)
- [Architecture Decision Records](docs/ADR.md)