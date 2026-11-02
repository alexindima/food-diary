import { randomUUID } from 'node:crypto';
import { open, readFile, rename, rm } from 'node:fs/promises';
import { basename, dirname, join } from 'node:path';

// Replacing a completed sibling file also works when a Windows reader has mapped
// the previous output. Never truncate a published generated artifact in place.
export async function writeGeneratedFile(target, content) {
    const bytes = Buffer.from(content);
    try {
        if ((await readFile(target)).equals(bytes)) return;
    } catch (error) {
        if (error.code !== 'ENOENT') throw error;
    }
    const temporary = join(dirname(target), `.${basename(target)}.${randomUUID()}.tmp`);
    let handle;
    try {
        handle = await open(temporary, 'wx');
        try {
            await handle.writeFile(bytes);
        } finally {
            await handle.close();
        }
        await rename(temporary, target);
    } finally {
        if (handle !== undefined) await rm(temporary, { force: true });
    }
}
