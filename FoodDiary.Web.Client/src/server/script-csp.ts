import { randomBytes } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

import sources from '../app/shared/security/script-csp-sources.json';

const NONCE_BYTES = 24;
let trustedHashes: string | null = null;

export function assertScriptCspReady(): string {
    if (trustedHashes === null) {
        const value = readFileSync(join(import.meta.dirname, 'script-csp-hashes.txt'), 'utf8').trim();
        if (value.length > 0 && !value.split(' ').every(hash => /^'sha256-[A-Za-z0-9+/]{43}='$/u.test(hash))) {
            throw new Error('Invalid trusted build script hash manifest.');
        }
        trustedHashes = value;
    }
    return trustedHashes;
}

export function createSsrScriptPolicy(): { nonce: string; policy: string } {
    const hashes = assertScriptCspReady();
    const nonce = randomBytes(NONCE_BYTES).toString('base64');
    return { nonce, policy: `script-src 'self' ${hashes} 'nonce-${nonce}' ${sources.join(' ')};` };
}
