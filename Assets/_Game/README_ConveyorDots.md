# Conveyor Dot Gameplay Setup

## Package

This project already contains `com.unity.splines` version `2.8.4` in
`Packages/manifest.json`. For another project, install **Splines 2.8.x** from
**Window > Package Manager > Unity Registry > Splines**.

## Prefabs

1. Create a small square sprite GameObject named `Dot`.
2. Add `SpriteRenderer`, `BoxCollider2D`, and `Dot`.
3. Do not add a Rigidbody2D. Dot-to-dot interaction is transform-based.
4. Put the prefab on a dedicated `Dot` layer, then assign that layer to
   `Shooter.dotLayerMask`.

## Scene

1. Use an orthographic camera and frame the grid above the conveyor.
2. Create `Conveyor`, add `SplineContainer`, and edit a left-to-right spline.
3. Add `ConveyorSpline` to the same object. Assign its container and set
   `beltWidth`.
4. Create `FallingPixelManager`, assign the conveyor, and keep `maxDots` at 500
   or lower.
5. Create `PixelGridManager`, assign the Dot prefab, a readable Texture2D or the
   fallback grid dimensions, palette colors, dot size, and HP.
6. Create a player below the grid, point local Y upward, add `Shooter`, and
   assign its Dot layer mask. A LineRenderer is optional.
7. Create bucket GameObjects with `Bucket`. Assign the conveyor, color ID,
   spline progress, lateral offset, attraction radius, and max fill. Multiple
   buckets may share a color ID.
8. Create `GameManager` and assign the grid and falling managers. Optional UI
   uses standard uGUI `Text` components.

## Texture Input

Enable **Read/Write** on a source texture because `PixelGridManager` reads its
pixels at runtime. Palette entries map texture colors to integer color IDs by
nearest RGB distance.

## Tuning For 500 Dots

- Keep `cellSizeMultiplier` near `1.2` so the 3x3 Moore neighborhood covers
  touching dots without creating large buckets.
- Start with `maxNeighborsPerDot` between 8 and 12.
- Keep the dot prefab free of Rigidbody2D and per-dot `Update` methods.
- Use one `FallingPixelManager`; it owns movement, rotation, separation, and
  reverse-loop cleanup.
- Reduce bucket count or attraction radius if many buckets scan 500 dots at
  once. Bucket scanning stops after claiming one dot per frame.
