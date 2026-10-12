# Purple Garden — authored story and content

Purple Garden is the eighth campaign city. The invitation becomes available at a combined population of **500,000** across Burning Hills, Big Rock City, Seven Hills, Little Montana, Cassa Vadela, Mark Wood City and Green Leaf Village. Purple Garden does not count towards its own invitation. Accepting the mandate grants a one-time start-up allowance of **12,500 money and 7 team cards** through the existing shared economy.

Purple Garden began as a botanical research city. Violet glasshouses, water courts and an open seed archive connected laboratory knowledge with the gardens people tended at home. Its shared water network failed after the plans were lost. Private archive contracts then separated records from the people who contributed them. Some residents left; others kept plants, drawings and unanswered questions alive.

The player helps the permanent residents rebuild everyday supply, open knowledge and share care. Malik learns to ask before drawing a solution. Ada acknowledges her part in the former private archive. Iris and Ravi recover time for themselves. The night bloom festival celebrates the first common future; it does not retire the residents or end their personal stories.

| District | English title | Existing gameplay zone |
| --- | --- | --- |
| Violetthaine | Violet Groves | Farm |
| Blütenmarkt | Bloom Market | City |
| Wasserhöfe | Water Courts | Settlement |
| Glashütten | Glassworks | Industry |

| Resident / stable ID | Role and personal conflict | Appears at local residents | Chapter thresholds |
| --- | --- | ---: | --- |
| Iris Voss / `pg_iris` | Botanical keeper; learns to share care and enjoy her own garden | 50 | 50 / 1,000 / 5,000 / 15,000 |
| Malik Reed / `pg_malik` | Irrigation engineer; incomplete inherited plans become a map of neighbours’ knowledge | 120 | 120 / 1,500 / 6,000 / 16,000 |
| Viola Fenn / `pg_viola` | Coffeeshop host; replaces exclusive entry rules with conversations that allow disagreement | 260 | 260 / 1,900 / 6,800 / 17,000 |
| Theo Moss / `pg_theo` | Glasshouse restorer; teaches repair knowledge instead of becoming indispensable | 500 | 500 / 2,400 / 7,600 / 17,500 |
| Jun Park / `pg_jun` | Community gardener; brings his grandmother’s unrecorded knowledge into research | 850 | 850 / 2,900 / 8,400 / 18,000 |
| Ada Bloom / `pg_ada` | Seed archivist; opens access and preserves her own mistakes beside replies | 1,300 | 1,300 / 3,500 / 9,300 / 18,500 |
| Ravi Sol / `pg_ravi` | Night market baker; shares recipes and shifts to regain evenings as a neighbour | 1,900 | 1,900 / 4,300 / 10,500 / 19,500 |
| Selma Rain / `pg_selma` | Mediator; creates conversations where quiet voices and a no remain visible | 2,600 | 2,600 / 5,200 / 11,500 / 20,000 |

Every resident has four personal chapters and three recurring variants, with the established 300-second recurring delay. Chapter three offers two distinct saved choices and outcomes. Chapter memories use the existing resident log. The seed archive landmark uses Theo’s `repair_team` / `teaching_bench` branch through the existing progress visual.

| Resident | Saved choice IDs |
| --- | --- |
| Iris | `twilight_walk` / `quiet_garden` |
| Malik | `public_map` / `care_roster` |
| Viola | `tasting_table` / `evening_circle` |
| Theo | `repair_team` / `teaching_bench` |
| Jun | `seed_exchange` / `paired_gardens` |
| Ada | `open_catalog` / `travelling_archive` |
| Ravi | `shared_recipes` / `neighbour_oven` |
| Selma | `listening_bench` / `public_minutes` |

The water network and archive are narrative places. Quests use existing building upgrade, harvest, processing, sales and delivery progress; there is no new irrigation resource, water simulator, seed inventory or laboratory production system. New progress counters start at quest acceptance, as in the accepted resident story schema.

| Community project | Local threshold | Residents’ required first chapters | Fresh progress goal |
| --- | ---: | --- | --- |
| `pg_joint_water_court` | 2,000 | Iris, Malik, Viola | 180 cannabis bud sales in the coffeeshop |
| `pg_joint_seed_archive` | 6,500 | Theo, Jun, Ada | 340 delivered goods |
| `pg_joint_night_bloom` | 12,500 | All eight | 45 completed deliveries |

The content includes 56 authored building instances, 35 routes and 18 main quests. `BurningHills.json` remains the shared catalog, now at version 9. The addition appends Purple Garden records, keeping every previous city, building, route, quest, resident story, collaboration, fallback text and unrelated root property intact. German fallback text and 315 keys in each of German, English, Spanish, French and Russian were authored; all placeholders match across languages.

Run the author explicitly from the repository root:

```powershell
python -X utf8 Tools/Implementation/author_purple_garden.py
```

The author reuses the previous content tool’s established translated goal wording and counter helpers. It authors original Purple Garden prose and does not edit the previous tool. It validates all proposed records and translations before writing. It refuses conflicting existing IDs or text keys rather than replacing content.

Evidence is saved in `content_validation_first_run.json`, `content_validation.json`, `idempotence_validation.json` and `preservation_validation.json`. The first pass added the content. The second pass reported `changed: false`; all six catalog files remained byte-identical. Baseline copies from before either pass live in `Preservation/Before`. `validate_preservation.py` checks every previous entry and locale property against that baseline and checks the new translations for common mojibake markers.

These receipts prove content authoring, references, localization parity and preservation. Unity import, domain tests, scene/prefab checks, gameplay and Game View evidence are recorded by the integration work separately.
