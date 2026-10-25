import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { readDevelopmentMcpConfiguration } from './development-mcp-diagnostics.mjs';

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
