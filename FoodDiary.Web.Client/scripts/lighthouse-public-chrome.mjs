import { mkdtempSync, realpathSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';

const prefix = 'fd-public-lighthouse-';
const temporaryRoot = realpathSync(tmpdir());

export function createAuditProfile() {
    return mkdtempSync(path.join(temporaryRoot, prefix));
}

export function removeAuditProfile(profile, remove = rmSync) {
    const target = path.resolve(profile);
    if (path.dirname(target) !== temporaryRoot || !path.basename(target).startsWith(prefix)) {
        throw new Error('Refusing to remove a directory outside the owned Lighthouse temporary profile scope.');
    }
    try {
        if (realpathSync(target) !== target) throw new Error('Refusing to remove a redirected Lighthouse profile.');
        remove(target, { recursive: true, force: true, maxRetries: 3, retryDelay: 100 });
        return true;
    } catch (error) {
        if (error.code === 'ENOENT') return true;
        if (['EPERM', 'EBUSY', 'EACCES'].includes(error.code)) return false;
        throw error;
    }
}
