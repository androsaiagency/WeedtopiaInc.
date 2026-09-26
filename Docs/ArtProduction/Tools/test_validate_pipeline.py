"""Regression tests use disposable synthetic fixtures outside production asset folders."""
import argparse
import copy
import csv
import importlib.util
import json
import contextlib
import io
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

from PIL import Image

SPEC = importlib.util.spec_from_file_location("pipeline", Path(__file__).with_name("validate_pipeline.py"))
pipeline = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(pipeline)
sys.path.insert(0, str(Path(__file__).parent))
sys.modules['validate_pipeline'] = pipeline
import prepare_image_job as prepare
import ingest_generated_image as ingest
import prepare_showcase_job as showcase


class PipelineTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="weedtopia-validator-test-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.slots = []
        for family in pipeline.ALL_FAMILIES:
            tiers = pipeline.TIERS if family in pipeline.FAMILIES else ("T_FIXED",)
            for index, tier in enumerate(tiers):
                folder = f"{pipeline.ART}/Buildings/{family}/{tier}"
                low = 0 if index == 0 else pipeline.LEVELS[index - 1]
                high = 0 if index == 0 else (pipeline.LEVELS[index] - 1 if index < 24 else 5000)
                if family == "town_hall":
                    high = 5000
                slot = {"building_id": family, "building_name_de": family, "tier_id": tier,
                        "min_level": low, "max_level": high, "era": "Test-Epoche",
                        "visual_name_de": "Test", "visible_delta_from_previous": f"Module {index}",
                        "footprint_m_x": 12, "footprint_m_z": 12, "height_m": 5,
                        "target_faces": 8000,
                        "driver_slot_delta": int(family in ("farm_warehouse", "city_warehouse") and index > 0),
                        "status": "pending_image",
                        "unity_incoming_folder": f"Assets/Weedtopia/Art/Buildings/Incoming/{family}/{tier}",
                        "unity_prefab_target": f"Assets/Weedtopia/Art/Buildings/Prefabs/{family}/WT_{family}_{tier}.prefab"}
                for field, filename in pipeline.PATH_FIELDS.items():
                    if filename:
                        slot[field] = f"{folder}/{filename}"
                directory = self.root / folder
                directory.mkdir(parents=True)
                prompt = (f"Building {tier}; broad rounded form; plaster, wood; stylized; game-ready, smart-topology, "
                          "clean edges, minimal geometry, single complete asset, centered, grounded, front facing +Z; "
                          "no floating parts, people, scenes, landscapes, text, logos, vehicles, loose props, diorama ground, cropped geometry.")
                (directory / "meshy_prompt_en.txt").write_text(prompt, encoding="utf-8")
                (directory / "texture_prompt_en.txt").write_text("Sage plaster, warm wood, matte roughness.", encoding="utf-8")
                (directory / "image_prompt_en.txt").write_text(f"Actual test generation prompt {family} {tier}", encoding="utf-8")
                (directory / "asset_spec.de.md").write_text("Funktion und Test-Silhouette, maximale Stellflaeche 12 m.", encoding="utf-8")
                pipeline.atomic_json(directory / "meshy_settings.json", {**pipeline.SETTINGS, "target_faces": 8000})
                (self.root / slot["unity_incoming_folder"]).mkdir(parents=True)
                self.slots.append(slot)
            (self.root / pipeline.ART / "Buildings" / family / "family_spec.de.md").write_text("Test-Familie", encoding="utf-8")
        for doc in ("ART_DIRECTION.md", "MESHY_WORKFLOW.md"):
            (self.root / pipeline.ART / doc).write_text("Disposable validation fixture", encoding="utf-8")
        self.write_data()

    def write_data(self):
        pipeline.atomic_json(self.root / pipeline.MANIFEST, {"schemaVersion": 1, "slots": self.slots})
        with (self.root / pipeline.MATRIX).open("w", encoding="utf-8", newline="") as stream:
            writer = csv.DictWriter(stream, fieldnames=list(self.slots[0]))
            writer.writeheader()
            writer.writerows(self.slots)

    def codes(self, report):
        return {error["code"] for error in report["errors"]}

    def png(self, index=1, size=(1024, 1024), color=(90, 130, 40)):
        path = self.root / self.slots[index]["reference_image"]
        Image.new("RGB", size, color).save(path)
        return path

    def accept_args(self, index=1):
        slot = self.slots[index]
        return argparse.Namespace(command="accept", slot=pipeline.slot_key(slot), showcase=None,
                                  notes="Inspected test fixture: expected single asset; tests only.",
                                  source_image=str(self.root / slot["reference_image"]),
                                  prompt_file=str(self.root / slot["image_prompt_file"]), reference=[])

    def test_metadata_success_does_not_accept_missing_art(self):
        report = pipeline.validate(self.root, data_only=True)
        self.assertTrue(report["passed"], report["errors"][:3])
        progress = pipeline.sync_progress(self.root, report)
        self.assertEqual(progress["expected_slots"], 251)
        self.assertEqual(progress["generated_slots"], 0)
        self.assertEqual(progress["failed_slots"], 0)
        self.assertEqual(progress["next_slot"], "cultivation_plot/T01_L001")
        self.assertFalse(progress["art_acceptance_complete"])
        self.assertFalse(pipeline.validate(self.root)["passed"])

    def test_unsafe_paths_are_rejected_without_file_access(self):
        for value in ("../escape", "C:/escape", "/escape", "a\\b", "a//b", "a/./b", "a/../b",
                      "https://host/x", "a/file. ", "a/NUL.png"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                pipeline.safe_path(self.root, value)
        self.slots[0]["asset_spec_file"] = "../not-in-project.md"
        self.write_data()
        self.assertIn("unsafe_path", self.codes(pipeline.validate(self.root, True)))

    def test_level_and_driver_contracts(self):
        self.slots[2]["min_level"] = 6
        self.slots[0]["driver_slot_delta"] = 1
        self.slots[-1]["max_level"] = 4999
        self.write_data()
        codes = self.codes(pipeline.validate(self.root, True))
        self.assertTrue({"level_band", "driver_delta", "fixed_band"}.issubset(codes))

    def test_matrix_mismatch_and_duplicate_identity(self):
        manifest = {"schemaVersion": 1, "slots": copy.deepcopy(self.slots)}
        manifest["slots"][0]["visible_delta_from_previous"] = "Changed only in JSON"
        manifest["slots"][1] = copy.deepcopy(manifest["slots"][0])
        pipeline.atomic_json(self.root / pipeline.MANIFEST, manifest)
        codes = self.codes(pipeline.validate(self.root, True))
        self.assertTrue({"matrix_mismatch", "duplicate_slot", "slot_set", "duplicate_path"}.issubset(codes))

    def test_face_budget_and_main_prompt_constraints(self):
        self.slots[0]["target_faces"] = 15001
        self.write_data()
        path = self.root / self.slots[0]["meshy_prompt_file"]
        path.write_text("x" * 801, encoding="utf-8")
        codes = self.codes(pipeline.validate(self.root, True))
        self.assertTrue({"face_budget", "prompt_length", "prompt_term", "prompt_exclusion", "settings_budget"}.issubset(codes))

    def test_duplicate_images_fail_both_slots(self):
        self.png(1)
        self.png(26)
        report = pipeline.validate(self.root, True)
        self.assertIn("duplicate_image", self.codes(report))
        self.assertEqual(report["counts"]["failed_slots"], 2)
        self.assertEqual(report["counts"]["validated_slots"], 0)

    def test_real_image_decode_dimensions_and_corrupt_stream(self):
        self.png(1, size=(1024, 800))
        image = self.png(26)
        data = image.read_bytes()
        image.write_bytes(data[:100])
        report = pipeline.validate(self.root, True)
        invalid = [error for error in report["errors"] if error["code"] == "invalid_image"]
        self.assertEqual(len(invalid), 2)

    def test_accept_is_hash_bound_and_idempotent(self):
        self.png()
        args = self.accept_args()
        progress = pipeline.record_qa(self.root, args)
        self.assertEqual(progress["validated_slots"], 1)
        self.assertEqual(progress["next_slot"], "farm_processing/T01_L001")
        self.assertEqual(pipeline.load_manifest(self.root)["slots"][1]["status"], "accepted")
        self.assertTrue(pipeline.validate(self.root, True)["passed"])
        ledger_bytes = (self.root / pipeline.QA).read_bytes()
        pipeline.record_qa(self.root, args)
        self.assertEqual((self.root / pipeline.QA).read_bytes(), ledger_bytes)
        self.png(color=(40, 20, 100))
        report = pipeline.validate(self.root, True)
        self.assertIn("accepted_file_changed", self.codes(report))
        self.assertEqual(report["counts"]["validated_slots"], 0)
        with self.assertRaisesRegex(ValueError, "Accepted data changed"):
            pipeline.record_qa(self.root, args)

    def test_regenerated_rejected_image_can_be_accepted_without_bypassing_validation(self):
        self.png(1)
        args = self.accept_args(1)
        args.command = "reject"
        pipeline.record_qa(self.root, args)
        self.png(1, color=(20, 50, 80))
        args.command = "accept"
        progress = pipeline.record_qa(self.root, args)
        self.assertEqual(progress["validated_slots"], 1)
        self.assertEqual(progress["failed_slots"], 0)
        ledger = pipeline.read_json(self.root / pipeline.QA)
        self.assertEqual(ledger["entries"][args.slot]["previous_record"]["status"], "rejected")

    def test_accepted_prompt_tamper_invalidates_qa(self):
        self.png()
        pipeline.record_qa(self.root, self.accept_args())
        (self.root / self.slots[1]["image_prompt_file"]).write_text("Different actual prompt", encoding="utf-8")
        report = pipeline.validate(self.root, True)
        self.assertIn("accepted_file_changed", self.codes(report))
        self.assertEqual(report["counts"]["validated_slots"], 0)

    def test_accepted_reference_tamper_invalidates_qa(self):
        self.png()
        reference = self.root / "reference.png"
        Image.new("RGB", (4, 4), (1, 2, 3)).save(reference)
        args = self.accept_args()
        args.reference = [str(reference)]
        pipeline.record_qa(self.root, args)
        Image.new("RGB", (4, 4), (4, 5, 6)).save(reference)
        report = pipeline.validate(self.root, True)
        self.assertIn("accepted_file_changed", self.codes(report))

    def test_missing_companion_blocks_acceptance_without_counting_absent_images_failed(self):
        self.png()
        (self.root / self.slots[1]["asset_spec_file"]).unlink()
        with self.assertRaisesRegex(ValueError, "Cannot accept invalid slot data"):
            pipeline.record_qa(self.root, self.accept_args())
        report = pipeline.validate(self.root, True)
        self.assertEqual(report["counts"]["failed_slots"], 1)
        self.assertEqual(report["counts"]["generated_slots"], 1)

    def test_status_projection_repairs_interrupted_csv_status_update(self):
        self.png()
        pipeline.record_qa(self.root, self.accept_args())
        self.write_data()  # Simulates another producer retaining stale pending statuses.
        report = pipeline.validate(self.root, True)
        pipeline.sync_progress(self.root, report)
        self.assertEqual(pipeline.load_manifest(self.root)["slots"][1]["status"], "accepted")
        self.assertTrue(pipeline.validate(self.root, True)["passed"])

    def test_unaccepted_predecessor_cannot_be_bypassed(self):
        self.png(2)
        args = self.accept_args(2)
        with self.assertRaisesRegex(ValueError, "required accepted reference"):
            pipeline.record_qa(self.root, args)
        self.assertFalse((self.root / pipeline.QA).exists())

    def test_source_provenance_and_qa_notes_required(self):
        self.png()
        args = self.accept_args()
        args.notes = " "
        with self.assertRaisesRegex(ValueError, "QA notes"):
            pipeline.record_qa(self.root, args)
        args.notes = "Reviewed fixture"
        args.source_image = str(self.root / "missing-output.png")
        with self.assertRaisesRegex(ValueError, "Source tool output"):
            pipeline.record_qa(self.root, args)

    def test_atomic_write_does_not_destroy_previous_progress_on_failure(self):
        path = self.root / pipeline.PROGRESS
        pipeline.atomic_json(path, {"value": "original"})
        with patch.object(pipeline.os, "replace", side_effect=OSError("simulated interruption")):
            with self.assertRaises(OSError):
                pipeline.atomic_json(path, {"value": "new"})
        self.assertEqual(pipeline.read_json(path), {"value": "original"})
        self.assertEqual(list(path.parent.glob(f".{path.name}.*.tmp")), [])

    def test_obsolete_layout_override_cannot_replace_mandatory_predecessor(self):
        first = self.png(1)
        pipeline.record_qa(self.root, self.accept_args(1))
        second = self.png(2, color=(3, 4, 5))
        second_args = self.accept_args(2)
        second_args.reference = [str(first)]
        pipeline.record_qa(self.root, second_args)
        self.png(3, color=(5, 6, 7))
        args = self.accept_args(3)
        args.reference = [str(first)]
        args.layout_slot = args.slot.split('/')[0] + '/T01_L001'  # Old request metadata cannot bypass the invariant.
        with self.assertRaisesRegex(ValueError, 'T02_L005/mesh_input_3q.png'):
            pipeline.record_qa(self.root, args)
        args.reference.append(str(second))
        pipeline.record_qa(self.root, args)
        entry = pipeline.read_json(self.root / pipeline.QA)['entries'][args.slot]
        self.assertNotIn('layout_reference_slot', entry)
        obsolete = copy.deepcopy(entry)
        obsolete['layout_reference_slot'] = 'cultivation_plot/T01_L001'
        obsolete['references'] = [r for r in obsolete['references'] if '/T02_L005/' not in r['path']]
        self.assertFalse(pipeline.qa_valid(self.root, obsolete, self.root / self.slots[3]['reference_image'],
                                           self.root / self.slots[3]['image_prompt_file']))

    def make_styles(self):
        folder = self.root / pipeline.ART / 'References/ChosenStyle'
        folder.mkdir(parents=True, exist_ok=True)
        for index, name in enumerate(('Reclaimed_Green_Haven.png', 'Herbal_Atom_Age.png', 'Burning_Hills_Afterglow.png')):
            Image.new('RGB', (4, 4), (index, index, index)).save(folder / name)

    def prepare_fixture(self, index=1, predecessor_first=False):
        self.make_styles()
        argv = ['--slot', pipeline.slot_key(self.slots[index])]
        if predecessor_first:
            argv.append('--predecessor-first')
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            prepare.run(argv, self.root)
        return json.loads(output.getvalue())

    def test_predecessor_first_preserves_exact_mandatory_reference_set(self):
        first = self.png(1)
        pipeline.record_qa(self.root, self.accept_args(1))
        second = self.png(2, color=(3, 4, 5))
        args = self.accept_args(2)
        args.reference = [str(first)]
        pipeline.record_qa(self.root, args)
        default = self.prepare_fixture(3)
        reordered = self.prepare_fixture(3, predecessor_first=True)
        self.assertEqual(set(default['referenced_image_paths']), set(reordered['referenced_image_paths']))
        self.assertEqual(len(reordered['referenced_image_paths']), 5)
        self.assertEqual(reordered['referenced_image_paths'][:2], [str(second), str(first)])
        self.assertEqual(default['reference_hashes'], reordered['reference_hashes'])
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            prepare.main(['--slot', 'cultivation_plot/T03_L010', '--layout-slot', 'cultivation_plot/T01_L001'], self.root)

    def test_ingestion_resume_requires_identical_existing_file_and_unchanged_references(self):
        job = self.prepare_fixture()
        target = self.png(1)
        source = self.root / 'original-output.png'
        source.write_bytes(target.read_bytes())
        argv = ['--slot', pipeline.slot_key(self.slots[1]), '--source', str(source), '--notes', 'Verified test fixture']
        with contextlib.redirect_stdout(io.StringIO()):
            ingest.run(argv, self.root)
        self.assertEqual(pipeline.read_json(self.root / pipeline.QA)['entries'][job['key']]['status'], 'accepted')
        # A different existing image must be protected before acceptance, even when resume is allowed.
        pipeline.atomic_json(self.root / pipeline.QA, {'schemaVersion': 1, 'entries': {}})
        self.write_data()
        Image.new('RGB', (1024, 1024), (123, 34, 56)).save(source)
        original = target.read_bytes()
        with self.assertRaisesRegex(ValueError, 'Existing output is protected'):
            ingest.run(argv, self.root)
        self.assertEqual(target.read_bytes(), original)
        source.write_bytes(original)
        reference = Path(job['referenced_image_paths'][0])
        Image.new('RGB', (4, 4), (90, 91, 92)).save(reference)
        with self.assertRaisesRegex(ValueError, 'reference hashes changed'):
            ingest.run(argv, self.root)
        self.assertEqual(target.read_bytes(), original)

    def test_reopen_requires_explicit_rejection_and_archives_accepted_data(self):
        job = self.prepare_fixture()
        target = self.png(1)
        source = self.root / 'original-output.png'
        source.write_bytes(target.read_bytes())
        argv = ['--slot', job['key'], '--source', str(source), '--notes', 'Test visual review']
        with contextlib.redirect_stdout(io.StringIO()):
            ingest.run(argv, self.root)
        with self.assertRaisesRegex(ValueError, 'Accepted slot is protected'):
            ingest.run(argv + ['--reject'], self.root)
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            ingest.run(argv + ['--reopen'], self.root)
        # A changed proposed prompt must not stop deliberate withdrawal; archive both current and actual prepared prompt.
        (self.root / self.slots[1]['image_prompt_file']).write_text('Proposed replacement prompt', encoding='utf-8')
        with contextlib.redirect_stdout(io.StringIO()):
            ingest.run(argv + ['--reject', '--reopen'], self.root)
        self.assertFalse(target.exists())
        archive = self.root / pipeline.ART / 'Validation/Rejected/cultivation_plot/T01_L001'
        self.assertEqual((archive / 'attempt_001.png').read_bytes(), source.read_bytes())
        self.assertEqual((archive / 'attempt_001_prepared_prompt_en.txt').read_text(encoding='utf-8'), job['prompt'])
        self.assertEqual((archive / 'attempt_001_prompt_en.txt').read_text(encoding='utf-8'), 'Proposed replacement prompt')
        ledger = pipeline.read_json(self.root / pipeline.QA)['entries'][job['key']]
        self.assertEqual(ledger['status'], 'rejected')
        self.assertEqual(ledger['previous_record']['status'], 'accepted')

    @unittest.skipUnless(sys.platform == 'win32', 'Production lock uses the Windows OS file-lock API')
    def test_concurrent_prepare_waits_for_production_lock(self):
        self.make_styles()
        code = ('import sys; from pathlib import Path; sys.path.insert(0,sys.argv[1]); '
                'import prepare_image_job as p; (Path(sys.argv[2])/"prepare_started.marker").write_text("started"); '
                'p.run(["--slot","cultivation_plot/T01_L001"],Path(sys.argv[2]))')
        request = self.root / pipeline.ART / 'Validation/ImageRequests/cultivation_plot/T01_L001.json'
        with ingest.production_lock(self.root):
            process = subprocess.Popen([sys.executable, '-c', code, str(Path(__file__).parent.resolve()), str(self.root)],
                                       stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
            try:
                marker = self.root / 'prepare_started.marker'
                deadline = time.monotonic() + 5
                while not marker.exists() and time.monotonic() < deadline:
                    time.sleep(0.02)
                self.assertTrue(marker.exists(), 'Child process did not reach prepare')
                time.sleep(0.2)
                self.assertIsNone(process.poll(), 'Prepare exited while another producer held the lock')
                self.assertFalse(request.exists(), 'Prepare wrote a request without acquiring the production lock')
            except BaseException:
                process.terminate()
                process.communicate(timeout=5)
                raise
        stdout, stderr = process.communicate(timeout=15)
        self.assertEqual(process.returncode, 0, stderr)
        self.assertEqual(json.loads(stdout)['key'], 'cultivation_plot/T01_L001')
        self.assertTrue(request.is_file())

    def test_showcase_uses_three_exact_tiers_and_two_styles_within_five_input_limit(self):
        self.make_styles()
        for slot in self.slots[:25]:
            slot['status'] = 'accepted'
        self.write_data()
        for index in (1, 12, 24):
            self.png(index, color=(index, 2, 3))
        prompt = self.root / pipeline.ART / 'Buildings/cultivation_plot/family_showcase_prompt_en.txt'
        prompt.write_text('Fixture showcase prompt', encoding='utf-8')
        # Isolate reference selection from visual acceptance, which has separate integration coverage.
        with patch.object(pipeline, 'qa_valid', return_value=True), contextlib.redirect_stdout(io.StringIO()) as output:
            showcase.run(['--family', 'cultivation_plot'], self.root)
        job = json.loads(output.getvalue())
        refs = [Path(value) for value in job['referenced_image_paths']]
        self.assertEqual(len(refs), 5)
        self.assertEqual({value.parent.name for value in refs[2:]}, {'T01_L001', 'T12_L600', 'T24_L5000'})
        self.assertEqual({value.name for value in refs[:2]}, {'Reclaimed_Green_Haven.png', 'Burning_Hills_Afterglow.png'})
        self.assertEqual(len(job['reference_hashes']), 5)


class ValidationMemoTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='weedtopia-validation-memo-test-')
        self.addCleanup(self.temp.cleanup)
        self.image = Path(self.temp.name) / 'asset.png'
        Image.new('RGB', (1024, 1024), (40, 80, 120)).save(self.image)

    def test_repeated_reads_reuse_only_current_validation_scope(self):
        @pipeline._validation_scope
        def probe():
            first = pipeline.decode_png(self.image)
            second = pipeline.decode_png(self.image)
            self.assertEqual(first, second)
            self.assertEqual(pipeline.sha256(self.image), first['sha256'])
            # A caller cannot modify the memo by modifying its returned object.
            second['width'] = 1
            self.assertEqual(pipeline.decode_png(self.image)['width'], 1024)

        with patch.object(pipeline.hashlib, 'file_digest', wraps=pipeline.hashlib.file_digest) as digest, \
             patch.object(Image, 'open', wraps=Image.open) as image_open:
            probe()
            self.assertEqual(digest.call_count, 1)
            self.assertEqual(image_open.call_count, 2)
            probe()
            self.assertEqual(digest.call_count, 2, 'A new validation must rehash actual bytes')
            self.assertEqual(image_open.call_count, 4, 'A new validation must fully decode again')
        self.assertIsNone(pipeline._VALIDATION_MEMO.get())

    def test_changed_file_and_dimension_mode_cannot_reuse_cached_acceptance(self):
        @pipeline._validation_scope
        def probe():
            original = pipeline.decode_png(self.image)
            Image.new('RGB', (1024, 1024), (120, 80, 40)).save(self.image)
            changed = pipeline.decode_png(self.image)
            self.assertNotEqual(original['sha256'], changed['sha256'])
            with self.assertRaisesRegex(ValueError, 'landscape'):
                pipeline.decode_png(self.image, showcase=True)
            self.image.write_bytes(b'not a PNG')
            with self.assertRaises(Exception):
                pipeline.decode_png(self.image)
        probe()

    def test_exception_discards_scope_and_standalone_hashes_remain_uncached(self):
        @pipeline._validation_scope
        def failing_probe():
            pipeline.sha256(self.image)
            raise ValueError('intentional validation failure')
        with self.assertRaisesRegex(ValueError, 'intentional'):
            failing_probe()
        self.assertIsNone(pipeline._VALIDATION_MEMO.get())
        with patch.object(pipeline.hashlib, 'file_digest', wraps=pipeline.hashlib.file_digest) as digest:
            self.assertEqual(pipeline.sha256(self.image), pipeline.sha256(self.image))
            self.assertEqual(digest.call_count, 2)


if __name__ == "__main__":
    unittest.main(verbosity=2)
