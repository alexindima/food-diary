#!/usr/bin/env python3
"""Publish immutable Angular releases inside an existing, stable Nginx bind root.

No release or retained asset is pruned. Call under the deployment lock when
coordinating both sites and SSR; each site also has its own publication lock.
"""

import argparse
from contextlib import contextmanager
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import sys
import uuid


RELEASE_ID = re.compile(r"(?:[0-9a-f]{40}|sha256-[0-9a-f]{64})\Z")
# Angular/esbuild uses eight base64url characters; older webpack builds use hex.
# Keep this expression aligned with both production Nginx hashed-asset locations.
HASHED_ASSET = re.compile(
    r"(?:^|/)[^/]+(?:-[A-Za-z0-9_-]{8}|\.[a-f0-9]{16,64})"
    r"\.(?:js|css|woff2?|ttf|otf|eot|svg|png|jpe?g|gif|ico|webp|avif)\Z"
)
RESERVED = {"current", "previous", "releases", "retained-assets", ".staging", ".publish.lock"}


class PublicationError(ValueError):
    pass


def is_link(path):
    return path.is_symlink() or (hasattr(path, "is_junction") and path.is_junction())


def absolute_directory(value, *, create=False):
    path = Path(value)
    if not path.is_absolute() or ".." in path.parts or path == Path(path.anchor):
        raise PublicationError(f"Expected an absolute non-root directory: {value}")
    for part in [*reversed(path.parents), path]:
        if is_link(part):
            raise PublicationError(f"Symlink/junction directory is not allowed: {part}")
    if create:
        path.mkdir(parents=True, exist_ok=True)
    if not path.is_dir() or path.resolve() != path:
        raise PublicationError(f"Invalid directory: {path}")
    return path


def relative_path(value):
    path = PurePosixPath(value)
    if (not value or path.is_absolute() or "\\" in value or ":" in value
            or any(part in {"", ".", ".."} for part in value.split("/"))):
        raise PublicationError(f"Unsafe relative path: {value}")
    return path


