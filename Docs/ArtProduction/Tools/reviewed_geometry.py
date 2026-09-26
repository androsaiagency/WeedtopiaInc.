"""Reviewed tier deltas and dimension notes, plus a local idempotent migration.

Run directly once after review; shared writes use the ingestion lock. Accepted
image prompts and image files are never edited. The authoring generator imports
the dimension helpers so that future missing-file scaffolds use the same data.
"""
from pathlib import Path
import csv
import hashlib
import json
import re

CORRECTIONS = {
    ('park', 2): ('Zweites breites eingefasstes Beet', 'second broad bounded planting bed',
        'Zusätzliche breite erhöhte Pflanzterrasse hinter dem Pavillon', 'add one broad raised rear planting terrace behind the retained pavilion'),
    ('park', 3): ('Große fest integrierte Sitzarkade', 'large integrated seating arcade',
        'Neue seitliche Sitzarkade mit drei massiven Rundbögen außerhalb des bisherigen Pavillondachs', 'add an attached side seating arcade with three thick rounded arches beyond the original pavilion roof'),
    ('park', 4): ('Überdachter Community-Pavillon', 'roofed community pavilion',
        'Breiter rückwärtiger Community-Anbau mit eigenem zweitem Runddach', 'add a broad attached rear community wing with a separate second rounded roof'),
    ('park', 6): ('Zweite große Baumgruppe als kompakte Krone', 'second large tree group with compact canopy',
        'Erste große kompakte Baumkrone über der rückwärtigen Pflanzterrasse', 'add the first large compact tree canopy rising above the rear planting terrace'),
    ('park', 9): ('Runder emaillierter Gartenpavillon', 'rounded enamel garden pavilion',
        'Breite runde Gartenrotunde auf der hinteren erhöhten Terrasse', 'add a broad attached round garden rotunda on the raised rear terrace with a separate roof'),
    ('residential_house', 3): ('Tiefe überdachte Eingangsloggia', 'deep roofed entrance loggia',
        'Seitliche Erweiterung der bestehenden Eingangsloggia um zwei breite Rundbogenfelder', 'extend the existing entrance loggia sideways with two broad rounded bays; keep the front door fixed'),
    ('bakery', 3): ('Großes kupfergedecktes Lieferfenster', 'large copper-roofed delivery window',
        'Breite baulich verbundene Versandloggia hinter dem bestehenden Lieferfenster', 'add a broad attached delivery loggia extending rearward from the existing copper-capped output window'),
    ('butter_factory', 7): ('Dicke geschlossene Rohrbrücke', 'thick enclosed pipe bridge',
        'Breite geschlossene Wartungs- und Prozessbrücke über der hinteren Rührwerkhalle', 'add a broad enclosed process-and-maintenance bridge above the rear mixing hall, above the original tank pipework'),
    ('supermarket', 3): ('Tiefe überdachte Einkaufsarkade', 'deep roofed shopping arcade',
        'Neue durchgehende seitliche Einkaufsarkade mit drei massiven Bögen', 'add a continuous attached side shopping arcade with three thick rounded arches beyond the entrance hood'),
    ('coffee_shop', 5): ('Dauerhafte verputzte Eckrotunde', 'permanent plaster corner rotunda',
        'Neuer hoher runder Ecksaal hinter der unveränderten Servicefront', 'add a tall rounded rear corner customer hall with a distinct curved roof above the original service front'),
    ('farm_warehouse', 5): ('Dauerhafte verputzte Lagerhalle', 'permanent plaster storage hall',
        'Erhöhte verputzte Hauptlagerhalle mit breitem durchgehendem Dachmonitor', 'raise the plaster main storage hall and add a broad continuous clerestory roof monitor above its barn ridge'),
    ('residential_house', 5): ('Dauerhaftes verputztes Wohnhaus', 'permanent plaster residential house',
        'Großes bewohnbares Dachgeschoss mit breiter zentraler Gaube', 'add a large inhabited attic with a broad central dormer; the first complete upper floor comes at T08'),
    ('butter_factory', 5): ('Dauerhafte cremefarbene Verarbeitungshalle', 'permanent cream processing hall',
        'Große gerundete Mischhallenhaube über der bisherigen Hauptanlage', 'add a large rounded cream mixing-hall roof hood above the retained main processing plant'),
    ('supermarket', 5): ('Dauerhafte verputzte Markthalle', 'permanent plaster market hall',
        'Hohe breite zentrale Lichtgewölbehalle über der bestehenden Verkaufsmitte', 'add a tall broad central clerestory-vault hall above the existing retail center'),
    ('bakery', 5): ('Dauerhafte verputzte Backhalle', 'permanent plaster baking hall',
        'Großes neues Terrakotta-Tonnengewölbe über der bisherigen Backhalle', 'add a large new terracotta barrel vault over the retained bakehouse hall'),
}

