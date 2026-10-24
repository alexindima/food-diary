import { createHash } from 'node:crypto';
import { readFile, writeFile, mkdir, rename, readdir, lstat } from 'node:fs/promises';
import path from 'node:path';

export const sha256 = value => createHash('sha256').update(value).digest('hex');
export const gitBlob = value =>
    createHash('sha1')
        .update(`blob ${Buffer.byteLength(value)}\0`)
        .update(value)
        .digest('hex');
export const readJson = async file => JSON.parse(await readFile(file, 'utf8'));
export async function writeJson(file, value) {
    await mkdir(path.dirname(file), { recursive: true });
    const temporary = `${file}.${process.pid}.${crypto.randomUUID()}.tmp`;
    await writeFile(temporary, `${JSON.stringify(value, null, 2)}\n`, 'utf8');
    await rename(temporary, file);
}

export function within(root, relative) {
    if (typeof relative !== 'string' || !relative || path.isAbsolute(relative) || relative.includes('\\')) {
        throw new Error(`Expected a portable relative path: ${relative}`);
    }
    const target = path.resolve(root, relative);
    const difference = path.relative(path.resolve(root), target);
    if (!difference || difference.startsWith('..') || path.isAbsolute(difference)) throw new Error(`Path escapes its root: ${relative}`);
    return target;
}

export async function listFiles(root, extensions, excluded = new Set(['node_modules', 'dist', '.angular', 'coverage'])) {
    const files = [];
    async function visit(directory) {
        const entries = await readdir(directory, { withFileTypes: true });
        for (const entry of entries.sort((a, b) => a.name.localeCompare(b.name))) {
            const file = path.join(directory, entry.name);
            if (entry.isSymbolicLink()) throw new Error(`Source scope contains a symbolic link: ${file}`);
            if (entry.isDirectory() && !excluded.has(entry.name)) await visit(file);
            if (entry.isFile() && (!extensions || extensions.has(path.extname(entry.name)))) files.push(file);
        }
    }
    if (!(await lstat(root)).isDirectory()) throw new Error(`Source scope is not a directory: ${root}`);
    await visit(root);
    return files;
}

export const portable = file => file.split(path.sep).join('/');
