# Saint Mary Ville: authoring and artwork

Saint Mary Ville uses the existing fixed isometric 2D/2.5D city contracts. The installer saves static objects in reusable prefabs and the city scene. `SaintMaryVilleStamp` records content version 11, six permanent inhabitants, three community places and six authored architecture prefabs.

The explicit menu command is **Weedtopia > Cities > Install or Repair Saint Mary Ville**. `SaintMaryVilleAuthoring.Apply` requires installed content and the three image files before it edits authored assets. Existing city scenes receive one additive travel row through `SaintMaryVilleCampaignAuthoring`; their existing scene objects, building references and map assignments stay under their original authoring ownership.

## Final production artwork

The following images were generated with the built-in `image_gen` tool and copied into `Assets/Weedtopia/Production/SaintMaryVille/Art`. The final prompt set is recorded in `generation_manifest.json`; dimensions, original PNG hashes, alpha ranges and grid checks are in `alpha_validation.json`.

| Image | Import | Sprites |
| --- | --- | --- |
| `resident_atlas.png` | 3 columns x 2 rows, source PNG 1254 x 1254 | Six bodies and six portraits |
| `architecture_atlas.png` | 3 columns x 2 rows, source PNG 1536 x 1024 | Six architecture sprites |
| `town_sign.png` | Single sprite, source PNG 1374 x 1145 | Saint Mary Ville welcome sign |

Both atlas images have zero pixels above alpha 24 on their expected row/column dividing lines. The importer trims alpha padding and leaves the source image bytes unchanged. Alpha validation is a read-only PNG inspection; no Python image editing was used.

The city palette is warm cream plaster, red brick and petrol woodwork. Architecture prefabs are `FerryCourt`, `MaryHouse`, `LightsPavilion`, `FerryWorkshop`, `RiverClassroom` and `RiverGardenHouse`. Production building bands use 251 copied wrapper prefabs and a dedicated cream/brick/petrol palette material, preserving the source sprites, transforms, hit areas and gameplay anchors.

## Residents and permanent visual history

| Identity | Art role |
| --- | --- |
| `sm_mara` / Mara Bell | Mary-house host, silver hair, book and old brass house key |
| `sm_elias` / Elias Reed | Ferry-yard mechanic, working apron, boat model and carpenter's mallet |
| `sm_rosa` / Rosa Marin | Market cook, red apron and bread basket |
| `sm_finn` / Finn Alder | River-bank courier, red shoulder satchel and rolled delivery maps |
| `sm_amira` / Amira Vale | Chronicler, petrol jacket, notebook and pencil |
| `sm_hugo` / Hugo Mercer | Gardener and river observer, straw hat, trowel and river map |

Each resident receives four serialized chapter-memory targets. The shared `VipStoryActor` refreshes their visibility from saved story progress. The three community places (`sm_faehrhof`, `sm_mary_house`, `sm_lights_evening`) use `CityPlaceProgressVisual` and the collaboration contracts `sm_joint_ferry_yard`, `sm_joint_open_house`, `sm_joint_lights_evening`.

Elias's third chapter has the saved choice IDs `ferry_workshop` and `river_classroom`. Both branches have separate architecture silhouettes, authored under the ferry yard and as personal chapter memories. The shared runtime component switches the appropriate objects from the saved choice without constructing static objects.

Unity installation, compiler, EditMode, PlayMode, preservation and Game View results are recorded by the task's verification run. PNG transparency validation alone does not establish Game View or device acceptance.
