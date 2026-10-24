import { createHash } from 'node:crypto';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { basename, dirname, resolve } from 'node:path';
import { runGraphProcess } from './code-graph-process.mjs';

const hash = (path) => existsSync(path)
  ? createHash('sha256').update(readFileSync(path)).digest('hex') : null;

export async function ensureDotnetToolRuntime({ repositoryRoot, projectPath, assemblyPath, sourcePaths = [], signal }) {
  const inputs = [projectPath, ...sourcePaths, ...['Directory.Build.props', 'Directory.Packages.props', 'global.json', 'NuGet.Config']
    .map((path) => resolve(repositoryRoot, path))];
  const fingerprint = createHash('sha256').update(JSON.stringify(inputs.map((path) => [path, hash(path)]))).digest('hex');
  const markerPath = `${assemblyPath}.build-inputs.json`;
  const stem = assemblyPath.replace(/\.dll$/, '');
  function readOutputs() {
    const dependencies = JSON.parse(readFileSync(`${stem}.deps.json`, 'utf8'));
    const target = dependencies.targets[dependencies.runtimeTarget.name];
    if (!target) throw new Error('The runtime dependency target is missing.');
    const files = new Set([assemblyPath, `${stem}.deps.json`, `${stem}.runtimeconfig.json`]);
    for (const library of Object.values(target)) {
      for (const asset of Object.keys(library.runtime ?? {})) {
        if (asset.endsWith('.dll')) files.add(resolve(dirname(assemblyPath), basename(asset)));
      }
    }
    return [...files].sort().map((path) => {
      const value = hash(path);
      if (!value) throw new Error(`Missing runtime dependency: ${path}`);
      return [path, value];
    });
  }
  try {
    const marker = JSON.parse(readFileSync(markerPath, 'utf8'));
    if (marker.fingerprint === fingerprint && JSON.stringify(marker.outputs) === JSON.stringify(readOutputs())) return false;
  } catch {
    // An old, incomplete or damaged private snapshot must rebuild its runtime.
  }
  await runGraphProcess('dotnet', ['build', projectPath, '-c', 'Release', '--no-incremental', '--nologo', '--verbosity', 'quiet'], { cwd: repositoryRoot, signal });
  const outputs = readOutputs();
  writeFileSync(markerPath, `${JSON.stringify({ schemaVersion: 1, fingerprint, outputs })}\n`, 'utf8');
  return true;
}
