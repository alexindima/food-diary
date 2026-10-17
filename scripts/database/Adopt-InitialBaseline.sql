-- One-time, explicit adoption of the primary FoodDiary migration baseline.
-- First apply ALL 185 old migrations through 20261017003851_HardenTelegramAssertionRecovery
-- using the pre-squash Initializer, stop writers, and verify a restorable backup.
-- Run with psql -X -v ON_ERROR_STOP=1 -f scripts/database/Adopt-InitialBaseline.sql.
-- This single DO statement is atomic and changes only __EFMigrationsHistory.
DO $baseline$
DECLARE
    actual_schema text;
    history_count integer;
    history_digest text;
BEGIN
    PERFORM set_config('search_path', 'public, pg_catalog', true);
    PERFORM set_config('lock_timeout', '10s', true);
    IF to_regclass('public."__EFMigrationsHistory"') IS NULL THEN
        RAISE EXCEPTION 'No EF migration history. Use the new Initializer update command for an empty database.';
    END IF;
    LOCK TABLE public."__EFMigrationsHistory" IN ACCESS EXCLUSIVE MODE;

    WITH objects AS (
        SELECT 'table' AS kind, c.relname::text AS name,
            jsonb_build_array(c.relkind, c.relpersistence, c.relrowsecurity, c.relforcerowsecurity)::text AS definition
        FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'public' AND c.relkind IN ('r', 'p') AND c.relname <> '__EFMigrationsHistory'
        UNION ALL
        SELECT 'column' AS kind, c.relname || '.' || a.attname AS name,
            jsonb_build_array(format_type(a.atttypid, a.atttypmod), a.attnotnull,
                pg_get_expr(d.adbin, d.adrelid), a.attidentity, a.attgenerated, a.attcollation::regcollation::text)::text AS definition
        FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid
        LEFT JOIN pg_attrdef d ON d.adrelid = c.oid AND d.adnum = a.attnum
        WHERE n.nspname = 'public' AND c.relkind IN ('r', 'p')
            AND c.relname <> '__EFMigrationsHistory' AND a.attnum > 0 AND NOT a.attisdropped
        UNION ALL
        SELECT 'constraint', c.relname || '.' || k.conname, pg_get_constraintdef(k.oid, true)
        FROM pg_constraint k JOIN pg_class c ON c.oid = k.conrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        -- PostgreSQL 18 also stores NOT NULL constraints here. Their names depend
        -- on table-rename history; nullability is already verified via attnotnull.
        WHERE n.nspname = 'public' AND c.relname <> '__EFMigrationsHistory' AND k.contype <> 'n'
        UNION ALL
        SELECT 'index', c.relname, pg_get_indexdef(c.oid) || ':' || i.indisvalid::text || ':' || i.indisready::text
        FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_index i ON i.indexrelid = c.oid JOIN pg_class t ON t.oid = i.indrelid
        WHERE n.nspname = 'public' AND t.relname <> '__EFMigrationsHistory'
        UNION ALL
        SELECT 'sequence', c.relname,
            jsonb_build_array(format_type(s.seqtypid, NULL), s.seqstart, s.seqincrement,
                s.seqmin, s.seqmax, s.seqcache, s.seqcycle)::text
        FROM pg_sequence s JOIN pg_class c ON c.oid = s.seqrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public'
        UNION ALL
        SELECT 'trigger', c.relname || '.' || t.tgname, pg_get_triggerdef(t.oid) || ':' || t.tgenabled::text
        FROM pg_trigger t JOIN pg_class c ON c.oid = t.tgrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND NOT t.tgisinternal
        UNION ALL
        SELECT 'view', c.relname, pg_get_viewdef(c.oid)
        FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'public' AND c.relkind IN ('v', 'm')
        UNION ALL
        SELECT 'policy', c.relname || '.' || p.polname,
            jsonb_build_array(p.polcmd, p.polpermissive, p.polroles::text,
                pg_get_expr(p.polqual, p.polrelid), pg_get_expr(p.polwithcheck, p.polrelid))::text
        FROM pg_policy p JOIN pg_class c ON c.oid = p.polrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public'
        UNION ALL
        SELECT 'extension', extname, '' FROM pg_extension WHERE extname <> 'plpgsql'
    )
    SELECT md5(string_agg(kind || ':' || name || ':' || definition, E'\n' ORDER BY kind COLLATE "C", name COLLATE "C")) INTO actual_schema FROM objects;
    -- pg_dump reparses two varchar-array expressions into an equivalent catalog form.
    -- Both fingerprints are independently verified against the original chain.
    IF actual_schema IS NULL OR actual_schema NOT IN ('ceba2ada2aca706b41d8c33782ea59db', '524aebadd95c9df5d144b2416b4880c7') THEN
        RAISE EXCEPTION 'Schema differs from the verified pre-baseline schema (fingerprint %). No history was changed.', actual_schema;
    END IF;

    SELECT count(*), md5(string_agg("MigrationId", E'\n' ORDER BY "MigrationId" COLLATE "C"))
    INTO history_count, history_digest FROM public."__EFMigrationsHistory";
    IF history_count = 1 AND EXISTS (
        SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20261017130323_InitialBaseline'
    ) THEN
        RAISE NOTICE 'InitialBaseline is already adopted.';
        RETURN;
    END IF;
    -- The digest covers the exact ordered IDs, not merely their count or last entry.
    -- Some long-lived databases retain this data-only seed migration, removed
    -- before the chain was frozen: 20260208133129_SeedEmailTemplates.
    -- Accept its exact history variant, never arbitrary extra migration IDs.
    IF NOT (
        (history_count = 185 AND history_digest = 'd67d3225dace03470525fa2fbd0a2a1c') OR
        (history_count = 186 AND history_digest = '6def9bd6a6535900f4b0ae9e7b21c4c5')
    ) THEN
        RAISE EXCEPTION 'Incomplete or unknown legacy migration history. Finish the old migration chain before adopting the baseline. No history was changed.';
    END IF;

    DELETE FROM public."__EFMigrationsHistory";
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261017130323_InitialBaseline', '10.0.11');
END $baseline$;