RESIDENTIAL_HEIGHTS = [2.5,4.8,5.0,5.0,5.3,6.3,6.3,6.5,8.8,9.0,11.5,11.8,14.5,
                       15.0,17.5,18.0,20.5,24.0,26.0,27.0,28.0,30.0,33.0,35.0,38.0]
PARK_BUILDING_HEIGHTS = [0.8,2.8,2.8,3.0,3.3,3.3,3.3,3.5,3.5,4.0,4.0,4.8,6.2,
                         6.2,7.2,8.0,9.5,11.0,12.0,12.5,13.0,14.5,16.0,16.5,18.0]
PARK_TOTAL_HEIGHTS = [0.8,2.8,2.8,3.0,3.3,3.3,4.8,4.8,4.8,5.0,5.0,5.5,6.2,
                      6.2,7.2,8.0,9.5,11.0,12.0,12.5,13.0,16.0,16.0,16.5,18.0]

def production_height(family, tier_index, maximum):
    if tier_index >= 0 and family == 'residential_house':
        return RESIDENTIAL_HEIGHTS[tier_index]
    if tier_index >= 0 and family == 'park':
        return PARK_TOTAL_HEIGHTS[tier_index]
    return maximum if tier_index < 0 else round(maximum*(.08 if tier_index == 0 else .12+.88*tier_index/24),1)

def dimension_note(family, tier_index):
    if family == 'cultivation_plot' and tier_index == 1:
        return 'Maßtrennung T01: Der Terrakottatopf hat etwa 0,65 m Außendurchmesser; 1,2 × 1,2 m bezeichnet die Pflanzenkronenhülle einschließlich Blattüberstand. Die Gesamthöhe inklusive Pflanze beträgt etwa 1,8 m. Der gespeicherte tatsächlich verwendete Bildprompt bleibt unverändert.'
    if family == 'residential_house' and tier_index >= 0:
        return f'Wohnmaßstab: Gesamthöhe etwa {RESIDENTIAL_HEIGHTS[tier_index]:g} m. Bis T07 bleibt der Kern eingeschossig; T05 ergänzt ein bewohnbares Dachgeschoss, T08 das erste vollständige Obergeschoss. Zusätzliche Flügel erhöhen die Grundfläche, nicht automatisch die Geschosszahl.'
    if family == 'park' and tier_index >= 0:
        return f'Parkmaßstab: Architektur/Pavillon maximal etwa {PARK_BUILDING_HEIGHTS[tier_index]:g} m hoch, gesamte Silhouette einschließlich Baumkronen etwa {PARK_TOTAL_HEIGHTS[tier_index]:g} m. Eine höhere Baumkrone vergrößert nicht die Pavillongeschosse; T12 ist der erste zweigeschossige Community-Pavillon.'
    return ''

def image_dimension_note(family, tier_index):
    if family == 'residential_house' and tier_index >= 0:
        return f'Current residential height about {RESIDENTIAL_HEIGHTS[tier_index]:g}m. Through T07 keep a single full ground floor; T05 adds an inhabited attic, T08 the first complete upper floor. Do not stretch doors or ceilings to fill the maximum family envelope.'
    if family == 'park' and tier_index >= 0:
        return f'Current park architecture height at most {PARK_BUILDING_HEIGHTS[tier_index]:g}m; total height including compact tree canopies about {PARK_TOTAL_HEIGHTS[tier_index]:g}m. Tree height is not pavilion height. The first two-storey community pavilion arrives at T12.'
    return ''

