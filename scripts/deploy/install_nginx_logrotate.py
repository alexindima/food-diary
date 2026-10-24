"""Install the reviewed host policy without overwriting an unknown policy."""

import argparse
import os
from pathlib import Path
import re
import stat
import tempfile


LEGACY_POLICY = r"""
/var/log/nginx/*.log {
    daily
    missingok
    rotate 14
    compress
    delaycompress
    notifempty
    create 0640 www-data adm
    sharedscripts
    prerotate
        if [ -d /etc/logrotate.d/httpd-prerotate ]; then \
            run-parts /etc/logrotate.d/httpd-prerotate; \
        fi
    endscript
    postrotate
        invoke-rc.d nginx rotate >/dev/null 2>&1
    endscript
}
"""


def normalized(text):
    return re.sub(r"\s+", " ", "\n".join(
        line for line in text.splitlines() if not line.lstrip().startswith("#"))).strip()


def regular_file(path):
    if not stat.S_ISREG(path.lstat().st_mode):
        raise ValueError("Rotation policy and backup must be regular files, not links.")


def atomic_write(path, content, mode):
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary = tempfile.mkstemp(prefix=".fooddiary-", dir=path.parent)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as stream:
            stream.write(content)
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary, mode)
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def install(root=Path("/")):
    policy = root / "etc/logrotate.d/nginx"
    backup = root / "var/lib/fooddiary/nginx-logrotate.original"
    managed = Path(__file__).with_name("nginx-logrotate.conf").read_text(encoding="utf-8")
    regular_file(policy)
    original = policy.read_text(encoding="utf-8")
    if normalized(original) == normalized(managed):
        return False
    if normalized(original) != normalized(LEGACY_POLICY):
        raise ValueError("Unknown Nginx logrotate policy; review it before installation. Nothing changed.")
    if backup.exists() or backup.is_symlink():
        regular_file(backup)
        if normalized(backup.read_text(encoding="utf-8")) != normalized(original):
            raise ValueError("Existing rotation backup differs; review it before installation.")
    else:
        atomic_write(backup, original, 0o600)
    atomic_write(policy, managed, 0o644)
    return True


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path("/"), help="Filesystem root; staging fixtures only.")
    arguments = parser.parse_args()
    try:
        changed = install(arguments.root)
    except (OSError, ValueError) as error:
        parser.exit(1, f"Nginx rotation installation failed: {error}\n")
    print("Nginx rotation policy installed." if changed else "Nginx rotation policy already current.")
