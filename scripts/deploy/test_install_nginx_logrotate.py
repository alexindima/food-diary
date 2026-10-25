import os
from pathlib import Path
import tempfile
import unittest

import install_nginx_logrotate as installer


class InstallNginxRotationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.policy = self.root / "etc/logrotate.d/nginx"
        self.policy.parent.mkdir(parents=True)
        self.policy.write_text(installer.LEGACY_POLICY, encoding="utf-8")

    def test_preserves_original_and_is_idempotent(self):
        self.assertTrue(installer.install(self.root))
        backup = self.root / "var/lib/fooddiary/nginx-logrotate.original"
        self.assertEqual(installer.LEGACY_POLICY, backup.read_text())
        self.assertIn("create 0640 root adm", self.policy.read_text())
        self.assertIn("--signal=USR1 fooddiary-nginx-1", self.policy.read_text())
        self.assertFalse(installer.install(self.root))
        self.assertEqual(installer.LEGACY_POLICY, backup.read_text())

    def test_unknown_host_configuration_is_untouched(self):
        original = installer.LEGACY_POLICY.replace("rotate 14", "rotate 90")
        self.policy.write_text(original)
        with self.assertRaisesRegex(ValueError, "Unknown"):
            installer.install(self.root)
        self.assertEqual(original, self.policy.read_text())
        self.assertFalse((self.root / "var/lib/fooddiary").exists())

    def test_reviewed_host_variant_preserves_original_and_is_idempotent(self):
        original = Path(__file__).with_name("fixtures").joinpath("nginx-logrotate-host.conf").read_text()
        self.policy.write_text(original)
        self.assertTrue(installer.install(self.root))
        backup = self.root / "var/lib/fooddiary/nginx-logrotate.original"
        managed = Path(__file__).with_name("nginx-logrotate.conf").read_text()
        self.assertEqual(original, backup.read_text())
        self.assertEqual(managed, self.policy.read_text())
        if os.name == "posix":
            self.assertEqual(0o600, backup.stat().st_mode & 0o777)
            self.assertEqual(0o644, self.policy.stat().st_mode & 0o777)
        self.assertFalse(installer.install(self.root))
        self.assertEqual(original, backup.read_text())

    def test_reviewed_variant_with_operator_changes_is_untouched(self):
        reviewed = Path(__file__).with_name("fixtures").joinpath("nginx-logrotate-host.conf").read_text()
        for original in [reviewed.replace("rotate 14", "rotate 90"),
                         reviewed.replace("invoke-rc.d nginx rotate", "invoke-rc.d unrelated rotate")]:
            with self.subTest(policy=original):
                self.policy.write_text(original)
                with self.assertRaisesRegex(ValueError, "Unknown"):
                    installer.install(self.root)
                self.assertEqual(original, self.policy.read_text())
                self.assertFalse((self.root / "var/lib/fooddiary").exists())

    def test_reviewed_variant_conflicting_backup_is_preserved(self):
        original = Path(__file__).with_name("fixtures").joinpath("nginx-logrotate-host.conf").read_text()
        self.policy.write_text(original)
        backup = self.root / "var/lib/fooddiary/nginx-logrotate.original"
        backup.parent.mkdir(parents=True)
        backup.write_text("operator-owned content")
        with self.assertRaisesRegex(ValueError, "backup differs"):
            installer.install(self.root)
        self.assertEqual(original, self.policy.read_text())
        self.assertEqual("operator-owned content", backup.read_text())

    def test_conflicting_backup_is_preserved(self):
        backup = self.root / "var/lib/fooddiary/nginx-logrotate.original"
        backup.parent.mkdir(parents=True)
        backup.write_text("operator-owned content")
        with self.assertRaisesRegex(ValueError, "backup differs"):
            installer.install(self.root)
        self.assertEqual(installer.LEGACY_POLICY, self.policy.read_text())
        self.assertEqual("operator-owned content", backup.read_text())

    @unittest.skipUnless(os.name == "posix", "POSIX symlinks and modes")
    def test_symlink_is_rejected(self):
        self.policy.unlink()
        target = self.root / "unrelated"
        target.write_text(installer.LEGACY_POLICY)
        self.policy.symlink_to(target)
        with self.assertRaisesRegex(ValueError, "regular files"):
            installer.install(self.root)
        self.assertEqual(installer.LEGACY_POLICY, target.read_text())


if __name__ == "__main__":
    unittest.main()