def family_dimension_note(family):
    if family == 'residential_house':
        return 'Höhenstaffel: frühe Wohnstufen T01–T07 etwa 4,8–6,5 m, mit bewohnbarem Dachgeschoss ab T05; erstes vollständiges Obergeschoss T08 bei etwa 8,8 m. Weitere Geschosse und Wohntürme steigen danach bis 38 m. Die Stufenhöhen folgen der Architektur, keiner linearen Levelskalierung.'
    if family == 'park':
        return 'Höhenstaffel: Die Slot-Gesamthöhe enthält Baumkronen. Architektur und Bäume werden in jeder Asset-Spezifikation getrennt beziffert: T01-Pavillon 2,8 m; T06–T08 Bäume bis 4,8 m bei 3,3–3,5 m hoher Architektur. Das zweite Pavillongeschoss entsteht erst T12. Die Endstufe erreicht insgesamt höchstens 18 m.'
    if family == 'cultivation_plot':
        return dimension_note(family, 1)
    return ''

def migrate():
    from ingest_generated_image import production_lock
    import validate_pipeline as pipeline
    root = Path(__file__).resolve().parents[3]
    art = root / 'Docs/ArtProduction'
    generator = art / 'Tools/build_production_data.py'
    with production_lock():
        manifest_path = art / 'BUILDING_ASSET_MANIFEST.json'
        manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
        slots = manifest['slots']
        accepted_prompts = {s['image_prompt_file']: hashlib.sha256((root/s['image_prompt_file']).read_bytes()).hexdigest()
            for s in slots if s['status'] == 'accepted'}
        before_status = {(s['building_id'],s['tier_id']):s['status'] for s in slots}
        source = generator.read_text(encoding='utf-8')
        for family, index in CORRECTIONS:
            old_de, old_en, new_de, new_en = CORRECTIONS[(family,index)]
            old_line, new_line = old_de+'|'+old_en, new_de+'|'+new_en
            if old_line in source:
                source = source.replace(old_line,new_line,1)
            elif new_line not in source:
                raise ValueError(f'Unrecognized generator plan: {family}/T{index:02d}')
        import_line = 'from reviewed_geometry import production_height, dimension_note, image_dimension_note, family_dimension_note\n'
        if import_line not in source:
            source = source.replace('import sys\n','import sys\n'+import_line,1)
        source = source.replace('height=maxh if t<0 else round(maxh*(.08 if t==0 else .12+.88*t/24),1)',
                                'height=production_height(family,t,maxh)')
        family_anchor = "        if family=='town_hall':\n            family_text+="
        if "family_text += family_dimension_note(family)" not in source:
            source = source.replace(family_anchor,"        family_text += family_dimension_note(family) + '\\n\\n'\n"+family_anchor,1)
        spec_anchor = "            write_new(folder/'meshy_prompt_en.txt',primary)"
        if "spec += dimension_note(family,t)" not in source:
            source = source.replace(spec_anchor,"            spec += '\\n' + dimension_note(family,t) + '\\n'\n"+spec_anchor,1)
        source = source.replace('Progression: {progression}\nScale:', 'Progression: {progression}\n{image_dimension_note(family,t)}\nScale:')
        # The generator itself must participate in the same concurrent writer lock.
        source = source.replace("if __name__=='__main__':\n    main()", "if __name__=='__main__':\n    from ingest_generated_image import production_lock\n    with production_lock():\n        main()")
        compile(source,str(generator),'exec')
        writes = {}
        for family in {key[0] for key in CORRECTIONS} | {'cultivation_plot'}:
            path = art / 'Buildings' / family / 'family_spec.de.md'
            content = path.read_text(encoding='utf-8')
            for (f,index),(old_de,old_en,new_de,new_en) in CORRECTIONS.items():
                if f == family:
                    content = content.replace(f'- T{index:02d}: {old_de}.',f'- T{index:02d}: {new_de}.')
            note = family_dimension_note(family)
            if note and note not in content:
                content += '\n'+note+'\n'
            writes[path] = content
        changed_slots = []
        for slot in slots:
            family,tier = slot['building_id'],slot['tier_id']
            index = -1 if tier == 'T_FIXED' else int(tier[1:3])
            protected = slot['status'] == 'accepted' or (root/slot['reference_image']).exists()
            correction = CORRECTIONS.get((family,index))
            if correction and protected:
                raise ValueError(f'Review correction now has a protected image: {family}/{tier}')
            if correction:
                slot['visual_name_de'] = slot['visible_delta_from_previous'] = correction[2]
                changed_slots.append(family+'/'+tier)
            if family in ('residential_house','park'):
                maximum = 38 if family == 'residential_house' else 18
                slot['height_m'] = production_height(family,index,maximum)
            for field in ('asset_spec_file','meshy_prompt_file','image_prompt_file'):
                path = root / slot[field]
                if field == 'image_prompt_file' and protected:
                    continue
                content = path.read_text(encoding='utf-8')
                for (f,changed_index),(old_de,old_en,new_de,new_en) in CORRECTIONS.items():
                    if family != f:
                        continue
                    if field == 'asset_spec_file':
                        content = content.replace(old_de,new_de)
                    elif field == 'meshy_prompt_file':
                        if index == changed_index:
                            content = content.replace(old_en,new_en)
                    elif index >= changed_index:
                        content = content.replace(old_en,new_en)
                if field == 'asset_spec_file':
                    if family in ('residential_house','park'):
                        content = re.sub(r'(- Ungefähres belegtes Volumen: [\d.]+ × [\d.]+ × )[\d.]+( m)',
                                         lambda m: m[1]+f"{slot['height_m']:g}"+m[2],content)
                    note = dimension_note(family,index)
                    if note and note not in content:
                        content += '\n'+note+'\n'
                elif field == 'image_prompt_file':
                    note = image_dimension_note(family,index)
                    if note and note not in content:
                        content = content.replace('Scale:',note+'\nScale:',1)
                elif len(content.strip()) > 800:
                    raise ValueError(f'Updated Meshy prompt exceeds 800: {family}/{tier}')
                if content != path.read_text(encoding='utf-8'):
                    writes[path] = content
        # All calculations and guard checks precede the short write batch.
        generator.write_text(source,encoding='utf-8',newline='\n')
        for path,content in writes.items():
            path.write_text(content,encoding='utf-8',newline='\n')
        pipeline.atomic_json(manifest_path,manifest)
        matrix = art / 'BUILDING_TIER_MATRIX.csv'
        columns = next(csv.reader(matrix.open(encoding='utf-8-sig',newline='')))
        temporary = matrix.with_suffix('.csv.review.tmp')
        with temporary.open('w',encoding='utf-8',newline='') as stream:
            writer=csv.DictWriter(stream,fieldnames=columns)
            writer.writeheader();writer.writerows(slots)
        temporary.replace(matrix)
        legacy = root / 'Tools/build_production_data.py'
        archive = art / 'Archive/Unexecuted/legacy_build_production_data.py'
        if legacy.exists():
            if archive.exists():
                raise ValueError('Legacy archive already exists; source was not moved.')
            archive.parent.mkdir(parents=True,exist_ok=True)
            legacy.rename(archive)
        after = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
        assert before_status == {(s['building_id'],s['tier_id']):s['status'] for s in after['slots']}
        assert all(hashlib.sha256((root/path).read_bytes()).hexdigest()==digest for path,digest in accepted_prompts.items())
        report = pipeline.validate(root,data_only=True)
        pipeline.atomic_json(art/'Validation/review_corrections_validation.json',report)
        result = {'reviewed_delta_corrections':len(changed_slots),'changed_companion_and_family_files':len(writes),
                  'protected_accepted_image_prompts':len(accepted_prompts),'statuses_preserved':True,
                  'metadata_validation_passed':report['passed'],'errors':report['errors'],
                  'legacy_archived':archive.exists(),'affected_slots':changed_slots}
        pipeline.atomic_json(art/'Validation/review_corrections_result.json',result)
        print(json.dumps(result,ensure_ascii=False,indent=2))
        if not report['passed']:
            raise SystemExit(1)

if __name__ == '__main__':
    migrate()
