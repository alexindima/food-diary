"""Execute the deployment shell against isolated tools; never contact Docker or a host."""

import json
import os
from pathlib import Path
import re
import shutil
import shlex
import signal
import socket
import subprocess
import tempfile
import textwrap
import threading
import unittest


ROOT = Path(__file__).resolve().parents[2]
HELPER = ROOT / "scripts/deploy/parallel_tasks.sh"
SHA = "a" * 40

TRACE = r'''
import json, os, socket
from pathlib import Path

def emit(name, **values):
    with socket.socket(socket.AF_UNIX, socket.SOCK_DGRAM) as connection:
        connection.sendto(json.dumps(dict(name=name, **values)).encode(), os.environ['EVENT_SOCKET'])

def gate(name):
    path = Path(os.environ['FIXTURE']) / 'gates' / name
    if path.exists():
        descriptor = os.open(path, os.O_RDWR)
        emit('gate', gate=name)
        try:
            os.read(descriptor, 1)
        finally:
            os.close(descriptor)
'''

TOOL = r'''#!/usr/bin/env python3
import json, os, sys
from pathlib import Path
from trace_fixture import emit, gate
tool, arguments = Path(sys.argv[0]).name, sys.argv[1:]
emit('command', tool=tool, arguments=arguments)
if tool == 'docker':
    if any('pg_dump' in value for value in arguments):
        emit('backup')
        if os.environ.get('FAIL_BACKUP'):
            sys.exit(23)
        print('verified fixture dump')
    elif 'pg_restore' in arguments:
        assert sys.stdin.read().strip()
        gate('backup-verify')
        if os.environ.get('FAIL_VERIFY'):
            sys.exit(24)
        emit('backup_verified')
    elif 'run' in arguments and any(value.endswith('db-init') for value in arguments):
        service = next(value for value in arguments if value.endswith('db-init'))
        emit('initializer_start', service=service)
        gate('initializer-' + service)
        if service == 'db-init' and os.environ.get('FAIL_MAIN'):
            emit('main_failed')
            sys.exit(31)
        emit('initializer_end', service=service)
    elif 'up' in arguments:
        service = arguments[-1]
        emit('service', service=service)
        if service == 'tech-radar':
            gate('tech-radar')
        if service == 'mail-relay' and os.environ.get('FAIL_AUXILIARY'):
            sys.exit(17)
    elif arguments[:1] == ['create']:
        print('fixture-image')
    elif arguments[:1] == ['cp']:
        destination = Path(arguments[-1])
        destination.mkdir(parents=True, exist_ok=True)
        (destination / 'index.html').write_text('fixture')
elif tool == 'curl':
    if any('/internal/version' in value for value in arguments):
        print(json.dumps({'commitSha': os.environ['DEPLOY_COMMIT_SHA'],
                          'imageTag': os.environ['DEPLOY_IMAGE_TAG']}, separators=(',', ':')))
    elif any('/health/ready' in value for value in arguments):
        emit('api_ready')
        print('Healthy')
    elif '--write-out' in arguments:
        print('401')
'''

BUG_TRIAGE = '''from trace_fixture import emit, gate
emit('initializer_start', service='bugtriage')
gate('initializer-bugtriage')
emit('initializer_end', service='bugtriage')
emit('bugtriage_started')
gate('bugtriage')
emit('bugtriage_finished')
'''


