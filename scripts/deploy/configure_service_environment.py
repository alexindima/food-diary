"""Project Compose interpolation settings into each supporting service's own environment."""

import json
import os
from pathlib import Path
import subprocess
import tempfile


SERVICE_PREFIXES = {
    ".env.mail-relay": ("mailrelay__", "mailrelayqueue__", "mailrelaybroker__", "relaysmtp__",
                        "mailrelaydelivery__", "directmx__", "mailrelaydkim__", "opentelemetry__", "logging__"),
    ".env.mailrelay-init": ("mailrelayqueue__", "fooddiary_mailrelayqueue__", "mailrelay_mailrelayqueue__"),
    ".env.mail-inbox": ("mailinboxsmtp__", "mailinboxstorage__", "mailinboxhttp__", "opentelemetry__", "logging__",
                        "mail_inbox_tls_certificate_source", "mail_inbox_tls_private_key_source"),
    ".env.telegram-bot": ("telegrambot__", "logging__"),
}


def read_environment(source):
    source = Path(source).resolve(strict=True)
    model = {"services": {"source": {"image": "scratch", "env_file": [str(source)]}}}
    result = subprocess.run(
        ["docker", "compose", "--env-file", str(source), "-f", "-", "config", "--format", "json"],
        input=json.dumps(model), cwd=source.parent, capture_output=True, text=True, encoding="utf-8", timeout=30,
    )
    if result.returncode:
        raise RuntimeError("Compose could not resolve the source environment")
    values = json.loads(result.stdout)["services"]["source"].get("environment", {})
    # Compose escapes literal dollars for replaying its canonical configuration.
    return {key: value.replace("$$", "$") if isinstance(value, str) else value for key, value in values.items()}


def write_environments(directory, environment):
    directory = Path(directory).resolve(strict=True)
    for filename, prefixes in SERVICE_PREFIXES.items():
        destination = directory / filename
        if destination.is_symlink():
            raise ValueError("Refusing a symlink for a generated service environment")
        entries = []
        for key, value in sorted(environment.items()):
            if key.casefold().startswith(prefixes) and value is not None:
                # Compose parses quotes, escapes and dollar interpolation again when loading this file.
                encoded = json.dumps(str(value), ensure_ascii=False).replace("$", "$$")
                entries.append(f"{key}={encoded}\n")
        temporary = None
        try:
            with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", dir=directory, delete=False) as output:
                temporary = Path(output.name)
                output.writelines(entries)
            os.chmod(temporary, 0o600)
            os.replace(temporary, destination)
        finally:
            if temporary is not None:
                temporary.unlink(missing_ok=True)


def main():
    directory = Path.cwd()
    write_environments(directory, read_environment(directory / ".env"))
    print("Configured isolated environments for supporting services.")


if __name__ == "__main__":
    main()
