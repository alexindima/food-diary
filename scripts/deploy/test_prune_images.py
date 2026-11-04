"""Verify post-deploy image retention against pinned release and rollback images."""

from pathlib import Path
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[2]
PREFIX = "ghcr.io/alexindima/food-diary"
API = f"{PREFIX}/api"
INITIALIZER = f"{PREFIX}/initializer"
CURRENT, INIT, ROLLBACK, OBSOLETE, OTHER = [f"sha256:{digit * 64}" for digit in "abcde"]


class ImageCleanupTests(unittest.TestCase):
    def test_deploy_cleans_images_by_release_after_finishing_handover(self):
        workflow = (ROOT / ".github/workflows/deploy.yml").read_text(encoding="utf-8")
        self.assertIn("python3 scripts/deploy/prune_images.py", workflow,
                      "deploy must clean by release identity instead of local image age")
        finish = workflow.index("python3 scripts/deploy/blue_green.py finish")
        cleanup = workflow.index("python3 scripts/deploy/prune_images.py")
        self.assertLess(finish, cleanup)
        self.assertLess(cleanup, workflow.index('echo "Deploy completed!"'))
        self.assertNotIn("until=24h", workflow)

    def test_retains_pinned_images_and_stopped_container_references(self):
        import prune_images

        old_tag = f"sha-{'1' * 40}"
        current = [f"{API}@{CURRENT}", f"{INITIALIZER}@{INIT}", f"redis@{OTHER}"]
        inventory = [
            f"{API} <none> {CURRENT} {CURRENT}",
            f"{API} {old_tag} {CURRENT} {CURRENT}",
            f"{INITIALIZER} <none> {INIT} {INIT}",
            f"{API} <none> {ROLLBACK} {ROLLBACK}",
            f"{API} <none> {OBSOLETE} {OBSOLETE}",
            f"{INITIALIZER} {old_tag} <none> {OBSOLETE}",
            f"{API} debug <none> {OBSOLETE}",
            f"{PREFIX}/other <none> {OTHER} {OTHER}",
            f"redis <none> {OTHER} {OTHER}",
        ]
        with patch.object(prune_images, "docker_lines", side_effect=[
            [CURRENT, INIT], ["stopped-container"], [f"{ROLLBACK} {API}@{ROLLBACK}"], inventory,
        ]) as docker:
            candidates = prune_images.find_obsolete_images(PREFIX, current)

        self.assertEqual([f"{API}@{OBSOLETE}", f"{INITIALIZER}:{old_tag}"], candidates)
        docker.assert_any_call("image", "inspect", "--format", "{{.Id}}", *current[:2])
        docker.assert_any_call("container", "ls", "--all", "--quiet")

    def test_rejects_an_unpinned_deployment_reference(self):
        import prune_images

        with patch.object(prune_images, "docker_lines") as docker:
            with self.assertRaisesRegex(ValueError, "pinned"):
                prune_images.find_obsolete_images(PREFIX, [f"{API}:latest"])
        docker.assert_not_called()