@unittest.skipUnless(os.name == "posix", "Run POSIX deployment tests in a Linux container on Windows")
class DeploymentFixture(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="fd-deploy-")
        self.root = Path(self.temporary.name)
        self.events = []
        self.condition = threading.Condition()
        self.processes = []
        self.socket = socket.socket(socket.AF_UNIX, socket.SOCK_DGRAM)
        self.socket.bind(str(self.root / "events.sock"))
        self.reader = threading.Thread(target=self.read_events, daemon=True)
        self.reader.start()
        self.bin = self.root / "bin"
        self.bin.mkdir()
        (self.bin / "trace_fixture.py").write_text(TRACE)
        for name in ("docker", "curl", "logrotate"):
            path = self.bin / name
            path.write_text(TOOL)
            path.chmod(0o700)
        (self.root / "gates").mkdir()
        self.environment = dict(os.environ, PATH=str(self.bin) + os.pathsep + os.environ["PATH"],
                                PYTHONPATH=str(self.bin), EVENT_SOCKET=str(self.root / "events.sock"),
                                FIXTURE=str(self.root), DEPLOY_COMMIT_SHA=SHA, DEPLOY_IMAGE_TAG="sha-" + SHA)

    def tearDown(self):
        for path in (self.root / "gates").iterdir():
            self.release(path.name)
        for process in self.processes:
            try:
                process.communicate(timeout=5)
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGKILL)
                process.communicate(timeout=5)
        with socket.socket(socket.AF_UNIX, socket.SOCK_DGRAM) as connection:
            connection.sendto(b'{"name":"stop"}', str(self.root / "events.sock"))
        self.reader.join(timeout=5)
        self.socket.close()
        self.temporary.cleanup()

    def read_events(self):
        while True:
            event = json.loads(self.socket.recv(8192))
            if event["name"] == "stop":
                return
            with self.condition:
                self.events.append(event)
                self.condition.notify_all()

    def wait_for(self, predicate, timeout=5):
        with self.condition:
            return self.condition.wait_for(lambda: predicate(self.events), timeout=timeout)

    def make_gate(self, name):
        os.mkfifo(self.root / "gates" / name)

    def release(self, name):
        try:
            descriptor = os.open(self.root / "gates" / name, os.O_WRONLY | os.O_NONBLOCK)
        except (FileNotFoundError, OSError):
            return
        try:
            os.write(descriptor, b"1")
        finally:
            os.close(descriptor)

    def start(self, script, **environment):
        path = self.root / f"script-{len(self.processes)}.sh"
        path.write_text(script)
        wrapper = '''bash "$1"; code=$?; python3 -c 'from trace_fixture import emit; import sys; emit("process_exit", code=int(sys.argv[1]), identity=sys.argv[2])' "$code" "$2"; exit "$code"'''
        identity = str(len(self.processes))
        process = subprocess.Popen(["bash", "-c", wrapper, "fixture", str(path), identity], cwd=self.root,
                                   env=self.environment | environment, text=True,
                                   stdout=subprocess.PIPE, stderr=subprocess.STDOUT, start_new_session=True)
        self.processes.append(process)
        process.fixture_identity = identity
        return process

    def result(self, process):
        output, _ = process.communicate(timeout=15)
        self.assertTrue(self.wait_for(lambda rows: any(row["name"] == "process_exit" and
                                                     row["identity"] == process.fixture_identity for row in rows)))
        return process.returncode, output

    def workflow(self):
        workflow = (ROOT / ".github/workflows/deploy.yml").read_text()
        source = textwrap.dedent(workflow.split("<< 'SCRIPT_EOF'\n", 1)[1].split("          SCRIPT_EOF", 1)[0])
        source = re.sub(r"\$\{\{.*?\}\}", "fixture", source)
        source = source.replace('"/tmp/fooddiary-deploy-', f'"{self.root}/task-logs-')
        for prefix in ("/opt/fooddiary", "/var/www", "/var/log", "/var/backups", "/var/lock", "/etc"):
            source = source.replace(prefix, str(self.root / prefix.lstrip("/")))
        application = self.root / "opt/fooddiary"
        scripts = application / "scripts/deploy"
        scripts.mkdir(parents=True, exist_ok=True)
        if HELPER.exists():
            shutil.copy(HELPER, scripts / HELPER.name)
        for name in ("install_nginx_logrotate.py", "configure_forwarded_proxy.py"):
            (scripts / name).write_text("# isolated host policy fixture\n")
        (scripts / "publish_frontend.py").write_text(
            "from trace_fixture import emit\nimport os, sys\nemit('assets')\n"
            "sys.exit(41 if os.environ.get('FAIL_PUBLISH') else 0)\n")
        (application / "bugtriage").mkdir(exist_ok=True)
        (application / "bugtriage/deploy.py").write_text(BUG_TRIAGE)
        for relative in ("etc/fooddiary/fooddiary.env", "etc/letsencrypt/renewal-hooks/deploy/restart-fooddiary-mail-inbox.sh",
                         "opt/fooddiary/scripts/bootstrap-tech-radar-tls.sh"):
            path = self.root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("# fixture\n")
        for relative in ("var/lock", "var/log/nginx"):
            (self.root / relative).mkdir(parents=True, exist_ok=True)
        environment = {name: "fixture@sha256:" + "b" * 64 for name in re.findall(r"\b[A-Z_]+_IMAGE_REF\b", source)}
        return self.start(source, **environment)


