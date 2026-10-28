"""Trust only the host proxy's gateway on this deployment's Compose bridge."""

import ipaddress
import json
import os
from pathlib import Path
import subprocess
import tempfile


def docker_json(*arguments):
    result = subprocess.run(
        ["docker", *arguments], check=True, capture_output=True, text=True, timeout=30,
    )
    return json.loads(result.stdout)


def discover_gateways():
    # PostgreSQL already exists before the deployment's backup/migration phase.
    result = subprocess.run(
        ["docker", "compose", "ps", "-q", "postgres"],
        check=True, capture_output=True, text=True, timeout=30,
    )
    containers = result.stdout.split()
    if len(containers) != 1:
        raise ValueError("Expected one running Compose PostgreSQL container")
    networks = docker_json("inspect", "--format", "{{json .NetworkSettings.Networks}}", containers[0])
    candidates = []
    for attachment in networks.values():
        network = docker_json("network", "inspect", attachment["NetworkID"])[0]
        if (network.get("Labels") or {}).get("com.docker.compose.network") != "default":
            continue
        if network.get("Driver") != "bridge":
            raise ValueError("The default Compose network must be a bridge")
        gateways = []
        for configuration in network["IPAM"]["Config"]:
            value = configuration.get("Gateway")
            if not value:
                continue
            address = ipaddress.ip_address(value)
            if not address.is_private or address.is_loopback or address.is_unspecified or address.is_multicast:
                raise ValueError("Expected a private, specific Docker gateway address")
            gateways.append(str(address))
        if not gateways:
            raise ValueError("The Compose bridge has no gateway")
        candidates.append(sorted(set(gateways)))
    if len(candidates) != 1:
        raise ValueError("Expected exactly one default Compose bridge")
    return candidates[0]


def write_environment(path, gateways):
    path = Path(path)
    if path.is_symlink():
        raise ValueError("Refusing a symlink for the generated proxy environment")
    content = "# Generated from the current Compose bridge; regenerate before recreating API.\n"
    content += "ForwardedHeaders__ForwardLimit=1\n"
    content += "".join(f"ForwardedHeaders__KnownProxies__{index}={value}\n" for index, value in enumerate(gateways))
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", dir=path.parent, delete=False) as output:
            temporary = Path(output.name)
            output.write(content)
        os.chmod(temporary, 0o644)
        os.replace(temporary, path)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def main():
    gateways = discover_gateways()
    write_environment(Path.cwd() / ".forwarded-proxy.env", gateways)
    print("Configured exact host proxy gateway(s): " + ", ".join(gateways))


if __name__ == "__main__":
    main()
