# Town vitality and overview navigation — 2026-09-27

The production scene is `Assets/Weedtopia/Game/Scenes/BurningHills.unity`.
The explicit authoring command is **Weedtopia > Production > Author Town Vitality**.
The scene is already authored; opening or playing the game does not create static decoration.

## Implemented

- Free navigation confines the entire ground viewport to the authored overview rectangle, including camera easing, zoom and aspect-ratio changes.
- The canonical overview is centred at plan coordinates (174, 65), with orthographic size 230 and reference aspect 16:9. Wider screens reduce maximum zoom to keep their edges inside that rectangle.
- All four district shortcuts retain orthographic size 48.
- 16 original transparent PNG assets, 16 reusable prefabs, 169 authored decoration instances, and 34 additional directional residents.
- A west neighborhood park with a market, reading cabinet, tea patio, playground and picnic corner; individual house gardens; bakery work/break courts; varied orchard/meadow planting.
- Twenty repetitive large street maples are replaced visually with smaller blossom/rowan variants, exposing the house-garden details. Original tree instances remain inactive in their original hierarchy and are referenced by the vitality stamp for restoration.
- New paths connect the new destinations to existing village paths. The existing farm garden and river crossing are preserved.
- New residents reuse the existing pause-aware animation and town presentation clock.

## Original generated assets

Generated with the built-in `image_gen` tool. Source PNG bytes are copied unchanged.
Full prompts, source paths and intended world sizes: [generation_sources.json](generation_sources.json).
Alpha bounds and source SHA-256 values: [alpha_validation.json](alpha_validation.json).

| Asset | Purpose |
| --- | --- |
| produce_stall | Fruit and vegetable market |
| bread_cart | Bakery handcart |
| flower_bicycle | Florist cargo bicycle |
| reading_corner | Book cabinet and bench |
| tea_patio | Shaded tea table |
| herb_garden | Raised herb beds |
| laundry_garden | Washing and clothesline vignette |
| apiary | Two distinct hive forms |
| workshop_corner | Workbench and tools |
| picnic_nook | Picnic and outdoor game |
| birdbath | Birdbath with small birds |
| play_corner | Swing and sandpit |
| blossom_tree | Flowering pear tree |
| rowan_tree | Rowan with berries |
| fern_garden | Ferns, blue flowers and rocks |
| poppy_meadow | Mixed poppy and wildflower patch |

Project assets: `Assets/Weedtopia/Production/TownVitality/`.
Shared materials and mipmapped, compressed textures at a maximum imported size of 1024 keep the new decoration bounded.
Decoration has no colliders and does not intercept building selection.

## Evidence

- [Integration inventory](integration.json)
- [Preservation comparison](preservation.json): zero original scene objects removed; the world's child list and town actor array extended; 20 original trees only deactivated, with their full prefab data retained.
- [Game View receipt](gameview_receipt.json): actual PlayMode screenshots and content fingerprint.
- [Overview](Screenshots/01_overview.png)
- [Neighborhood park](Screenshots/03_neighborhood_park.png)
- [Market detail](Screenshots/04_market_details.png)
- [Residential gardens](Screenshots/06_settlement.png)
- [Industry](Screenshots/07_industry.png)

Native verification: **28/28 Asset test cases and 33/33 PlayMode test cases passed**, using full runs plus targeted reruns of corrected cases. Final visual review and all 14 Game View images are recorded in [validation.json](validation.json).
Virtual Unity Input System touch tests are distinct from physical touchscreen testing.
Rendered visual inspection is recorded separately from user art acceptance.

