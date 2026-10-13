"""Apply the final story-role alignment once Unity's running test phase has ended."""
from pathlib import Path

path = Path('Assets/Weedtopia/Scripts/Editor/GameSetup/SaintMaryVilleAuthoring.cs')
source = path.read_text(encoding='utf-8')
changes = [
    ('new Vector2(84, 214), new Vector2(-152, 64), new Vector2(-27, -57), new Vector2(240, -95), new Vector2(-170, 22), new Vector2(-28, 14)',
     'new Vector2(84, 214), new Vector2(-152, 64), new Vector2(-27, -57), new Vector2(-28, 14), new Vector2(84, 145), new Vector2(240, -95)'),
    ('new[] { "Prop_sign", "Town_reading_corner", "Town_laundry_garden", "Town_tea_patio" }',
     'new[] { "Town_tea_patio", "Prop_sign", "Town_reading_corner", "Town_picnic_nook" }'),
    ('new[] { "Prop_crates", "Town_workshop_corner", "Town_workshop_corner", "Prop_lantern" }',
     'new[] { "Town_workshop_corner", "Town_workshop_corner", "Town_workshop_corner", "Town_tea_patio" }'),
    ('new[] { "Town_bread_cart", "Town_tea_patio", "Town_produce_stall", "Town_picnic_nook" }',
     'new[] { "Town_produce_stall", "Town_bread_cart", "Town_tea_patio", "Town_picnic_nook" }'),
    ('new[] { "Prop_planter", "Town_herb_garden", "Town_rowan_tree", "Town_herb_garden" }',
     'new[] { "Prop_sign", "Prop_crates", "Town_flower_bicycle", "Prop_lantern" }'),
    ('new[] { "Prop_crates", "Town_workshop_corner", "Prop_sign", "Town_workshop_corner" }',
     'new[] { "Town_reading_corner", "Prop_sign", "Town_reading_corner", "Town_tea_patio" }'),
    ('new[] { "Prop_lantern", "Town_picnic_nook", "Town_reading_corner", "Town_tea_patio" }',
     'new[] { "Prop_sign", "Town_herb_garden", "Town_reading_corner", "Prop_lantern" }'),
    ('"AmirasSharedTools"', '"EliasSharedTools"'),
]
for before, after in changes:
    if before in source:
        if source.count(before) != 1:
            raise SystemExit('Unexpected repeated role-alignment source: ' + before)
        source = source.replace(before, after, 1)
    elif after not in source:
        raise SystemExit('Expected role-alignment source has changed: ' + before)
if source != path.read_text(encoding='utf-8'):
    path.write_text(source, encoding='utf-8')
    print('Applied final Saint Mary Ville story-role locations and authored memories.')
else:
    print('Saint Mary Ville story-role locations and authored memories already aligned.')
