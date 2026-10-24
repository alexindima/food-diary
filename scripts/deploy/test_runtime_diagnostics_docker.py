"""Linux boundary tests; enable explicitly with FOODDIARY_RUNTIME_DOCKER_TESTS=1."""

import json
import os
from pathlib import Path
import subprocess
import tempfile
import time
import unittest
import uuid

import install_nginx_logrotate as installer


ROOT = Path(__file__).resolve().parents[2]
NGINX = "nginx:alpine@sha256:4a73073bd557c65b759505da037898b61f1be6cbcc3c2c3aeac22d2a470c1752"
RUNTIME = "mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:819bd3649d8bf53f35527c68d62e17b348ef6f24ae73073f497f00a878348062"
SDK = "mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:230c3fbae0ceedc70bca04eed2bfb5694e8b248cadbb81010f41b7a993601f55"


def docker(*arguments, input=None, check=True, timeout=120):
    # Keep Linux stdin LF-only even when this test runs on Windows.
    result = subprocess.run(["docker", *arguments], input=input.encode("utf-8") if input is not None else None,
                            capture_output=True, timeout=timeout)
    result.stdout = result.stdout.decode("utf-8", errors="replace")
    result.stderr = result.stderr.decode("utf-8", errors="replace")
    if check and result.returncode:
        raise AssertionError(f"Docker fixture command failed: {arguments[0]}: {result.stderr[-1500:]}")
    return result


def wait_for(probe, message, timeout=30):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if probe():
            return
        time.sleep(0.1)
    raise AssertionError(message)


