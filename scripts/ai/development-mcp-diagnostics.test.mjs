import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { diagnoseDevelopmentMcp, readDevelopmentMcpConfiguration } from './development-mcp-diagnostics.mjs';

test('MCP diagnostic does not silently anchor a relative cwd to the repository', () => {
  const root = mkdtempSync(join(tmpdir(), 'fooddiary-mcp-cwd-'));
  try {
    mkdirSync(join(root, '.codex'));
    mkdirSync(join(root, 'scripts'));
    writeFileSync(join(root, 'scripts/launcher.ps1'), '# fixture');
    writeFileSync(join(root, '.codex/config.toml'), [
      '[mcp_servers.fooddiary_development]',
      'command = "powershell.exe"',
      'args = ["-File", "scripts/launcher.ps1"]',
      'cwd = "."',
      'enabled = true',
      'enabled_tools = ["get_server_status"]',
      '',
    ].join('\n'));
    const config = readDevelopmentMcpConfiguration(root);
    assert.equal(config.cwd, process.cwd(), 'Relative MCP cwd follows the launching process, not the repository');
    assert.equal(config.launcherPath, resolve('scripts/launcher.ps1'));

    writeFileSync(join(root, '.codex/config.toml'), [
      '[mcp_servers.fooddiary_development]',
      'command = "powershell.exe"',
      'args = ["-File", "scripts/launcher.ps1"]',
      'cwd = ".."',
      '',
    ].join('\n'));
    assert.equal(readDevelopmentMcpConfiguration(root).cwd, resolve('..'));
  } finally {
    rmSync(root, { recursive: true, force: true });
  }
});

async function withStdioServer(run, { successful = true, grace = 1, cwd = 'absolute', angularCwd, angularEnabled = true, cliCwd } = {}) {
  const root = mkdtempSync(join(tmpdir(), 'fooddiary mcp probe '));
  try {
    mkdirSync(join(root, '.codex'));
    writeFileSync(join(root, '.codex/config.toml'), [
      `mcp_optional_startup_grace_ms = ${grace}`,
      '[mcp_servers.fooddiary_development]',
      `command = ${JSON.stringify(process.execPath)}`,
      'args = ["server.mjs"]',
      ...(cwd === 'missing' ? [] : [`cwd = ${JSON.stringify(cwd === 'absolute' ? root : cwd)}`]),
      'startup_timeout_sec = 15',
      'enabled_tools = ["get_server_status"]',
      ...(angularCwd === undefined ? [] : [
        '[mcp_servers.angular_cli]',
        `command = ${JSON.stringify(process.execPath)}`,
        'args = []',
        `cwd = ${JSON.stringify(angularCwd === 'absolute' ? root : angularCwd)}`,
        `enabled = ${angularEnabled}`,
      ]),
      '',
    ].join('\n'));
    writeFileSync(join(root, 'server.mjs'), `
      import { createInterface } from 'node:readline';
      const lines = createInterface({ input: process.stdin });
      lines.on('line', async line => {
        const request = JSON.parse(line);
        if (!request.id) return;
        let result;
        if (request.method === 'initialize') {
          await new Promise(resolve => setTimeout(resolve, 25));
          result = { serverInfo: { name: 'fixture' } };
        } else if (request.method === 'tools/list') {
          result = { tools: [{ name: 'get_server_status' }] };
        } else {
          result = { structuredContent: { success: ${successful}, data: {
            runningCodeMatchesRepositoryHead: false, runningCodeIncludesWorktreeChanges: true,
            indexesMatchWorktree: false, indexStatusCode: 'index_stale'
          } } };
        }
        process.stdout.write(JSON.stringify({ jsonrpc: '2.0', id: request.id, result }) + '\\n');
      });
    `);
    if (cliCwd) {
      const diagnosticPath = fileURLToPath(new URL('./development-mcp-diagnostics.mjs', import.meta.url));
      const result = spawnSync(process.execPath, [diagnosticPath, '--repository-root', root], {
        cwd: cliCwd === 'repository' ? root : cliCwd, encoding: 'utf8', timeout: 15000, windowsHide: true,
      });
      assert.ifError(result.error);
      await run(JSON.parse(result.stdout), result.status);
    } else {
      await run(await diagnoseDevelopmentMcp(root, 10000));
    }
  } finally {
    rmSync(root, { recursive: true, force: true });
  }
}