class DeployWorkflowTests(DeploymentFixture):
    def test_api_ready_does_not_wait_for_independent_bugtriage(self):
        self.make_gate("bugtriage")
        process = self.workflow()
        self.assertTrue(self.wait_for(lambda rows: any(row["name"] == "api_ready" for row in rows)),
                        "API readiness must not wait for independent BugTriage")
        self.assertTrue(self.wait_for(lambda rows: any(row.get("gate") == "bugtriage" for row in rows)))
        self.assertFalse(any(row["name"] == "assets" for row in self.events))
        self.release("bugtriage")
        code, output = self.result(process)
        self.assertEqual(0, code, output)
        names = [row["name"] for row in self.events]
        self.assertLess(names.index("api_ready"), names.index("bugtriage_finished"))
        self.assertLess(names.index("bugtriage_finished"), names.index("assets"))

    def test_auxiliary_failure_blocks_frontend_after_primary_api_recovers(self):
        self.environment["FAIL_AUXILIARY"] = "1"
        self.make_gate("bugtriage")
        process = self.workflow()
        self.assertTrue(self.wait_for(lambda rows: any(row.get("gate") == "bugtriage" for row in rows)))
        self.assertTrue(self.wait_for(lambda rows: any(row["name"] == "api_ready" for row in rows)))
        self.assertFalse(self.wait_for(lambda rows: any(row["name"] == "process_exit" for row in rows), timeout=0.2))
        self.release("bugtriage")
        code, output = self.result(process)
        self.assertNotEqual(0, code, output)
        self.assertTrue(any(row["name"] == "api_ready" for row in self.events), output)
        self.assertFalse(any(row["name"] == "assets" for row in self.events), output)

    def test_backup_failure_restores_writers_without_any_migration(self):
        for variable in ("FAIL_BACKUP", "FAIL_VERIFY"):
            with self.subTest(variable=variable):
                self.events.clear()
                self.environment[variable] = "1"
                code, output = self.result(self.workflow())
                self.assertNotEqual(0, code, output)
                self.assertFalse(any(row["name"] == "initializer_start" for row in self.events), output)
                self.assertFalse(any(row["name"] == "assets" for row in self.events), output)
                self.assertFalse(any(row["name"] == "service" and row["service"] in ("api", "mail-relay", "mail-inbox")
                                     for row in self.events), output)
                self.assertTrue(any(row["name"] == "command" and "start" in row.get("arguments", [])
                                    and "api" in row["arguments"] for row in self.events), output)
                del self.environment[variable]

    def test_main_failure_drains_children_before_releasing_deployment_lock(self):
        self.environment["FAIL_MAIN"] = "1"
        self.make_gate("tech-radar")
        process = self.workflow()
        self.assertTrue(self.wait_for(lambda rows: any(row["name"] == "main_failed" for row in rows)))
        self.assertTrue(self.wait_for(lambda rows: any(row.get("gate") == "tech-radar" for row in rows)))
        lock = self.root / "var/lock/fooddiary-deploy.lock"
        contender = subprocess.run(["flock", "-n", str(lock), "true"], capture_output=True)
        self.assertNotEqual(0, contender.returncode, "Deployment lock must remain held while tasks drain")
        self.assertFalse(self.wait_for(lambda rows: any(row["name"] == "process_exit" for row in rows), timeout=0.2))
        self.release("tech-radar")
        code, output = self.result(process)
        self.assertEqual(31, code, output)
        self.assertFalse(any(row["name"] == "assets" for row in self.events), output)
        self.assertEqual(0, subprocess.run(["flock", "-n", str(lock), "true"], capture_output=True).returncode)

    def test_initializers_are_serialized_across_primary_and_auxiliary_databases(self):
        services = ("db-init", "mailrelay-db-init", "mailinbox-db-init", "bugtriage")
        for service in services:
            self.make_gate("initializer-" + service)
        process = self.workflow()
        for count in range(1, 5):
            self.assertTrue(self.wait_for(lambda rows: sum(row["name"] == "initializer_start" for row in rows) >= count))
            starts = [row for row in self.events if row["name"] == "initializer_start"]
            self.assertEqual(count, len(starts))
            self.assertFalse(self.wait_for(
                lambda rows: sum(row["name"] == "initializer_start" for row in rows) > count, timeout=0.2))
            if count == 1:
                self.assertEqual("db-init", starts[0]["service"])
                self.assertFalse(any(row["name"] == "service" and row["service"] in ("api", "telegram-bot", "job-manager")
                                     for row in self.events))
            self.release("initializer-" + starts[-1]["service"])
        code, output = self.result(process)
        self.assertEqual(0, code, output)
        active = 0
        for row in self.events:
            if row["name"] == "initializer_start":
                active += 1
                self.assertEqual(1, active)
            elif row["name"] == "initializer_end":
                active -= 1
        self.assertEqual(0, active)

    def test_publication_failure_cleans_image_container_without_starting_ssr(self):
        self.environment["FAIL_PUBLISH"] = "1"
        code, output = self.result(self.workflow())
        self.assertEqual(1, code, output)
        self.assertIn("exit status 41", output)
        self.assertFalse(any(row["name"] == "service" and row["service"] == "client-ssr" for row in self.events))
        self.assertTrue(any(row["name"] == "command" and row.get("arguments") == ["rm", "fixture-image"]
                            for row in self.events), output)

    def test_primary_and_frontend_order(self):
        self.make_gate("backup-verify")
        process = self.workflow()
        self.assertTrue(self.wait_for(lambda rows: any(row.get("gate") == "backup-verify" for row in rows)))
        self.assertFalse(any(row["name"] == "initializer_start" for row in self.events))
        self.release("backup-verify")
        code, output = self.result(process)
        self.assertEqual(0, code, output)
        rows = self.events
        index = lambda predicate: next(i for i, row in enumerate(rows) if predicate(row))
        stopped = index(lambda row: row["name"] == "command" and "stop" in row.get("arguments", []))
        backup = index(lambda row: row["name"] == "backup")
        verified = index(lambda row: row["name"] == "backup_verified")
        migration = index(lambda row: row["name"] == "initializer_end" and row["service"] == "db-init")
        api = index(lambda row: row["name"] == "service" and row["service"] == "api")
        assets = index(lambda row: row["name"] == "assets")
        ssr = index(lambda row: row["name"] == "service" and row["service"] == "client-ssr")
        nginx = index(lambda row: row["name"] == "service" and row["service"] == "nginx")
        self.assertEqual(sorted([stopped, backup, verified, migration, api, assets, ssr, nginx]),
                         [stopped, backup, verified, migration, api, assets, ssr, nginx])
        for i, row in enumerate(rows):
            if row["name"] == "service" and row["service"] in ("api", "telegram-bot", "job-manager"):
                self.assertGreater(i, migration)


