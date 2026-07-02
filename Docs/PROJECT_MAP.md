# LoopSort Project Map

Use this map to route work without scanning the entire Unity project. Verify the relevant file before editing because scenes and prefabs can evolve independently of this document.

## Project Snapshot

| Item | Current location/value |
|---|---|
| Unity version | `6000.4.12f1` |
| Render pipeline | Universal Render Pipeline 17.4 |
| Input | Unity Input System 1.19 (`Mouse.current`) |
| Conveyor geometry | Unity Splines 2.8.4 |
| Tests | Unity Test Framework 1.6, EditMode tests under `Assets/_Game/Script/Editor/` |
| Build scene | `Assets/_Game/Scenes/SampleScene.unity` |
| Gameplay namespace | `FruitSort` (except the standalone `SpriteGridFill`) |

DOTween is supplied under `Assets/Plugins/Demigiant/`. Unity MCP is configured as a local package in `Packages/manifest.json`.

## Runtime Flow

```text
PixelGridManager + Shooter        ModelDotSpawner + ModelDotSpawnerColumn
             \                                  /
              +-------------> Dot <------------+
                                |
                                v
                     FallingPixelManager
                       |              |
                       v              v
              ConveyorSpline --> ConveyorConnections
                                |
                                v
                              Bucket
                         /              \
                  GameManager       BucketWorker
```

- `colorId` is the shared identity across fruit data, dots, spawners, grids, and buckets.
- A `Dot` moves through `InGrid`, `Falling`, `OnBelt`, `Attracting`, or `Launched` states.
- `FallingPixelManager` owns runtime dot motion, conveyor transitions, separation, and bucket detection without Rigidbody simulation.
- `Bucket` reserves matching dots, animates them into grid cells, updates fill visuals, supports releasing contents, and raises `OnBucketFull`.
- `BucketWorker` listens for full buckets and carries them from its idle point to the drop zone.

## File Routing

### Fruit identity and visuals

| File | Responsibility |
|---|---|
| `Assets/_Game/Script/FruitData.cs` | ScriptableObject for one fruit: `colorId`, display name, color, main sprite, and dot sprite. |
| `Assets/_Game/Script/FruitDatabase.cs` | Collection and lookup of `FruitData` by `colorId`; warns about duplicate IDs. |
| `Assets/_Game/Data/FruitDatabase.asset` | Project fruit database asset. |
| `Assets/_Game/Data/FruitData_New*.asset` | Individual fruit configurations. |
| `Assets/_Game/Script/SpriteGridFill.cs` | Material-property-block controller for grid dimensions, cell gaps, fill amount, and cell positions. |
| `Assets/_Game/Shader/SpriteGridFill.shader` | Shader used by grid-fill fruit/package visuals. |

### Dot creation and interaction

| File | Responsibility |
|---|---|
| `Assets/_Game/Script/Dot.cs` | Dot identity, health, sprite/color application, and runtime movement state. |
| `Assets/_Game/Script/PixelGridManager.cs` | Builds the destructible dot grid and transfers destroyed dots to the falling manager. |
| `Assets/_Game/Script/Shooter.cs` | Mouse-driven hitscan damage against grid dots. |
| `Assets/_Game/Script/ModelDotSpawner.cs` | Clickable fruit package that emits dots and consumes a grid-filled package sprite. |
| `Assets/_Game/Script/ModelDotSpawnerColumn.cs` | Selects the active package in a stacked column and forwards clicks to it. |

### Conveyor simulation

| File | Responsibility |
|---|---|
| `Assets/_Game/Script/FallingPixelManager.cs` | Singleton runtime simulation for falling, direction-launched, and belt-bound dots. |
| `Assets/_Game/Script/ConveyorSpline.cs` | Spline sampling, baked lookup table, width/lateral positioning, and closest-progress queries. |
| `Assets/_Game/Script/ConveyorConnections.cs` | Routes the end of one conveyor to zero, one, or multiple next conveyors. |
| `Assets/_Game/Script/ConveyorBeltRenderer.cs` | Builds and scrolls the conveyor mesh and optional wall submeshes. |
| `Assets/_Game/Script/ConveyorNetworkAsset.cs` | Serializable conveyor network data used by the editor save/load workflow. |

### Bucket and scoring