@unittest.skipUnless(os.environ.get("FOODDIARY_RUNTIME_DOCKER_TESTS") == "1", "Explicit Docker boundary suite")
class RuntimeDiagnosticsDockerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        docker("version", timeout=20)
        cls.temporary = tempfile.TemporaryDirectory(prefix="fooddiary-runtime-test-")
        cls.addClassCleanup(cls.temporary.cleanup)
        cls.fixture = Path(cls.temporary.name)
        cls.tag = "fooddiary-runtime-fixture:" + uuid.uuid4().hex
        cls.addClassCleanup(lambda: docker("image", "rm", cls.tag, check=False))
        (cls.fixture / "Dockerfile").write_text(f"""FROM {SDK} AS build
RUN apk add --no-cache gcc musl-dev
WORKDIR /source
COPY Fixture.csproj Program.cs native-crash.c ./
RUN dotnet publish -c Release -o /fixture && cc -shared -fPIC -g -fasynchronous-unwind-tables -fno-omit-frame-pointer -o /fixture/native-crash.so native-crash.c
FROM {RUNTIME}
RUN apk add --no-cache logrotate gdb
COPY --from=build /fixture /fixture
WORKDIR /fixture
ENTRYPOINT []
""", encoding="utf-8")
        (cls.fixture / "Fixture.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
<AssemblyName>FoodDiary.JobManager</AssemblyName>
</PropertyGroup></Project>''', encoding="utf-8")
        (cls.fixture / "Program.cs").write_text('''class Program {
    [System.Runtime.InteropServices.DllImport("/fixture/native-crash.so")]
    private static extern void crash();
    static void Main() {
        System.Console.WriteLine("fixture-ready");
        while (!System.IO.File.Exists("/tmp/crash")) System.Threading.Thread.Sleep(100);
        crash();
    }
}''', encoding="utf-8")
        (cls.fixture / "native-crash.c").write_text('''void crash(void) { *(volatile int *)0x123456789ULL = 1; }
''', encoding="utf-8")
        docker("build", "--tag", cls.tag, str(cls.fixture), timeout=600)

    def unique(self, suffix):
        return "fd-runtime-" + uuid.uuid4().hex + "-" + suffix

    def volume(self, *options):
        name = self.unique("volume")
        docker("volume", "create", *options, name)
        self.addCleanup(lambda: docker("volume", "rm", name, check=False))
        return name

    def container(self, *arguments):
        name = self.unique("container")
        self.addCleanup(lambda: docker("rm", "--force", name, check=False))
        docker("run", "--detach", "--name", name, *arguments)
        return name

    def nginx_case(self, managed):
        logs = self.volume()
        config = self.fixture / (self.unique("nginx") + ".conf")
        config.write_text('''user nginx;
events {}
http {
  access_log /test-logs/access.log;
  error_log /test-logs/error.log;
  server { listen 8080; location / { return 200 'ok'; } }
}''', encoding="utf-8")
        docker("run", "--rm", "-v", logs + ":/test-logs", self.tag, "sh", "-c",
               "touch /test-logs/access.log /test-logs/error.log; chmod 0640 /test-logs/*.log")
        nginx = self.container("--read-only", "--cap-drop=ALL", "--cap-add=CHOWN",
                               "--cap-add=SETUID", "--cap-add=SETGID", "--cap-add=NET_BIND_SERVICE",
                               "--security-opt=no-new-privileges:true", "--tmpfs", "/var/cache/nginx",
                               "--tmpfs", "/var/run", "-v", logs + ":/test-logs",
                               "-v", str(config) + ":/etc/nginx/nginx.conf:ro", NGINX)
        def request(marker):
            return docker("exec", nginx, "wget", "-q", "-O", "/dev/null",
                          "http://127.0.0.1:8080/" + marker, check=False).returncode == 0
        wait_for(lambda: request("before"), "Nginx fixture did not become ready")
        policy = (ROOT / "scripts/deploy/nginx-logrotate.conf").read_text() if managed else installer.LEGACY_POLICY
        policy = policy.replace("/var/log/nginx/", "/test-logs/")
        # The host-like helper shares only the fixture PID namespace to deliver USR1.
        policy = policy.replace("/usr/bin/docker kill --signal=USR1 fooddiary-nginx-1 > /dev/null", "kill -USR1 1")
        policy = policy.replace("invoke-rc.d nginx rotate >/dev/null 2>&1", "kill -USR1 1")
        # Alpine's existing unprivileged account reproduces the foreign-owner
        # condition without depending on Debian's www-data UID database.
        policy = policy.replace("www-data adm", "nobody root").replace("root adm", "root root")
        for index in range(2 if managed else 1):
            docker("run", "--rm", "--pid=container:" + nginx, "-i", "-v", logs + ":/test-logs",
                   self.tag, "sh", "-c", "umask 077; cat >/tmp/rotation; logrotate -f -s /tmp/state /tmp/rotation", input=policy)
            marker = "after-" + str(index)
            wait_for(lambda: request(marker), "Nginx stopped serving during rotation")
            def new_file_has_marker():
                return docker("exec", nginx, "grep", "-q", marker, "/test-logs/access.log", check=False).returncode == 0
            if managed:
                wait_for(new_file_has_marker, "New access log did not receive requests")
                rotated_size = docker("exec", nginx, "stat", "-c", "%s", "/test-logs/access.log.1").stdout
                self.assertTrue(request("new-file-only"))
                self.assertEqual(rotated_size, docker("exec", nginx, "stat", "-c", "%s", "/test-logs/access.log.1").stdout)
            else:
                wait_for(lambda: docker("exec", nginx, "grep", "-q", marker, "/test-logs/access.log.1", check=False).returncode == 0,
                         "Legacy policy did not reproduce stale-file writes")
                self.assertFalse(new_file_has_marker())
                errors = docker("logs", nginx).stderr
                for path in ["/test-logs/error.log", "/test-logs/error.log.1"]:
                    errors += docker("exec", nginx, "cat", path, check=False).stdout
                self.assertIn("Permission denied", errors)

    def test_legacy_rotation_reproduces_permission_failure(self):
        self.nginx_case(False)

    def test_managed_rotation_reopens_files_twice(self):
        self.nginx_case(True)

    def test_native_capture_survives_restart_and_storage_is_bounded(self):
        project = self.unique("compose")
        base = self.fixture / "compose.yml"
        base.write_text('''services:
  job-manager:
    image: ${JOB_MANAGER_IMAGE_REF}
    read_only: true
    cap_drop: [ALL]
    security_opt: [no-new-privileges:true]
    tmpfs: [/tmp:size=32m]
''', encoding="utf-8")
        compose = ["compose", "--project-directory", str(ROOT), "--project-name", project,
                   "-f", str(base), "-f", str(ROOT / "docker-compose.job-manager-diagnostics.yml"), "--profile", "jobs"]
        previous_image = os.environ.get("JOB_MANAGER_IMAGE_REF")
        os.environ["JOB_MANAGER_IMAGE_REF"] = self.tag
        def cleanup():
            try:
                docker(*compose, "down", "--volumes", check=False)
            finally:
                if previous_image is None:
                    os.environ.pop("JOB_MANAGER_IMAGE_REF", None)
                else:
                    os.environ["JOB_MANAGER_IMAGE_REF"] = previous_image
        self.addCleanup(cleanup)
        docker(*compose, "up", "--detach", "--wait", timeout=90)
        keeper = docker(*compose, "ps", "--quiet", "job-manager-diagnostics-retention").stdout.strip()
        app = docker(*compose, "ps", "--quiet", "job-manager").stdout.strip()
        self.assertTrue(keeper)
        self.assertTrue(app)
        directory = "/var/lib/fooddiary/job-manager-crashdumps"
        control = "/opt/fooddiary/job-manager-diagnostics.sh"
        wait_for(lambda: "fixture-ready" in docker("logs", app).stdout, "Managed fixture did not start")
        docker("exec", app, "test", "-f", "/fixture/native-crash.so")
        docker("exec", app, "touch", "/tmp/crash")
        wait_for(lambda: docker("inspect", "--format", "{{.State.Running}}", app).stdout.strip() == "false",
                 "Native crash did not terminate", timeout=90)
        inventory = docker("exec", keeper, "sh", "-c", "find '" + directory + "' -name '*.crashreport.json'").stdout.splitlines()
        self.assertEqual(1, len(inventory), "Native crash must produce an actual report, not just configuration")
        # Synthetic fixtures only. Production artifacts must never enter CI or logs.
        report = json.loads(docker("exec", keeper, "cat", inventory[0]).stdout)
        threads = report.get("payload", {}).get("threads", [])
        self.assertTrue(threads, "Crash report must contain thread data")
        # A native fault can occur inside the loader before the test library's
        # entrypoint. Require the crashing thread's native frames, not its symbol.
        crashed_threads = [thread for thread in threads if str(thread.get("crashed")).lower() == "true"]
        native_frames = [frame for thread in crashed_threads for frame in thread.get("stack_frames", [])
                         if str(frame.get("is_managed")).lower() == "false"]
        self.assertTrue(native_frames, "The crashing thread must retain native frames")
        dump = inventory[0].removesuffix(".crashreport.json")
        size = int(docker("exec", keeper, "stat", "-c", "%s", dump).stdout)
        self.assertGreater(size, 4096)
        backtrace = docker("exec", keeper, "gdb", "--batch", "--nx", "-iex", "set auto-load off",
                           "/usr/share/dotnet/dotnet", dump, "-ex", "bt 5")
        self.assertIn("#0", backtrace.stdout, "A native debugger must read an actual core backtrace")
        print(f"Synthetic native capture: {size} dump bytes, {len(native_frames)} native frames.", flush=True)
        self.assertEqual("600", docker("exec", keeper, "stat", "-c", "%a", dump).stdout.strip())
        docker("start", app)
        wait_for(lambda: docker("inspect", "--format", "{{.State.Running}}", app).stdout.strip() == "true",
                 "Application fixture did not restart")
        self.assertEqual(size, int(docker("exec", keeper, "stat", "-c", "%s", dump).stdout))
        # Only the owned diagnostic filesystem is filled. The app's /tmp stays usable.
        fill = docker("exec", app, "sh", "-c", "dd if=/dev/zero of='" + directory + "/capacity-probe' bs=1M count=300 2>/dev/null", check=False)
        self.assertNotEqual(0, fill.returncode)
        self.assertEqual(0, docker("exec", app, "sh", "-c", "echo healthy >/tmp/application-probe", check=False).returncode)
        docker("exec", keeper, "rm", directory + "/capacity-probe")
        docker("exec", keeper, "sh", "-c", "for n in 1 2 3 4 5 6; do touch '" + directory + "/job-manager.100.'\"$n\"; done; touch -t 202001010000 '" + directory + "/job-manager.expired'")
        docker("exec", keeper, "sh", control, "prune")
        remaining = docker("exec", keeper, "sh", "-c", "find '" + directory + "' -name 'job-manager.*' -type f").stdout.splitlines()
        self.assertLessEqual(len(remaining), 4)
        self.assertFalse(any("expired" in path for path in remaining))


if __name__ == "__main__":
    unittest.main()