def tree(directory, *, legacy=False):
    """Return exact regular-file content identities; reject every symlink."""
    directory = absolute_directory(directory)
    result = {}
    for base, directories, files in os.walk(directory, followlinks=False):
        base = Path(base)
        if base == directory and legacy:
            directories[:] = [name for name in directories if name not in RESERVED and name != ".well-known"]
            files = [name for name in files if name not in RESERVED]
        for name in [*directories, *files]:
            path = base / name
            relative_path(path.relative_to(directory).as_posix())
            mode = path.lstat().st_mode
            if is_link(path) or not (stat.S_ISDIR(mode) or stat.S_ISREG(mode)):
                raise PublicationError(f"Only regular files/directories are allowed: {path}")
        for name in files:
            path = base / name
            result[path.relative_to(directory).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    return result


def child_directory(root, name):
    relative_path(name)
    path = root / name
    absolute_directory(path, create=True)
    return path


def copy_snapshot(source, destination, expected):
    for name in sorted(expected):
        target = destination / name
        absolute_directory(target.parent, create=True)
        if os.path.lexists(target):
            raise PublicationError(f"Unexpected staging collision: {target}")
        # The source is trusted build output, but changing it during copy must fail.
        if is_link(source / name):
            raise PublicationError(f"Source changed to a symlink: {name}")
        shutil.copyfile(source / name, target, follow_symlinks=False)
        if is_link(target) or not stat.S_ISREG(target.lstat().st_mode):
            raise PublicationError(f"Source changed during copy: {name}")
        target.chmod(0o644)
    if tree(destination) != expected:
        raise PublicationError("Source changed or copy was incomplete")


def validate_release(directory, *, require_ngsw=False):
    contents = tree(directory)
    if "index.html" not in contents:
        raise PublicationError("Release must contain index.html")
    manifest_path = directory / "ngsw.json"
    if not manifest_path.exists():
        if require_ngsw:
            raise PublicationError("Public release must contain ngsw.json")
        return contents
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        hashes = manifest["hashTable"]
        index = manifest["index"]
        if not isinstance(hashes, dict) or not hashes or index not in hashes:
            raise PublicationError("Service worker index must be versioned")
        for url, digest in hashes.items():
            if not isinstance(url, str) or not url.startswith("/") or not isinstance(digest, str):
                raise PublicationError("Invalid service worker hash entry")
            name = relative_path(url[1:]).as_posix()
            if name not in contents or hashlib.sha1((directory / name).read_bytes()).hexdigest() != digest:
                raise PublicationError(f"Service worker content mismatch: {url}")
    except (KeyError, TypeError, json.JSONDecodeError) as error:
        raise PublicationError("Invalid ngsw.json") from error
    return contents


def release_id(value):
    if not RELEASE_ID.fullmatch(value):
        raise PublicationError("Release ID must be a lowercase commit SHA or sha256-<digest>")
    return value


def pointer(root, name):
    path = root / name
    if not os.path.lexists(path):
        return None
    if not path.is_symlink():
        raise PublicationError(f"{name} must be an internal relative symlink")
    target = os.readlink(path).replace("\\", "/")
    parts = PurePosixPath(target).parts
    if len(parts) != 2 or parts[0] != "releases" or target != f"releases/{parts[1]}":
        raise PublicationError(f"Unsafe {name} symlink")
    identity = release_id(parts[1])
    directory = absolute_directory(root / "releases" / identity)
    if path.resolve() != directory:
        raise PublicationError(f"Escaping {name} symlink")
    return identity


def switch(root, name, identity):
    path = root / f".{name}-{uuid.uuid4().hex}"
    os.symlink(f"releases/{identity}", path, target_is_directory=True)
    os.replace(path, root / name)


@contextmanager
def publication_lock(root):
    lock = root / ".publish.lock"
    if is_link(lock):
        raise PublicationError("Publication lock must not be a symlink")
    descriptor = os.open(lock, os.O_CREAT | os.O_RDWR | getattr(os, "O_NOFOLLOW", 0), 0o600)
    try:
        if os.name == "posix":
            import fcntl
            fcntl.flock(descriptor, fcntl.LOCK_EX)
        yield
    finally:
        os.close(descriptor)


def retain(root, directory, contents):
    retained = child_directory(root, "retained-assets")
    # Preflight ALL collisions before writing either compatibility or retained files.
    hashed = [name for name in contents if HASHED_ASSET.search(name)]
    for base in (retained, root):
        for name in hashed:
            destination = base / name
            absolute_directory(destination.parent, create=True)
            if os.path.lexists(destination):
                if (is_link(destination) or not destination.is_file()
                        or hashlib.sha256(destination.read_bytes()).hexdigest() != contents[name]):
                    raise PublicationError(f"Immutable asset URL collision: {destination}")
    for base in (retained, root):
        for name in hashed:
            destination = base / name
            if not destination.exists():
                temporary = destination.with_name(f".{destination.name}-{uuid.uuid4().hex}")
                shutil.copyfile(directory / name, temporary)
                temporary.chmod(0o644)
                if hashlib.sha256(temporary.read_bytes()).hexdigest() != contents[name]:
                    raise PublicationError(f"Incomplete retained asset copy: {name}")
                os.replace(temporary, destination)


def stage(root, source, contents, identity):
    releases = child_directory(root, "releases")
    destination = releases / release_id(identity)
    if os.path.lexists(destination):
        if tree(destination) != contents:
            raise PublicationError(f"Release ID already contains different bytes: {identity}")
        return destination
    temporary = child_directory(child_directory(root, ".staging"), uuid.uuid4().hex)
    copy_snapshot(source, temporary, contents)
    # Same filesystem as releases/current. Never expose an incomplete release.
    os.rename(temporary, destination)
    return destination


def publish(source, root, identity, *, require_ngsw=False):
    source = absolute_directory(source)
    root = absolute_directory(root, create=True)
    if source == root or source.is_relative_to(root) or root.is_relative_to(source):
        raise PublicationError("Source and destination must be disjoint directories")
    release_id(identity)
    with publication_lock(root):
        old = pointer(root, "current")
        pointer(root, "previous")
        contents = validate_release(source, require_ngsw=require_ngsw)
        if any(PurePosixPath(name).parts[0] in RESERVED for name in contents):
            raise PublicationError("Release contains a reserved publication path")
        if old is None:
            legacy = tree(root, legacy=True)
            if "index.html" in legacy:
                digest = hashlib.sha256(json.dumps(legacy, sort_keys=True).encode()).hexdigest()
                old = f"sha256-{digest}"
                legacy_release = stage(root, root, legacy, old)
                retain(root, legacy_release, legacy)
        directory = stage(root, source, contents, identity)
        validate_release(directory, require_ngsw=require_ngsw)
        retain(root, directory, contents)
        if old and old != identity:
            switch(root, "previous", old)
        switch(root, "current", identity)
    return directory


def rollback(root, identity=None, *, require_ngsw=False):
    root = absolute_directory(root)
    with publication_lock(root):
        old = pointer(root, "current")
        previous = pointer(root, "previous")
        identity = release_id(identity) if identity else previous
        if identity is None:
            raise PublicationError("No previous release exists")
        directory = absolute_directory(root / "releases" / identity)
        contents = validate_release(directory, require_ngsw=require_ngsw)
        retain(root, directory, contents)
        if old and old != identity:
            switch(root, "previous", old)
        switch(root, "current", identity)
    return directory


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    publish_command = commands.add_parser("publish")
    publish_command.add_argument("--source", required=True, type=Path)
    publish_command.add_argument("--root", required=True, type=Path)
    publish_command.add_argument("--release-id", required=True)
    publish_command.add_argument("--require-ngsw", action="store_true")
    rollback_command = commands.add_parser("rollback")
    rollback_command.add_argument("--root", required=True, type=Path)
    rollback_command.add_argument("--release-id")
    rollback_command.add_argument("--require-ngsw", action="store_true")
    args = parser.parse_args()
    try:
        if args.command == "publish":
            result = publish(args.source, args.root, args.release_id, require_ngsw=args.require_ngsw)
        else:
            result = rollback(args.root, args.release_id, require_ngsw=args.require_ngsw)
    except (OSError, PublicationError) as error:
        print(f"Frontend publication failed: {error}", file=sys.stderr)
        return 1
    print(f"Frontend {args.command}: {result}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
