"""Record the explicit product decision without discarding experiment history."""
import json
from datetime import datetime, timezone
from pathlib import Path

task = Path(__file__).parent
root = task.resolve().parents[5]
now = datetime.now(timezone.utc).isoformat()
archive = 'Assets/Weedtopia/Art/Buildings/Archive/TownHall3D20260928'
report = task / 'Restore2D'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def write(path, data):
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')

receipt = read(report / 'restoration.json')
assert receipt['originalSpriteActive'] and receipt['noMeshDependencies']
approval = task / 'CityIntegration/approval.json'
history = report / 'Backup/3d_installation_approval.json'
if not history.exists():
    history.write_bytes(approval.read_bytes())
write(approval, {
    'approved': False,
    'date': '2026-09-30',
    'reason': 'User explicitly restored the original 2D sprite; no free camera planned. 3D experiment archived.',
    'previous_approval_preserved': str(history.relative_to(root)).replace('\\', '/'),
    'requires_new_explicit_authorization_to_reinstall': True,
})
progress = root / 'Docs/ArtProduction/PROGRESS.json'
p = read(progress)
p['updated_at_utc'] = now
p['status'] = '251_original_sprite_slots_active_release_gates_open'
p['active_delivery_strategy'] = 'isometric_2d_2_5d_city_with_fixed_camera_no_3d_building_exception'
p['active_production_visuals'] = {'sprite_slots': 251, 'mesh_lod_slots': 0}
p['camera_decision'] = {'date': '2026-09-30', 'free_camera_planned': False, 'source': 'explicit_user_decision'}
experiment = p['town_hall_highpoly_experiment_2026_09_28']
experiment['status'] = 'completed_and_archived_by_product_decision_original_2d_sprite_restored'
experiment['scope'] = 'historical_single_town_hall_3d_experiment_no_active_production_exception'
experiment['active_in_production'] = False
experiment['archive'] = archive
experiment['restoration'] = str((report/'README.de.md').relative_to(root)).replace('\\','/')
experiment['city_lighting']['status'] = 'user_reported_improvement_experiment_now_archived'
write(progress, p)
progress = task / 'Optimization_v1/PROGRESS.json'
p = read(progress)
p['status'] = 'completed_experiment_archived_original_2d_sprite_restored'
p['updated_utc'] = now
p['active_in_production'] = False
p['archive'] = archive
p['steps']['city_integration'] = 'historical_success_reverted_to_original_sprite_by_user_request'
p['steps']['restore_original_2d'] = 'applied_validation_in_progress'
p['city_lighting']['status'] = 'user_reported_improvement_experiment_now_archived'
p['resume'] = 'No further 3D work for Weedtopia. Preserve optimized models, rebakes and reviews. Fixed camera and original 2D/2.5D buildings are the chosen production direction. Do not reinstall 3D without new explicit user authorization.'
write(progress, p)
print('Recorded fixed-camera 2D decision and disabled historical 3D installation approval.')
