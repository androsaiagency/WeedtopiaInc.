# Project structure

This document defines the canonical locations for authored and generated Weedtopia assets. Keep Unity `.meta` files with their assets when moving content so serialized GUID references remain stable.

## Canonical asset roots

| Path | Responsibility |
| --- | --- |
| `Assets/Weedtopia/Art` | Building source art, import staging, shaders, and the 251 reference-stable building wrappers. |
| `Assets/Weedtopia/Data` | Authored catalogs and other project data assets. |
| `Assets/Weedtopia/Game/Audio` | Game audio, split into approved runtime-ready files and retained source material. |
| `Assets/Weedtopia/Game/Content` | Game content definitions and content configuration. |
| `Assets/Weedtopia/Game/Materials` | Materials owned by the playable game layer. |
| `Assets/Weedtopia/Game/Prefabs` | Reusable gameplay prefabs. |
| `Assets/Weedtopia/Game/Scenes` | Production build scenes. Only `Bootstrap` and `BurningHills` belong here currently. |
| `Assets/Weedtopia/Game/VisualReview` | Isolated review scenes and their review-only fixtures. These are not build scenes. |
| `Assets/Weedtopia/Game/Archive` | Retained, non-production assets that may still be useful for provenance or rollback. |
| `Assets/Weedtopia/Production` | Authored production presentation assets and generated runtime resources. Preserve established sub-pipelines and their evidence contracts. |
| `Assets/Weedtopia/Scripts` | Runtime, editor, and test assemblies, separated by compilation boundary. |
| `Assets/Settings` | Unity and URP project infrastructure. This conventional Unity root intentionally remains outside the game-owned tree. |
| `Docs` | Architecture, design, production evidence, validation reports, and implementation handoffs. |
| `Tools` | External/offline asset-production tooling. Generated environments and caches remain ignored. |

## Scene policy

- Production scenes live only in `Assets/Weedtopia/Game/Scenes`.
- Review and approval scenes stay beside the review workflow that owns them and must not be added to build settings.
- Stock Unity template material is retained under `Assets/Weedtopia/Game/Archive/UnityTemplate` and must not be added to build settings.
- Build settings are the source of truth for shipped scenes; currently they contain `Bootstrap` followed by `BurningHills`.

## Audio policy

- `Game/Audio/Approved` contains clips that editor authoring tools may bind to runtime objects.
- `Game/Audio/Source` contains source generations retained for reproducibility and is not the runtime lookup location.
- Code must use the canonical `Assets/Weedtopia/Game/Audio/...` path when an editor-only path lookup is unavoidable.

## Change rules

1. Reuse an existing domain folder before creating a new root under `Assets`.
2. Move assets together with their `.meta` files; never regenerate GUIDs for referenced assets.
3. Check both GUID references and literal path references before a move.
4. Do not deduplicate review or evidence assets merely because their bytes match a production asset; their separate paths communicate workflow ownership.
5. Treat building wrappers, catalog assignments, authored anchors, offsets, production resources, and acceptance evidence as protected inputs.
6. Keep generated Unity folders (`Library`, `Temp`, `Logs`, `obj`, and `UserSettings`) out of version control.
