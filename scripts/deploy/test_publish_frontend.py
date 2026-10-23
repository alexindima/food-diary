import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import textwrap
import unittest
from unittest.mock import patch

import publish_frontend as publication


class PublicationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.base = Path(self.temporary.name).resolve()
        self.root = self.base / "site"
        self.a = "a" * 40
        self.b = "b" * 40

    def build(self, label, *, worker=True):
        source = self.base / f"build-{label}"
        source.mkdir()
        files = {
            "index.html": f"<html>public-{label}</html>",
            "index.csr.html": f"<html>app-{label}</html>",
            f"chunk-{label * 8}.js": f"lazy-{label}",
            f"styles-{label * 8}.css": f"style-{label}",
            f"media/font-{label * 8}.woff2": f"font-{label}",
            "assets/i18n/en.json": f'{{"release":"{label}"}}',
            "ngsw-worker.js": f"worker-{label}",
        }
        for name, content in files.items():
            (source / name).parent.mkdir(parents=True, exist_ok=True)
            (source / name).write_bytes(content.encode())
        if worker:
            manifest = {
                "index": "/index.csr.html",
                "hashTable": {f"/{name}": hashlib.sha1(content.encode()).hexdigest()
                              for name, content in files.items() if name != "ngsw-worker.js"},
            }
            (source / "ngsw.json").write_text(json.dumps(manifest), encoding="utf-8")
        return source

    def publish(self, source, identity):
        return publication.publish(source, self.root, identity, require_ngsw=True)

    def test_publish_a_b_keeps_exact_shell_manifest_and_old_lazy_assets(self):
        first, second = self.build("A"), self.build("b")
        self.publish(first, self.a)
        root_inode = self.root.stat().st_ino
        old_bytes = (first / "chunk-AAAAAAAA.js").read_bytes()
        self.publish(second, self.b)
        self.assertEqual(self.root.stat().st_ino, root_inode)
        self.assertEqual(publication.pointer(self.root, "current"), self.b)
        self.assertEqual(publication.pointer(self.root, "previous"), self.a)
        for name in ("index.html", "index.csr.html", "ngsw.json", "ngsw-worker.js"):
            self.assertEqual((self.root / "current" / name).read_bytes(), (second / name).read_bytes())
            self.assertFalse((self.root / "retained-assets" / name).exists())
        self.assertFalse((self.root / "retained-assets/assets/i18n/en.json").exists())
        self.assertEqual((self.root / "retained-assets/chunk-AAAAAAAA.js").read_bytes(), old_bytes)
        # Old Nginx still sees its stable flat root during first-deploy SSR restart.
        self.assertEqual((self.root / "chunk-bbbbbbbb.js").read_bytes(), (second / "chunk-bbbbbbbb.js").read_bytes())
        self.assertEqual((self.root / "retained-assets/media/font-AAAAAAAA.woff2").read_bytes(), b"font-A")

    def test_legacy_migration_preserves_flat_shell_and_captures_rollback(self):
        legacy, second = self.build("A"), self.build("b")
        publication.copy_snapshot(legacy, publication.absolute_directory(self.root, create=True), publication.tree(legacy))
        self.publish(second, self.b)
        previous = publication.pointer(self.root, "previous")
        self.assertRegex(previous, r"^sha256-[0-9a-f]{64}$")
        self.assertEqual((self.root / "index.html").read_bytes(), (legacy / "index.html").read_bytes())
        self.assertEqual((self.root / "ngsw.json").read_bytes(), (legacy / "ngsw.json").read_bytes())
        self.assertEqual((self.root / "retained-assets/chunk-AAAAAAAA.js").read_bytes(), b"lazy-A")
        publication.rollback(self.root)
        self.assertEqual((self.root / "current/index.html").read_bytes(), (legacy / "index.html").read_bytes())

    def test_rollback_to_a_preserves_b_assets(self):
        first, second = self.build("A"), self.build("b")
        self.publish(first, self.a)
        self.publish(second, self.b)
        publication.rollback(self.root)
        self.assertEqual(publication.pointer(self.root, "current"), self.a)
        self.assertEqual(publication.pointer(self.root, "previous"), self.b)
        self.assertEqual((self.root / "current/ngsw.json").read_bytes(), (first / "ngsw.json").read_bytes())
        self.assertEqual((self.root / "retained-assets/chunk-bbbbbbbb.js").read_bytes(), b"lazy-b")

    def test_public_rollback_rejects_missing_manifest(self):
        first, second = self.build("A"), self.build("b")
        self.publish(first, self.a)
        self.publish(second, self.b)
        (self.root / "releases" / self.a / "ngsw.json").unlink()
        with self.assertRaisesRegex(publication.PublicationError, "must contain ngsw.json"):
            publication.rollback(self.root, require_ngsw=True)
        self.assertEqual(publication.pointer(self.root, "current"), self.b)

    def test_failed_copy_does_not_change_current(self):
        first, second = self.build("A"), self.build("b")
        self.publish(first, self.a)
        original_copy = publication.shutil.copyfile
        calls = 0

        def partial_copy(*args, **kwargs):
            nonlocal calls
            calls += 1
            if calls == 2:
                raise OSError("disk full")
            return original_copy(*args, **kwargs)

        with patch.object(publication.shutil, "copyfile", side_effect=partial_copy):
            with self.assertRaises(OSError):
                self.publish(second, self.b)
        self.assertEqual(calls, 2)
        self.assertEqual(publication.pointer(self.root, "current"), self.a)
        self.assertFalse((self.root / "releases" / self.b).exists())

    def test_admin_without_service_worker_and_public_missing_manifest(self):
        source = self.build("A", worker=False)
        with self.assertRaisesRegex(publication.PublicationError, "must contain ngsw.json"):
            self.publish(source, self.a)
        publication.publish(source, self.root, self.a)
        self.assertEqual((self.root / "current/index.html").read_bytes(), (source / "index.html").read_bytes())

    def test_symlink_created_during_copy_is_rejected_before_chmod(self):
        first, second = self.build("A"), self.build("b")
        self.publish(first, self.a)
        outside = self.base / "outside.txt"
        outside.write_text("outside", encoding="utf-8")
        outside.chmod(0o600)

        def changing_source(source, target, **kwargs):
            os.symlink(outside, target)

        with patch.object(publication.shutil, "copyfile", side_effect=changing_source):
            with self.assertRaisesRegex(publication.PublicationError, "changed during copy"):
                self.publish(second, self.b)
        self.assertEqual(outside.stat().st_mode & 0o777, 0o600)
        self.assertEqual(publication.pointer(self.root, "current"), self.a)

    def test_corrupt_manifest_does_not_change_current(self):
        first, second = self.build("A"), self.build("b")
        self.publish(first, self.a)
        (second / "index.csr.html").write_text("mismatch", encoding="utf-8")
        with self.assertRaisesRegex(publication.PublicationError, "content mismatch"):
            self.publish(second, self.b)
        self.assertEqual(publication.pointer(self.root, "current"), self.a)

    def test_manifest_path_traversal_does_not_change_current(self):
        first, second = self.build("A"), self.build("b")
        self.publish(first, self.a)
        manifest = json.loads((second / "ngsw.json").read_text(encoding="utf-8"))
        manifest["hashTable"]["/../outside"] = "0" * 40
        (second / "ngsw.json").write_text(json.dumps(manifest), encoding="utf-8")
        with self.assertRaisesRegex(publication.PublicationError, "Unsafe relative"):
            self.publish(second, self.b)
        self.assertEqual(publication.pointer(self.root, "current"), self.a)

    def test_retention_collision_does_not_change_current(self):
        first, second = self.build("A"), self.build("b")
        self.publish(first, self.a)
        (self.root / "chunk-bbbbbbbb.js").write_text("different bytes", encoding="utf-8")
        with self.assertRaisesRegex(publication.PublicationError, "collision"):
            self.publish(second, self.b)
        self.assertEqual(publication.pointer(self.root, "current"), self.a)

    def test_same_release_id_is_idempotent_but_rejects_different_content(self):
        first, second = self.build("A"), self.build("b")
        self.publish(first, self.a)
        self.publish(first, self.a)
        with self.assertRaisesRegex(publication.PublicationError, "different bytes"):
            self.publish(second, self.a)
        self.assertEqual(publication.pointer(self.root, "current"), self.a)

    def test_traversal_release_ids_are_rejected(self):
        source = self.build("A")
        for identity in ("../escape", "/tmp/escape", self.a + "/..", "A" * 40, "latest"):
            with self.subTest(identity=identity), self.assertRaises(publication.PublicationError):
                self.publish(source, identity)
        self.assertFalse((self.root / "current").exists())

    def test_source_symlink_and_destination_symlink_escape_are_rejected(self):
        source = self.build("A")
        outside = self.base / "outside"
        outside.mkdir()
        (outside / "secret").write_text("outside", encoding="utf-8")
        os.symlink(outside / "secret", source / "escape.js")
        with self.assertRaises(publication.PublicationError):
            self.publish(source, self.a)
        (source / "escape.js").unlink()
        self.root.mkdir(exist_ok=True)
        os.symlink(outside, self.root / "releases", target_is_directory=True)
        with self.assertRaises(publication.PublicationError):
            self.publish(source, self.a)
        self.assertEqual(sorted(path.name for path in outside.iterdir()), ["secret"])

    def test_current_pointer_escape_is_rejected(self):
        source = self.build("A")
        self.root.mkdir()
        os.symlink("../build-A", self.root / "current", target_is_directory=True)
        with self.assertRaisesRegex(publication.PublicationError, "Unsafe current"):
            self.publish(source, self.a)
        self.assertEqual(os.readlink(self.root / "current"), "../build-A")

    def test_only_known_content_hash_asset_names_are_retained(self):
        accepted = ["chunk-20jbkJr8.js", "chunk-B6gy2_0O.js", "media/a-BKXr_Th0.webp", "main.1234567890abcdef.js"]
        rejected = ["index.html", "ngsw.json", "ngsw-worker.js", "worker-basic.min.js", "config-ABCDEFGH.json", "assets/logo.svg", "chunk-ABC.js"]
        for name in accepted:
            self.assertIsNotNone(publication.HASHED_ASSET.search(name), name)
        for name in rejected:
            self.assertIsNone(publication.HASHED_ASSET.search(name), name)

    def test_production_nginx_fallback_only_matches_retained_asset_allowlist(self):
        configuration = (Path(__file__).resolve().parents[2] / "nginx/sites-enabled/fooddiary.club").read_text(encoding="utf-8")
        patterns = re.findall(r"location ~ (\S+) \{\s*# Retained Angular assets[\s\S]*?try_files \$uri @(\w+);", configuration)
        self.assertEqual(len(patterns), 2)
        for pattern, fallback in patterns:
            matcher = re.compile(pattern.strip('"'))
            self.assertIsNotNone(matcher.search("/media/font-BKXr_Th0.woff2"))
            for url in ("/ngsw.json", "/ngsw-worker.js", "/index.html", "/config-ABCDEFGH.json"):
                self.assertIsNone(matcher.search(url), url)
            named = re.search(r"location @" + fallback + r" \{([^}]+)\}", configuration)
            self.assertIsNotNone(named)
            self.assertIn("/retained-assets;", named[1])
            self.assertIn("try_files $uri =404;", named[1])

    def test_workflow_publishes_before_ssr_and_never_wipes_live_roots(self):
        workflow = (Path(__file__).resolve().parents[2] / ".github/workflows/deploy.yml").read_text(encoding="utf-8")
        self.assertLess(workflow.index('publish_frontend.py", "publish"'), workflow.index("--no-deps client-ssr"))
        self.assertNotIn("rm -rf /var/www/fooddiary.club", workflow)
        self.assertNotIn("rm -rf /var/www/admin.fooddiary.club", workflow)
        self.assertIn("--require-ngsw", workflow)

    def test_actual_workflow_uses_immutable_image_digest_for_both_sites(self):
        workflow = (Path(__file__).resolve().parents[2] / ".github/workflows/deploy.yml").read_text(encoding="utf-8")
        self.assertIn('python3 - "$id" "$CLIENT_IMAGE_REF"', workflow)
        script = textwrap.dedent(workflow.split("<<'PYTHON_EOF'\n", 1)[1].split("          PYTHON_EOF", 1)[0])
        # Execute the actual workflow program; only Docker/publisher processes are mocked.
        for digest in ("1" * 64, "2" * 64):
            with self.subTest(digest=digest), patch.object(sys, "argv", ["-", "container-id", f"client@sha256:{digest}"]), patch.object(subprocess, "run") as runner:
                exec(compile(script, "deploy.yml:frontend-publication", "exec"), {})
                commands = [call.args[0] for call in runner.call_args_list]
                self.assertEqual(len(commands), 4)
                for command in (commands[1], commands[3]):
                    self.assertEqual(command[command.index("--release-id") + 1], f"sha256-{digest}")
                self.assertIn("--require-ngsw", commands[1])
                self.assertNotIn("--require-ngsw", commands[3])


if __name__ == "__main__":
    unittest.main()
