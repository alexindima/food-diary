import { createHash, randomUUID } from "node:crypto";
import { spawnSync } from "node:child_process";
import { mkdirSync, readFileSync, unlinkSync, writeFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { repositoryRoot } from "./generate-feature.mjs";
import { ownedContainer, ownedRuntime } from "./owned-runtime-client.mjs";
import { acquireRuntimeCheck } from "./runtime-check-lease.mjs";

export function restoreDatabaseName(value) {
    if (!/^fd_restore_[a-f0-9]{8}$/u.test(value))
        throw new Error("Only a fresh drill database name is permitted");
    return value;
}

export function sameSnapshot(left, right) {
    const canonical = (value) =>
        JSON.stringify(
            [...value].sort((a, b) => a.table.localeCompare(b.table)),
        );
    return canonical(left) === canonical(right);
}

const snapshotSql = `
CREATE TEMP TABLE drill_snapshot(table_name text, rows bigint, digest text);
DO $$ DECLARE item record; BEGIN
FOR item IN SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename LOOP
EXECUTE format('INSERT INTO drill_snapshot SELECT %L, count(*), md5(coalesce(string_agg(to_jsonb(t)::text, E''\\n'' ORDER BY to_jsonb(t)::text), '''')) FROM %I.%I t', item.tablename, 'public', item.tablename);
END LOOP; END $$;
SELECT coalesce(json_agg(json_build_object('table',table_name,'rows',rows,'digest',digest) ORDER BY table_name),'[]'::json) FROM drill_snapshot;
`;

const constraintSql = `
CREATE TEMP TABLE drill_constraints(table_name text, constraint_name text, definition text);
DO $$ DECLARE item record; canonical text; BEGIN
FOR item IN SELECT conrelid, conname, contype, pg_get_constraintdef(oid) AS definition FROM pg_constraint WHERE connamespace='public'::regnamespace LOOP
canonical := item.definition;
IF item.contype='c' THEN
EXECUTE format('CREATE TEMP TABLE drill_check_probe (LIKE %s)', item.conrelid::regclass);
EXECUTE format('ALTER TABLE drill_check_probe ADD CONSTRAINT drill_probe_check %s', item.definition);
SELECT pg_get_constraintdef(oid) INTO canonical FROM pg_constraint WHERE conrelid='drill_check_probe'::regclass AND conname='drill_probe_check';
DROP TABLE drill_check_probe;
END IF;
INSERT INTO drill_constraints VALUES(item.conrelid::regclass::text,item.conname,canonical);
END LOOP; END $$;
SELECT coalesce(json_agg(json_build_object('table',table_name,'name',constraint_name,'definition',definition) ORDER BY table_name,constraint_name),'[]'::json) FROM drill_constraints;
`;

export function recoveryDrill(name, root = repositoryRoot, maxSeconds = 300) {
    const release = acquireRuntimeCheck(ownedRuntime(name, root), "recovery");
    try {
        return executeRecovery(name, root, maxSeconds);
    } finally {
        release();
    }
}

function executeRecovery(name, root, maxSeconds) {
    if (!Number.isFinite(maxSeconds) || maxSeconds <= 0 || maxSeconds > 3600)
        throw new Error("Invalid local recovery budget");
    const state = ownedRuntime(name, root);
    const container = ownedContainer(state, "postgres");
    const target = restoreDatabaseName(
        `fd_restore_${randomUUID().slice(0, 8)}`,
    );
    const directory = join(root, ".artifacts/reliability", name);
    mkdirSync(directory, { recursive: true });
    const dumpPath = join(directory, `${target}.dump`);
    const reportPath = join(directory, "recovery.json");
    const started = performance.now();
    let created = false;
    const docker = (args, input, binary = false) => {
        ownedContainer(state, "postgres");
        const result = spawnSync(
            "docker",
            [
                "exec",
                ...(input === undefined ? [] : ["-i"]),
                container,
                ...args,
            ],
            {
                input,
                encoding: binary ? undefined : "utf8",
                windowsHide: true,
                timeout: 180_000,
                maxBuffer: 128 * 1024 * 1024,
            },
        );
        if (result.error || result.status !== 0)
            throw new Error(
                `Owned recovery command failed (${args[0]}); source database retained`,
            );
        return result.stdout;
    };
    const snapshot = (database) =>
        JSON.parse(
            docker([
                "psql",
                "-U",
                "postgres",
                "-d",
                database,
                "-v",
                "ON_ERROR_STOP=1",
                "-qAt",
                "-c",
                snapshotSql,
            ]),
        );
    const schema = (database) =>
        JSON.parse(
            docker([
                "psql",
                "-U",
                "postgres",
                "-d",
                database,
                "-v",
                "ON_ERROR_STOP=1",
                "-qAt",
                "-c",
                constraintSql,
            ]),
        );
    const report = {
        schemaVersion: 1,
        scope: "owned synthetic PostgreSQL database only; object storage and production RPO/RTO are not verified",
        runtime: name,
        sourceFingerprint: state.sourceFingerprint,
        status: "running",
        maximumSeconds: maxSeconds,
    };
    try {
        console.log(
            "Recovery: capturing a source data fingerprint and a consistent custom-format dump",
        );
        const before = snapshot("fooddiary_task");
        const schemaBefore = schema("fooddiary_task");
        const dump = docker(
            [
                "pg_dump",
                "-U",
                "postgres",
                "-d",
                "fooddiary_task",
                "--format=custom",
                "--no-owner",
                "--no-acl",
            ],
            undefined,
            true,
        );
        writeFileSync(dumpPath, dump, { mode: 0o600 });
        const dumpSha256 = createHash("sha256").update(dump).digest("hex");
        docker(["createdb", "-U", "postgres", "-T", "template0", target]);
        created = true;
        console.log(
            "Recovery: restoring into a fresh isolated database and comparing every public table",
        );
        docker(
            [
                "pg_restore",
                "-U",
                "postgres",
                "--dbname",
                target,
                "--exit-on-error",
                "--no-owner",
                "--no-acl",
            ],
            readFileSync(dumpPath),
        );
        const after = snapshot("fooddiary_task");
        const restored = snapshot(target);
        const schemaRestored = schema(target);
        const constraintsEqual =
            JSON.stringify(schemaBefore) === JSON.stringify(schemaRestored);
        Object.assign(report, {
            rowContentsEqual: sameSnapshot(before, restored),
            constraintsEqual,
            differentTables: before
                .filter(
                    (table) =>
                        !restored.some(
                            (other) =>
                                other.table === table.table &&
                                other.rows === table.rows &&
                                other.digest === table.digest,
                        ),
                )
                .map((table) => table.table),
            differentConstraints: schemaBefore.filter(
                (item) =>
                    !schemaRestored.some(
                        (other) =>
                            JSON.stringify(item) === JSON.stringify(other),
                    ),
            ),
        });
        if (!sameSnapshot(before, after))
            throw new Error(
                "Source changed during the drill; repeat with owned writers quiescent",
            );
        if (!sameSnapshot(before, restored) || !constraintsEqual)
            throw new Error(
                "Restored row contents or constraints differ from the source",
            );
        const seconds = (performance.now() - started) / 1000;
        if (seconds > maxSeconds)
            throw new Error(
                "Database restore exceeded the declared local drill budget",
            );
        Object.assign(report, {
            status: "passed",
            tables: before.length,
            rows: before.reduce((sum, table) => sum + table.rows, 0),
            rowContentsEqual: true,
            constraintsEqual: true,
            observedFixtureRecordLoss: 0,
            databaseRestoreSeconds: seconds,
            dumpBytes: dump.length,
            dumpSha256,
        });
    } catch (error) {
        Object.assign(report, { status: "failed", error: error.message });
        throw error;
    } finally {
        try {
            if (created)
                docker(["dropdb", "-U", "postgres", "--if-exists", target]);
            report.targetRemoved = true;
        } catch {
            report.targetRemoved = false;
            report.status = "failed";
        }
        try {
            unlinkSync(dumpPath);
            report.privateDumpRemoved = true;
        } catch {
            report.privateDumpRemoved = false;
        }
        writeFileSync(reportPath, `${JSON.stringify(report, null, 2)}\n`);
    }
    if (!report.targetRemoved || report.status !== "passed")
        throw new Error(
            "Recovery cleanup incomplete; inspect the owned report",
        );
    return report;
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
) {
    const [name] = process.argv.slice(2);
    console.log(JSON.stringify(recoveryDrill(name)));
}
