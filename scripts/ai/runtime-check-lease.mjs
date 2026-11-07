import {
    closeSync,
    existsSync,
    openSync,
    readFileSync,
    unlinkSync,
    writeFileSync,
} from "node:fs";
import { join } from "node:path";
import { DatabaseSync } from "node:sqlite";
import { processIdentity } from "./task-runtime.mjs";

export function acquireRuntimeCheck(state, purpose) {
    // Keep this inode stable. SQLite's OS write lock is released on process
    // death and serializes stale metadata recovery without unlink/create ABA.
    const guard = new DatabaseSync(join(state.directory, 'verification-lock.sqlite'));
    try {
        guard.exec('PRAGMA busy_timeout=0; BEGIN IMMEDIATE');
    } catch (error) {
        guard.close();
        throw new Error('Another verification owns this runtime; recovery, load and browser writes must be sequential', { cause: error });
    }
    try {
    const path = join(state.directory, "verification.lock");
    if (existsSync(path)) {
        let previous;
        try {
            previous = JSON.parse(readFileSync(path, "utf8"));
        } catch {
            throw new Error(
                "Owned verification lock is malformed; inspect before retrying",
            );
        }
        if (
            previous.ownerId !== state.id ||
            processIdentity(previous.pid) === previous.identity
        )
            throw new Error(
                "Another verification owns this runtime; recovery, load and browser writes must be sequential",
            );
        unlinkSync(path);
    }
    const fd = openSync(path, "wx", 0o600);
    writeFileSync(
        fd,
        JSON.stringify({
            ownerId: state.id,
            pid: process.pid,
            identity: processIdentity(process.pid),
            purpose,
        }),
    );
    let released = false;
    return () => {
        if (released) return;
        released = true;
        try { closeSync(fd); unlinkSync(path); }
        finally { guard.close(); }
    };
    } catch (error) {
        guard.close();
        throw error;
    }
}