class ParallelTaskTests(DeploymentFixture):
    def setUp(self):
        super().setUp()
        self.logs = self.root / "logs"
        self.logs.mkdir(mode=0o700)
        (self.root / "task.py").write_text('''import sys
from trace_fixture import emit, gate
name = sys.argv[1]
emit('task_start', task=name)
gate(name)
print('log:' + name)
emit('task_end', task=name)
''')

    def batch(self, limit="2", names=("alpha", "beta", "gamma"), definitions=""):
        functions = '''alpha() { python3 "$FIXTURE/task.py" alpha; }
beta() { python3 "$FIXTURE/task.py" beta; }
gamma() { python3 "$FIXTURE/task.py" gamma; }
'''
        source = functions + definitions + '\nexport -f alpha beta gamma\n'
        arguments = [str(HELPER), str(self.logs), limit, *names]
        return self.start(source + "bash " + " ".join(shlex.quote(value) for value in arguments) + "\n")

    def test_two_workers_and_queued_task_obey_the_concurrency_bound(self):
        self.make_gate("alpha")
        self.make_gate("beta")
        process = self.batch()
        self.assertTrue(self.wait_for(lambda rows: sum(row["name"] == "task_start" for row in rows) == 2))
        self.assertFalse(self.wait_for(lambda rows: any(row.get("task") == "gamma" for row in rows), timeout=0.2))
        self.release("alpha")
        self.assertTrue(self.wait_for(lambda rows: any(row.get("task") == "gamma" for row in rows)))
        self.release("beta")
        code, output = self.result(process)
        self.assertEqual(0, code, output)
        active = 0
        for row in self.events:
            if row["name"] == "task_start":
                active += 1
                self.assertLessEqual(active, 2)
            elif row["name"] == "task_end":
                active -= 1
        self.assertEqual(0, active)

    def test_early_failure_is_retained_and_later_commands_cannot_hide_it(self):
        self.make_gate("beta")
        process = self.batch(definitions="alpha() { echo before_failure; false; echo forbidden_success; }")
        self.assertTrue(self.wait_for(lambda rows: any(row.get("gate") == "beta" for row in rows)))
        self.assertFalse(self.wait_for(lambda rows: any(row["name"] == "process_exit" for row in rows), timeout=0.2))
        self.release("beta")
        code, output = self.result(process)
        self.assertNotEqual(0, code)
        self.assertIn("before_failure", output)
        self.assertNotIn("forbidden_success", output)
        self.assertTrue(any(row["name"] == "task_end" and row["task"] == "gamma" for row in self.events))

    def test_already_exited_workers_are_waited_and_logs_are_private(self):
        code, output = self.result(self.batch(definitions="alpha() { echo alpha_log; return 7; }\nbeta() { :; }\ngamma() { :; }"))
        self.assertEqual(1, code, output)
        self.assertIn("exit=7", output)
        self.assertIn("gamma finished: exit=0", output)
        self.assertEqual(0o700, self.logs.stat().st_mode & 0o777)
        self.assertEqual({"alpha.log", "beta.log", "gamma.log"}, {path.name for path in self.logs.iterdir()})
        for path in self.logs.iterdir():
            self.assertEqual(0o600, path.stat().st_mode & 0o777)

    def test_invalid_or_duplicate_inputs_start_no_worker(self):
        for limit, names in (("0", ("alpha",)), ("3", ("alpha",)), ("invalid", ("alpha",)),
                             ("2", ()), ("2", ("alpha", "alpha")), ("2", ("unknown",)),
                             ("2", ("alpha;echo injected",))):
            with self.subTest(limit=limit, names=names):
                code, output = self.result(self.batch(limit, names))
                self.assertEqual(64, code, output)
                self.assertFalse(any(row["name"] == "task_start" for row in self.events))
                self.assertEqual([], list(self.logs.iterdir()))

    def test_existing_log_cannot_be_overwritten(self):
        path = self.logs / "alpha.log"
        path.write_text("retained evidence")
        code, output = self.result(self.batch())
        self.assertEqual(64, code, output)
        self.assertEqual("retained evidence", path.read_text())
        self.assertFalse(any(row["name"] == "task_start" for row in self.events))


if __name__ == "__main__":
    unittest.main()
