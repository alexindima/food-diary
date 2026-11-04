"""Remove obsolete local FoodDiary release images after a successful handover."""

import argparse
import re
import subprocess


def docker_lines(*args):
    return subprocess.check_output(["docker", *args], text=True).splitlines()


def find_obsolete_images(prefix, deployed_images):
    deployed_images = sorted({ref for ref in deployed_images if ref.startswith(f"{prefix}/")})
    if not deployed_images or any(
        not re.fullmatch(r"[^@]+@sha256:[0-9a-f]{64}", ref) for ref in deployed_images
    ):
        raise ValueError("Expected pinned FoodDiary deployment images.")

    repositories = {ref.split("@", 1)[0] for ref in deployed_images}
    protected_refs = set(deployed_images)
    protected_ids = set(docker_lines("image", "inspect", "--format", "{{.Id}}", *deployed_images))

    containers = docker_lines("container", "ls", "--all", "--quiet")
    if containers:
        for row in docker_lines("container", "inspect", "--format", "{{.Image}} {{.Config.Image}}", *containers):
            image_id, reference = row.split()
            protected_ids.add(image_id)
            protected_refs.add(reference)

    candidates = set()
    for row in docker_lines("image", "ls", "--all", "--no-trunc", "--digests", "--format",
                            "{{.Repository}} {{.Tag}} {{.Digest}} {{.ID}}"):
        repository, tag, digest, image_id = row.split()
        if repository not in repositories or image_id in protected_ids:
            continue
        if tag == "<none>" and digest != "<none>":
            reference = f"{repository}@{digest}"
        elif re.fullmatch(r"sha-[0-9a-f]{40}", tag):
            reference = f"{repository}:{tag}"
        else:
            continue
        if reference not in protected_refs:
            candidates.add(reference)

    return sorted(candidates)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dry-run", action="store_true", help="List obsolete references without removing them.")
    parser.add_argument("prefix", help="FoodDiary image repository prefix.")
    parser.add_argument("deployed_images", nargs="+", help="Images configured for the deployed release.")
    args = parser.parse_args()

    candidates = find_obsolete_images(args.prefix, args.deployed_images)
    print(f"Obsolete FoodDiary image references: {len(candidates)}", flush=True)
    if args.dry_run:
        for reference in candidates:
            print(reference)
    elif candidates:
        subprocess.run(["docker", "image", "rm", "--no-prune", *candidates], check=True)


if __name__ == "__main__":
    main()
