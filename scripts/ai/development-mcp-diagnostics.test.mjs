import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { diagnoseDevelopmentMcp, readDevelopmentMcpConfiguration } from './development-mcp-diagnostics.mjs';

test('MCP diagnostic resolves cwd and launcher from the repository working directory', () => {
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
    assert.equal(config.cwd, root, 'MCP cwd must follow the Codex repository working directory');
    assert.equal(config.launcherPath, join(root, 'scripts/launcher.ps1'));
    assert.equal(config.launcherExists, true);

    writeFileSync(join(root, '.codex/config.toml'), [
      '[mcp_servers.fooddiary_development]',
      'command = "powershell.exe"',
      'args = ["-File", "scripts/launcher.ps1"]',
      'cwd = ".."',
      '',
    ].join('\n'));
    assert.equal(readDevelopmentMcpConfiguration(root).cwd, resolve(root, '..'));
  } finally {
    rmSync(root, { recursive: true, force: true });
  }
});

async function withStdioServer(run, { successful = true, grace = 1 } = {}) {
  const root = mkdtempSync(join(tmpdir(), 'fooddiary-mcp-probe-'));
  try {
    mkdirSync(join(root, '.codex'));
    writeFileSync(join(root, '.codex/config.toml'), [
      `mcp_optional_startup_grace_ms = ${grace}`,
      '[mcp_servers.fooddiary_development]',
      `command = ${JSON.stringify(process.execPath)}`,
      'args = ["server.mjs"]',
      'startup_timeout_sec = 15',
      'enabled_tools = ["get_server_status"]',
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
    await run(await diagnoseDevelopmentMcp(root, 10000));
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
