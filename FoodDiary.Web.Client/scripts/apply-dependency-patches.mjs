import { createHash } from 'node:crypto';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Temporary mitigation for GHSA-vfj7-8cjw-p6xm. Review/remove on an upstream fix.
const hashes = {
    'parse.js': 'e572166565f15fa6ad9865ae49d678218e32aabfd1b3720f6d0d43d39800d310',
    'compile.js': 'dc98f22eee3d511785d92a00758d5f0d48efed5f5813bdecc2de430c529b5c9f',
    'expand.js': '41ccc196ebfa7b7781a634e721eb744e4e7bcb54cba427a7e3d6806a1b9e58f7',
    'stringify.js': '379f22d77bfa1478341ccd49c5e4267464aabcbba03558bab332aac23fc6f23a',
};
const parserGuard = "\n      if (stack.length > 128) throw new SyntaxError('Brace nesting exceeds 128 levels');";
const astGuard = "\n  require('./fooddiary-depth-limit')(ast);";
const depthLimit = `'use strict';
module.exports = ast => {
  const ancestors = new Set();
  const pending = [[ast, 0, false]];
  while (pending.length) {
    const [node, depth, leaving] = pending.pop();
    if (!node || typeof node !== 'object') continue;
    if (leaving) { ancestors.delete(node); continue; }
    if (depth > 128 || ancestors.has(node)) throw new SyntaxError('Brace nesting exceeds 128 levels or contains a cycle');
    ancestors.add(node);
    pending.push([node, depth, true]);
    if (Array.isArray(node.nodes)) {
      for (const child of node.nodes) pending.push([child, depth + 1, false]);
    }
  }
};
`;

export async function applyDependencyPatches(root, checkOnly = false) {
    const lock = JSON.parse(await readFile(path.join(root, 'package-lock.json'), 'utf8'));
    const locations = Object.keys(lock.packages).filter(key => /(^|\/)node_modules\/braces$/.test(key));
    if (!locations.length) throw new Error('Review obsolete braces mitigation: no installed consumers');
    const writes = [];
    for (const location of locations) {
        const directory = path.resolve(root, location);
        if (!directory.startsWith(path.resolve(root, 'node_modules') + path.sep)) throw new Error('Invalid dependency path');
        const manifest = JSON.parse(await readFile(path.join(directory, 'package.json'), 'utf8'));
        if (manifest.name !== 'braces' || manifest.version !== '3.0.3') throw new Error('Review braces mitigation for the new version');
        for (const [name, expectedHash] of Object.entries(hashes)) {
            const filename = path.join(directory, 'lib', name);
            const installed = (await readFile(filename, 'utf8')).replaceAll('\r\n', '\n');
            const original = installed.replaceAll(parserGuard, '').replaceAll(astGuard, '');
            if (createHash('sha256').update(original).digest('hex') !== expectedHash)
                throw new Error(`Unexpected braces source: ${filename}`);
            const patched =
                name === 'parse.js'
                    ? original.replaceAll('stack.push(block);', 'stack.push(block);' + parserGuard)
                    : original.replace('(ast, options = {}) => {', '(ast, options = {}) => {' + astGuard);
            if (checkOnly && installed !== patched) throw new Error('Missing braces mitigation; run npm install with scripts enabled');
            if (installed !== patched) writes.push([filename, patched]);
        }
        const helper = path.join(directory, 'lib', 'fooddiary-depth-limit.js');
        const installedHelper = await readFile(helper, 'utf8').catch(error => {
            if (error.code === 'ENOENT') return null;
            throw error;
        });
        if (checkOnly && installedHelper !== depthLimit) throw new Error('Missing or modified braces depth guard');
        if (installedHelper !== depthLimit) writes.push([helper, depthLimit]);
    }
    for (const [filename, content] of writes) await writeFile(filename, content);
    return locations.length;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    const root = fileURLToPath(new URL('..', import.meta.url));
    const count = await applyDependencyPatches(root, process.argv.includes('--check'));
    console.log(`Verified braces mitigation in ${count} installation(s).`);
}