test('a healthy standalone probe reports optional catalog timing without claiming chat readiness', async () => {
  await withStdioServer(result => {
    assert.equal(result.ok, true);
    assert.equal(result.connectionAssessment.nativeChatCatalog, 'not-observed');
    assert.equal(result.connectionAssessment.mayMissOptionalStartupWindow, true);
    assert.ok(result.connectionAssessment.catalogReadyMilliseconds >= 25);
    assert.equal(result.connectionAssessment.runtimeCode, 'current-sources');
    assert.equal(result.connectionAssessment.indexes, 'stale');
    assert.ok(result.recoveryActions.some(action => action.includes('mcp_optional_startup_grace_ms')));
  });
});

test('a relative FoodDiary cwd cannot produce a healthy diagnostic even from the repository', async () => {
  await withStdioServer((result, exitCode) => {
    assert.equal(result.ok, false, 'A relative MCP cwd must not be reported as healthy');
    assert.equal(result.reason, 'invalid-working-directory');
    assert.equal(result.workingDirectoryIssues[0].serverName, 'fooddiary_development');
    assert.equal(result.workingDirectoryIssues[0].reason, 'working-directory-not-absolute');
    assert.equal(exitCode, 1);
  }, { cwd: '.', cliCwd: 'repository' });
});

test('an omitted cwd cannot hide dependency on the launching directory', async () => {
  await withStdioServer(result => {
    assert.equal(result.ok, false, 'An omitted MCP cwd must not be reported as healthy');
    assert.equal(result.workingDirectoryIssues[0].configuredCwd, null);
    assert.equal(result.workingDirectoryIssues[0].reason, 'working-directory-not-absolute');
  }, { cwd: 'missing' });
});

test('a relative Angular cwd fails the diagnostic even when FoodDiary is correctly configured', async () => {
  await withStdioServer(result => {
    assert.equal(result.ok, false, 'A relative Angular cwd must not be ignored');
    assert.equal(result.workingDirectoryIssues[0].serverName, 'angular_cli');
    assert.equal(result.workingDirectoryIssues[0].reason, 'working-directory-not-absolute');
    assert.ok(result.recoveryActions.some(action => action.includes('absolute cwd')));
  }, { angularCwd: '.' });
});

test('a disabled server does not block a healthy configured server', async () => {
  await withStdioServer(result => assert.equal(result.ok, true), { angularCwd: '.', angularEnabled: false });
});

test('a nonexistent absolute cwd is reported before spawning the server', async () => {
  await withStdioServer(result => {
    assert.equal(result.ok, false);
    assert.equal(result.workingDirectoryIssues[0].reason, 'working-directory-not-found');
  }, { angularCwd: join(tmpdir(), 'fooddiary-mcp-missing-parent', 'does-not-exist') });
});

test('absolute server directories work when the diagnostic starts outside the repository', async () => {
  await withStdioServer((result, exitCode) => {
    assert.equal(result.ok, true);
    assert.equal(exitCode, 0);
    assert.equal(result.connectionAssessment.nativeChatCatalog, 'not-observed');
  }, { angularCwd: 'absolute', cliCwd: tmpdir() });
});

test('waiting for the configured timeout removes the optional grace warning', async () => {
  await withStdioServer(result => {
    assert.equal(result.configuration.optionalStartupGraceMilliseconds, 0);
    assert.equal(result.connectionAssessment.mayMissOptionalStartupWindow, false);
    assert.equal(result.connectionAssessment.nativeChatCatalog, 'not-observed');
  }, { grace: 0 });
});

test('an application-level failed status is not reported as a healthy MCP connection', async () => {
  await withStdioServer(result => {
    assert.equal(result.ok, false);
    assert.equal(result.connectionAssessment.standalone, 'failed');
    assert.equal(result.connectionAssessment.runtimeCode, 'unknown');
    assert.equal(result.connectionAssessment.indexes, 'unknown');
  }, { successful: false });
});
