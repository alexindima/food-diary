import {
    closeSync,
    existsSync,
    openSync,
    readFileSync,
    unlinkSync,
    writeFileSync,
} from "node:fs";
import { join } from "node:path";
import { processIdentity } from "./task-runtime.mjs";

export function acquireRuntimeCheck(state, purpose) {
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
    return () => {
        closeSync(fd);
        unlinkSync(path);
    };
}