| File | Responsibility |
|---|---|
| `Assets/_Game/Script/Bucket.cs` | Dot reservation/acceptance, fruit visuals, fill/release behavior, and full-bucket event. |
| `Assets/_Game/Script/BucketWorker.cs` | Queues and transports full buckets. |
| `Assets/_Game/Script/GameManager.cs` | Score plus optional uGUI counters for grid and belt dots. |

## Scenes and Prefabs

### Main scene

`Assets/_Game/Scenes/SampleScene.unity` is the only enabled build scene. Its important objects include:

- `Main Camera`
- `GameManager`
- `FallingPixelManager`
- `_FS_PixelGridManager` and `_FS_GridOrigin`
- `Shooter`
- `_Worker`, `_WorkerIdlePoint`, and `_WorkerDropZone`
- Conveyor belt mesh objects

The setup guide still mentions `Gameplay.unity` in one section; treat that as legacy wording unless such a scene is added later.

### Reusable prefabs

| Prefab | Main components/use |
|---|---|
| `Assets/_Game/Prefabs/Dot.prefab` | `Dot` plus its sprite renderer. |
| `Assets/_Game/Prefabs/Conveyor.prefab` | `SplineContainer`, `ConveyorSpline`, `ConveyorConnections`, and `ConveyorBeltRenderer`. |
| `Assets/_Game/Prefabs/Cage.prefab` | Bucket/cage hierarchy, hit zone, fruit fill layer, fruit background layer, and `SpriteGridFill`. |
| `Assets/_Game/Prefabs/FruitSpawn.prefab` | `ModelDotSpawner` with package `SpriteGridFill`. |
| `Assets/_Game/Prefabs/FruitSpawnColumn.prefab` | `ModelDotSpawnerColumn` container for stacked fruit packages. |

When changing a serialized field, inspect both scene instances and the corresponding prefab. Do not assume scene overrides match prefab defaults.

## Editor Tools

| Menu | Source | Purpose |
|---|---|---|
| `Tools > FruitSort > Conveyor Editor` | `Assets/_Game/Script/Editor/ConveyorEditorWindow.cs` | Creates/edits conveyor networks and their links. |
| `Tools > FruitSort > Build Sample Scene` | `Assets/_Game/Script/Editor/FruitSortSceneBuilder.cs` | Rebuilds the sample gameplay objects, generally using the `_FS_` prefix. |

Running the sample-scene builder is destructive to the objects it manages. Use it only when the user explicitly wants the scene rebuilt.

## Tests and Verification

- Current focused suite: `Assets/_Game/Script/Editor/BucketColorFlowTests.cs`.
- Test namespace: `FruitSort.EditorTests`.
- Run through Unity Test Runner in EditMode, preferably the targeted class/test before the whole suite.
- After any C# edit, wait for Unity compilation and inspect Console errors.
- After scene/prefab visual changes, inspect the target in the Unity Editor and run an appropriate Play Mode smoke test.
- Generated solution files may contain package-level duplication; Unity compilation/Test Runner is the authoritative verification path.

## High-Risk Invariants

- Keep every fruit `colorId` unique in `FruitDatabase` and consistent with dots/buckets.
- Preserve the no-Rigidbody design in the high-volume dot simulation unless the architecture is intentionally changed.
- Avoid allocations inside per-dot/per-frame conveyor loops.
- `SpriteGridFill` expects a `SpriteRenderer` on the same GameObject and writes shader properties through `MaterialPropertyBlock`.
- Bucket background and fill are separate renderer layers; keep background sorting below the fill renderer.
- Conveyor connections are directional: the end of `from` routes to the beginning of `to`.

## Search Shortcuts

```powershell
# Gameplay scripts only
rg -n "term" Assets/_Game/Script

# Serialized references in the main gameplay assets
rg -n "fieldName|ComponentName" Assets/_Game/Scenes/SampleScene.unity Assets/_Game/Prefabs

# Fruit configuration IDs and asset references
rg -n "colorId|fruitName" Assets/_Game/Data

# Current focused tests
rg -n "\[Test\]|public void" Assets/_Game/Script/Editor/BucketColorFlowTests.cs
```

Avoid broad searches under `Assets/_Game/Sprites/` unless the task is specifically about art assets.
