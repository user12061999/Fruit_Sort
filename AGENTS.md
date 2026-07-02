# LoopSort — Agent Guide

This is a Unity 2D project. Keep startup context small: do not preload the full ECC catalog or recursively scan `Assets/`.

## Start Here

- For a small task, inspect only the named object/file and its direct dependencies.
- Before a broad search, architecture change, or unfamiliar subsystem, consult `Docs/PROJECT_MAP.md`.
- Use `rg`/`rg --files` with a narrow path such as `Assets/_Game/Script` or `Assets/_Game/Prefabs`; avoid listing all sprites and generated files.
- Check `git status --short` before editing. Preserve all unrelated user changes and recovery files.

## Project Boundaries

- Gameplay source of truth: `Assets/_Game/Script/`.
- Main scene: `Assets/_Game/Scenes/SampleScene.unity`.
- Reusable gameplay prefabs: `Assets/_Game/Prefabs/`.
- Fruit configuration: `Assets/_Game/Data/` (`FruitData` and `FruitDatabase`).
- Do not edit generated folders: `Library/`, `Temp/`, `Logs/`, `obj/`, or generated `.csproj`/`.sln` files.
- Do not modify imported art, materials, prefabs, or scenes unless the task requires it.

## Unity Workflow

- Prefer Unity MCP for scene, prefab, component, console, and test operations when it is available.
- Confirm the active Unity instance is `LoopSort` before changing Editor state.
- After editing C# scripts, wait for compilation and check the Unity Console for errors.
- Run the smallest relevant EditMode test set first. Add focused tests for behavior changes where practical.
- Never claim a test or coverage result that was not actually run.

## Coding Conventions

- Runtime gameplay code normally belongs to namespace `FruitSort`; keep Editor-only code under `Assets/_Game/Script/Editor/`.
- Prefer focused changes and existing project patterns over new abstractions or dependencies.
- Keep serialized Inspector fields clear with useful headers/tooltips; validate boundary values in `OnValidate` when appropriate.
- Avoid per-frame allocations in dot/conveyor hot paths. `FallingPixelManager` intentionally uses transform-based simulation rather than Rigidbody physics.
- Preserve the shared `colorId` contract across `FruitData`, `Dot`, spawners, grids, and buckets.
- Handle null Unity references and disabled/destroyed objects explicitly.

## Skills and Agents

- Load only the skill directly relevant to the current task; do not read every ECC skill or generic reference file.
- Use subagents only when the user requests delegation/parallel work or the task clearly needs independent parallel investigation.
- Detailed ECC material in `.agents/` is a library, not mandatory startup context.

## Documentation Routes

- Architecture and file routing: `Docs/PROJECT_MAP.md`.
- Gameplay setup notes: `Assets/_Game/SETUP_GUIDE.md` (some scene names may be legacy; verify against the map).
- Conveyor editor specification: `Docs/ConveyorEditor_Spec.md`.
- Sprite grid-fill specification: `Assets/_Game/mdfile/unity-sprite-grid-fill-spec.md`.

## Completion

- Review the final diff and Unity Console.
- Report files changed and verification performed.
- Do not commit, push, or change external resources unless the user explicitly asks.
