# Burning Hills presentation system — 2026-09-22

The chosen source direction remains the warm readability of mockup 1, brass/copper machinery of mockup 4 and the optimistic evening mood of mockup 5. `Docs/ArtProduction` and all accepted building sources remain unchanged. Final visual acceptance is open.

## Authored contracts

- `Assets/Weedtopia/Production/PresentationArt.asset` binds 70 named sprites and the two original Lato fonts. Missing names throw; there is no generated graphic fallback.
- Eight `Character_*.prefab` assets supply whole-body art, portraits from the same source, collider, ground contact shadow and state-dependent 2.5D motion. Speaker and workplace bindings are serialized in BurningHills. NPC picking accounts for broad adjacent building colliders and matches sprite sorting.
- Ten reusable environment prop prefabs plus the delivery prefab replace capsule residents, route bars and cube trucks. There are 48 authored vehicle slots; deliveries reuse them. Three fixed cargo markers per conveyor follow actual source availability and stop with a blocked flow.
- Continuous 440×440 terrain, curved river, parcels and route meshes are saved assets. Roads and delivery interpolation share authored route points. Scene object and building instance coordinates remain distinct: no building site was repositioned.
- Ground material atlas uses derivative-correct texture sampling, mipmaps and anisotropy. Character and prop sprites use a bottom pivot; UI sprites use a central pivot. Camera pitch is 35.264°, yaw 45°, gameplay zoom stays 12–55. Transparency sorting uses the isometric x+z ordering, with small foreground offsets for characters and deliveries.
- Fixed scene objects are authoring outputs, not rebuilt during startup. Runtime creation remains limited to existing UI list entries/building visual instances, pooled actors and explicit diagnostic instrumentation.
- Original 251 wrapper anchors, offsets, collider roots, tier mapping and catalog references are protected. No productive building sprite replacement has occurred.

## UI

Teal enamel panels, cream paper, brass frames and gold primary actions use six original skin sprites. Thirty-six icons cover resources, action states and navigation. Lato Regular/Bold replace LegacyRuntime in reachable UI, including inactive modal panels. Body text generally uses 18–28 reference pixels; main actions are 48–60 reference pixels high. Actual physical touch acceptance remains required.

HUD counters show four actual resources. Only the selected building has a world label. Internal tier IDs and the development banner are hidden from presentation; the placeholder condition remains explicit in tooling and release reports. Contextual actions collapse vertically; role cards have distinct role icons, portraits, rarity colors and a real spin-result animation. New conversation panels carry a matching portrait and relevant authored dialog. Close controls remain on top of modal siblings.

## Budgets and their limits

Six compact original raster files supply the whole new non-building presentation. Their actual pixel dimensions and SHA256 hashes are in `art_provenance.json`; the importer caps textures at 2048 and keeps one shared atlas per set. The four existing building prototypes remain review-only; the 251 accepted building PNGs remain outside player dependencies under Docs.

The diagnostic target is Windows x64, Direct3D11, RTX 3070, 1920×1080. Desired engineering targets after complete building integration: p95 frame time below 16.7 ms, no repeated managed allocations from presentation-only idle work, no more than 600 draw calls in overview, and a measured texture budget below 512 MiB on that desktop target. These are design budgets, not blanket acceptance. Current measured reports distinguish development buildings, editor overhead, actual player counters, runtime texture size and unavailable GPU VRAM measurement. Re-profile after the 251 integration and on each actual shipping target.

## Safe explicit operations

`Weedtopia > Production > Apply Authored Presentation` installs only once and preserves an existing authoring stamp. `Apply Reviewed Visual Corrections` performs bounded known corrections and preserves independently moved NPCs. `Production Visual Preflight` writes exact issues and deliberately fails with current building/gate blockers. The ordinary scaffold repair preserves production character/vehicle/audio assets and the new UI instead of reverting them.

All capture/build/test tools require an owned batch instance. The PowerShell runner checks both Unity processes and `Temp/UnityLockfile`; it neither deletes a lock nor closes an editor.

The first diagnostic build caused Unity to serialize URP shader-prefilter/global-settings caches and to change the project batching and Unity Connect switches. The two project-setting changes were restored from the immutable baseline. The runner now snapshots and restores those project settings and both URP asset files around every owned run. The first two URP cache serializations remain explicitly listed as Unity build metadata changes in `Evidence/preservation.json`; their asset files and GUIDs are retained. No building source, catalog, wrapper or manually assigned art was replaced by this serialization.
