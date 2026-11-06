import { spawn, spawnSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { createInterface } from 'node:readline';
import { pathToFileURL } from 'node:url';

export function readDevelopmentMcpConfiguration(repositoryRoot) {
  const configPath = resolve(repositoryRoot, '.codex/config.toml');
  const text = readFileSync(configPath, 'utf8');
  const section = text.match(/^\[mcp_servers\.fooddiary_development\]\s*\r?\n([\s\S]*?)(?=^\[|$(?![\s\S]))/m)?.[1];
  if (!section) throw new Error('Project configuration has no FoodDiary Development MCP server.');
  const values = Object.fromEntries([...section.matchAll(/^([a-z_]+)\s*=\s*(.+)$/gm)].map(([, key, value]) => [key, JSON.parse(value.trim())]));
  const cwd = resolve(repositoryRoot, values.cwd ?? '.');
  const fileIndex = values.args.indexOf('-File');
  const launcherPath = fileIndex >= 0 ? resolve(cwd, values.args[fileIndex + 1]) : null;
  return { configPath, cwd, command: values.command, args: values.args, enabled: values.enabled !== false,
    launcherPath, launcherExists: launcherPath === null || existsSync(launcherPath),
    enabledTools: values.enabled_tools ?? [], required: values.required === true,
    startupTimeoutSeconds: values.startup_timeout_sec ?? 10,
    optionalStartupGraceMilliseconds: Number(text.split(/^\[/m)[0].match(/^mcp_optional_startup_grace_ms\s*=\s*(\d+)\s*(?:#.*)?$/m)?.[1] ?? 1000) };
}

function readStatusPayload(status) {
  if (status.structuredContent) return status.structuredContent;
  for (const item of status.content ?? []) {
    if (item.type !== 'text') continue;
    try { return JSON.parse(item.text); } catch { /* Other content is not a status envelope. */ }
  }
  return null;
}

export async function diagnoseDevelopmentMcp(repositoryRoot, timeoutMilliseconds = 120000) {
  const configuration = readDevelopmentMcpConfiguration(repositoryRoot);
  if (!configuration.enabled || !configuration.launcherExists) return { ok: false, configuration, reason: !configuration.enabled ? 'server-disabled' : 'launcher-not-found' };
  const child = spawn(configuration.command, configuration.args, { cwd: configuration.cwd, stdio: ['pipe', 'pipe', 'pipe'], windowsHide: true });
  const pending = new Map();
  let nextId = 0;
  let stderrBytes = 0;
  let timer;
  const failed = (error) => { for (const request of pending.values()) request.reject(error); pending.clear(); };
  child.on('error', failed);
  child.stdin.on('error', failed);
  child.on('exit', (code, signal) => failed(new Error(`MCP process ended before its response (exit=${code}, signal=${signal}).`)));
  child.stderr.on('data', data => { stderrBytes += data.length; });
  const lines = createInterface({ input: child.stdout });
  lines.on('line', line => {
    try {
      const response = JSON.parse(line);
      const request = pending.get(response.id);
      if (!request) return;
      pending.delete(response.id);
      if (response.error) request.reject(new Error(`MCP protocol error ${response.error.code}: ${response.error.message}`));
      else request.resolve(response.result);
    } catch (error) { failed(new Error('MCP stdout is not a valid JSON protocol stream.', { cause: error })); }
  });
  const request = (method, params) => new Promise((resolveResponse, reject) => {
    const id = ++nextId;
    pending.set(id, { resolve: resolveResponse, reject });
    child.stdin.write(`${JSON.stringify({ jsonrpc: '2.0', id, method, params })}\n`);
  });
  const started = performance.now();
  const timeout = new Promise((_, reject) => {
    timer = setTimeout(() => reject(new Error(`MCP diagnostic exceeded ${timeoutMilliseconds}ms.`)), timeoutMilliseconds);
  });
  const probe = async () => {
    const initialization = await request('initialize', { protocolVersion: '2025-03-26', capabilities: {}, clientInfo: { name: 'fooddiary-development-mcp-diagnostics', version: '1.0' } });
    const startupMilliseconds = Math.round(performance.now() - started);
    child.stdin.write(`${JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized' })}\n`);
    const catalog = await request('tools/list', {});
    const catalogReadyMilliseconds = Math.round(performance.now() - started);
    const toolNames = catalog.tools.map(tool => tool.name).sort();
    const missingTools = configuration.enabledTools.filter(name => !toolNames.includes(name));
    const status = await request('tools/call', { name: 'get_server_status', arguments: {} });
    const payload = readStatusPayload(status);
    const ok = status.isError !== true && payload?.success === true && missingTools.length === 0;
    const runtime = payload?.success === true ? payload.data : null;
    const mayMissOptionalStartupWindow = !configuration.required && configuration.optionalStartupGraceMilliseconds > 0
      && catalogReadyMilliseconds >= configuration.optionalStartupGraceMilliseconds;
    const connectionAssessment = {
      standalone: ok ? 'healthy' : 'failed', nativeChatCatalog: 'not-observed',
      catalogReadyMilliseconds, mayMissOptionalStartupWindow,
      runtimeCode: runtime?.runningCodeIncludesWorktreeChanges === true ? 'current-sources'
        : runtime?.runningCodeIncludesWorktreeChanges === false ? 'stale' : 'unknown',
      revisionMatches: runtime?.runningCodeMatchesRepositoryHead ?? null,
      indexes: runtime?.indexesMatchWorktree === true ? 'current'
        : runtime?.indexesMatchWorktree === false ? 'stale' : 'unknown',
    };
    const recoveryActions = [
      'For missing native tools, open the trusted repository in Codex, verify its MCP server is enabled, then reconnect or start a new chat. This probe cannot refresh an existing chat catalog.',
    ];
    if (mayMissOptionalStartupWindow) recoveryActions.push('Set top-level mcp_optional_startup_grace_ms = 0 in the project .codex/config.toml so a new chat waits for startup_timeout_sec.');
    if (connectionAssessment.runtimeCode === 'stale') recoveryActions.push('Restart the configured --build-if-stale launcher; the running source fingerprint differs from the checkout.');
    if (connectionAssessment.indexes === 'stale') recoveryActions.push('Inspect ./.llm-wiki/wiki.ps1 verify-status before choosing the affected index refresh or verification. An old commit alone does not prove stale executable sources.');
    return { ok, configuration,
      serverInfo: initialization.serverInfo, startupMilliseconds, toolNames, missingTools,
      serverStatus: status.structuredContent ?? status.content, stderrBytes,
      connectionAssessment, recoveryActions,
      nativeChatCatalog: 'A healthy standalone connection does not prove that an existing chat refreshed its tools.' };
  };
  try { return await Promise.race([probe(), timeout]); }
  finally {
    clearTimeout(timer);
    child.stdin.end();
    if (child.exitCode === null) {
      await Promise.race([new Promise(resolveExit => child.once('exit', resolveExit)), new Promise(resolveWait => setTimeout(resolveWait, 3000))]);
      if (child.exitCode === null) {
        if (process.platform === 'win32') spawnSync('taskkill', ['/PID', String(child.pid), '/T', '/F'], { windowsHide: true, stdio: 'ignore' });
        else child.kill('SIGTERM');
      }
    }
    lines.close();
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  const rootArgument = process.argv.indexOf('--repository-root');
  const repositoryRoot = rootArgument >= 0 ? resolve(process.argv[rootArgument + 1]) : resolve(import.meta.dirname, '../..');
  try {
    const result = await diagnoseDevelopmentMcp(repositoryRoot);
    process.stdout.write(`${JSON.stringify(result, null, 2)}\n`);
    if (!result.ok) process.exitCode = 1;
  } catch (error) {
    process.stdout.write(`${JSON.stringify({ ok: false, error: error.message })}\n`);
    process.exitCode = 1;
  }
}
