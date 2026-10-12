# Art, font and audio provenance

## Commercial audio eligibility confirmed, 30 September 2026

The user explicitly confirmed completion of the Stability AI registration and clarified that current revenue before deducting costs is USD 0: no sales yet. These are user-provided confirmations, not independently audited financial or account records. They resolve the previously undocumented registration and revenue eligibility for commercial use under the Stability AI Community License. The original agreement's other terms continue to apply.

The current agreement was checked at <https://stability.ai/community-license-agreement>: Section III permits commercial use subject to registration and the revenue threshold; Section IV(c)(iii) addresses ownership of generated outputs. The model used for the background music is `stabilityai/stable-audio-3-small-music`, as documented in `generation_details.txt` and `audio_provenance.json`.

The confirmation is recorded in `Evidence/audio_license_confirmation.json` against the current music file hash. It is a commercial eligibility record, not a whole-build fingerprint receipt or a listening, device, visual, or overall release acceptance. A future build preflight must bind its audio-license receipt to that build's fingerprint. This update does not re-run or alter historical preflight results.

## Natural environment revision, 24 September 2026

Three additional original atlases were generated with the built-in ImageGen for the user's requested lush, natural environment: Woodland, Gardens and Ground. They are preserved without pixel edits under `Assets/Weedtopia/Production/Environment/Art`. Unity import rectangles, materials and authored scene/prefab placement provide integration. The user's environment example guided the composition and palette; its graphics were not extracted or copied into the game.

Exact prompts, source dimensions and SHA-256 hashes are in [EnvironmentRevision/art_prompts.json](../EnvironmentRevision/art_prompts.json) and [art_provenance.json](../EnvironmentRevision/art_provenance.json). All previous building art and original assets remain unchanged according to the fresh [preservation comparison](../EnvironmentRevision/preservation.json). Existing environment assets remain archived in inactive scene groups. This revision adds no external purchase or stock license and does not assert final user acceptance. [Current visual and technical evidence](../EnvironmentRevision/REPORT.md).

## Current building integration, 23 September 2026

The four existing prototypes were explicitly approved by the user. The separate explicit reply “Lokale pixelbewahrende Freistellung verwenden” authorizes local extraction for the other 247 accepted sources. Receipts are `Docs/LayoutRevision/building_approval.json` and `cutout_method_approval.json`.

The four prototype PNGs are copied byte-for-byte. The remaining sprites retain every original RGB pixel; local segmentation and color-guided correction modify alpha only. Seven images receive transparent outer padding to keep edge foliage clear of the sprite canvas. The original source files are untouched. Paths, hashes, alpha bounds, scale and review coverage are recorded per slot in `Docs/LayoutRevision/Cutouts/manifest.json`. Processing scripts and the local segmentation model remain outside `Assets` and are not included as runtime game assets.

All 251 production wrappers now contain their corresponding sprite binding. Their existing seven anchors, GUIDs and catalog references are preserved; wrapper files themselves intentionally changed. The current preservation evidence is `Docs/LayoutRevision/preservation.json`. The historical preservation report below predates this explicitly authorized conversion. Whole-game, listening and target-device acceptance remain separate open gates. Commercial audio eligibility was subsequently confirmed above on 30 September 2026.

## Existing building art

All 251 accepted images, three chosen style references, five layout references, original four cutout prototypes, productive wrappers and catalog are preserved. `Evidence/preservation.json` verifies current bytes against the fresh immutable baseline. Ownership and acceptance records remain in `Docs/ArtProduction`; this pass does not invent additional license or user approvals.

## Original raster generation

Six atlas/material images were generated using the available built-in `image_gen` tool: Characters, Props, Icons, Terrain, Skin and Conveyor. Prompts are stored verbatim in `generation_prompts.json`. There was no external purchase, stock-image substitution or third-party asset download. Original generated pixels are preserved; `prepare_production_art.py` only reads alpha to calculate Unity import rectangles. Sub-sprite import regions and pivots are in `Assets/Weedtopia/Production/art_slices.json`. Production PNG paths, dimensions and hashes are in `art_provenance.json`.

The images were generated for this task and inspected by the agent as atlases and in actual Unity Game View. That inspection is not the user's building approval, a guarantee of copyright exclusivity, or a full human visual acceptance of the game.

## Fonts

Lato Regular and Bold were downloaded unmodified from the official Google Fonts repository (`ofl/lato`). Original TTF files and `OFL.txt` are under `Assets/Weedtopia/Production/Fonts`. The SIL Open Font License 1.1 permits embedding and redistribution subject to its terms; the license accompanies the font files. Sources: <https://github.com/google/fonts/tree/main/ofl/lato>, <https://github.com/google/fonts/blob/main/ofl/lato/OFL.txt>.

## Original audio

The established local Stable Audio 3 workflow generated a music source with `small-music` on CUDA and four cues with `small-sfx` on CUDA. Raw WAVs are preserved under `Assets/Weedtopia/Game/Audio/Source/BurningHills`; Unity imports the processed WAVs under `Assets/Weedtopia/Game/Audio/Approved/BurningHills`. The folder name follows the existing Source/Approved workflow and does not itself assert human listening or commercial release approval.

`generation_details.txt` contains the exact music/negative/SFX prompts and parameters. `audio_provenance.json` records each source, result, hash and PCM measurement. Music: 62-second source, 60-second circular crossfade loop, 2-second overlap; 44,100 Hz stereo PCM_24, exactly 2,646,000 samples. SFX: generated at two seconds, active-region trim, short edge fades, no runtime synthesis. Unity music uses streaming; short cues are decoded on load and played on their actual action/state events.

Model cards checked during the original generation pass: <https://huggingface.co/stabilityai/stable-audio-3-small-music> and <https://huggingface.co/stabilityai/stable-audio-3-small-sfx>. They identify the Stable Audio Community License and link to commercial eligibility terms at <https://stability.ai/license>. The linked agreement is <https://huggingface.co/stabilityai/stable-audio-open-1.0/blob/main/LICENSE.md>. Registration and revenue eligibility were originally undocumented; the user's explicit confirmations on 30 September 2026 resolve those two points as recorded above. This is permission subject to the license terms, not an unconditional warranty concerning generated audio.

Technical PCM and Unity binding checks passed where backed by the linked evidence. Listening on actual speakers/headphones, the full in-game mix, repeated loop perception and target-device audio are **not accepted**. They remain separate release blockers. The old two development cues and old resident/vehicle prefabs were moved with identical asset/meta bytes into `Assets/Weedtopia/Game/Archive/ReplacedPresentation` after all active references had replacements.
