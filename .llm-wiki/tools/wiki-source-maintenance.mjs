import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync, lstatSync } from 'node:fs';
import { resolve } from 'node:path';
import { repairWikiReferences } from './code-graph-maintenance.mjs';

const root = resolve(import.meta.dirname, '../..');
const options = Object.fromEntries(process.argv.slice(2).map(argument => {
  const index = argument.indexOf('=');
  return [argument.slice(2, index), argument.slice(index + 1)];
}));
const git = args => execFileSync('git', ['-C', root, ...args], { encoding: 'utf8', maxBuffer: 32 * 1024 * 1024 });
const paths = git(['ls-files', '--cached', '--others', '--exclude-standard', '-z']).split('\0').filter(Boolean);
const existing = new Set(paths.filter(path => existsSync(resolve(root, path))));
// Markdown commonly links directories as well as files.
for (const path of [...existing]) {
  let parent = path;
  while (parent.includes('/')) { parent = parent.slice(0, parent.lastIndexOf('/')); existing.add(parent); }
}
const moves = new Map();
const changes = git(['diff', '--name-status', '--find-renames=100%', '-z', options.base ?? 'HEAD', '--']).split('\0');
for (let i = 0; i < changes.length && changes[i];) {
  const status = changes[i++];
  const source = changes[i++];
  if (status.startsWith('R')) { const target = changes[i++]; if (status === 'R100') moves.set(source, target); }
  else if (status.startsWith('C')) i++;
}
const findings = [];
const changedPages = [];
for (const path of existing) {
  if (!path.startsWith('.llm-wiki/') || !path.endsWith('.md')) continue;
  const absolute = resolve(root, path);
  if (lstatSync(absolute).isSymbolicLink()) continue;
  const text = readFileSync(absolute, 'utf8');
  const result = repairWikiReferences(text, path, moves, existing);
  findings.push(...result.findings);
  // Generated pages belong to their generators; never patch their projection.
  const generated = /^generated_by:/m.test(text);
  if (options.repair === 'true' && result.text !== text && !generated) {
    if (readFileSync(absolute, 'utf8') !== text) throw new Error(`Concurrent edit: ${path}`);
    writeFileSync(absolute, result.text, 'utf8');
    changedPages.push(path);
  }
}
process.stdout.write(JSON.stringify({ findings, changedPages, confirmedMoves: moves.size }) + '\n');
