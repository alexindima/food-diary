using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class NpgsqlMailInboxRuntimeRoleValidator(NpgsqlDataSource dataSource) {
    public async Task ValidateAsync(
        string expectedRoleName,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedRoleName);
        if (!NpgsqlMailInboxRuntimeRoleProvisioner.HasValidRoleName(expectedRoleName)) {
            throw new InvalidOperationException(
                "MailInbox runtime database role name must identify a dedicated mailinbox_* role.");
        }

        const string sql = """
                           select current_user = @expected_role_name
                              and exists (
                                  select 1
                                  from pg_roles as role
                                  where role.rolname = current_user
                                    and role.rolcanlogin
                                    and not role.rolinherit
                                    and not role.rolsuper
                                    and not role.rolcreatedb
                                    and not role.rolcreaterole
                                    and not role.rolreplication
                                    and not role.rolbypassrls
                                    and role.rolconfig is null
                                    and shobj_description(role.oid, 'pg_authid') = @role_marker
                              )
                              and not exists (
                                  select 1
                                  from pg_auth_members as membership
                                  join pg_roles as member_role on member_role.oid = membership.member
                                  where member_role.rolname = current_user
                              )
                              and not exists (
                                  select 1
                                  from pg_shdepend as dependency
                                  join pg_roles as referenced_role on referenced_role.oid = dependency.refobjid
                                  where dependency.refclassid = 'pg_authid'::regclass
                                    and dependency.deptype = 'o'
                                    and referenced_role.rolname = current_user
                              )
                              and has_schema_privilege(current_user, 'public', 'USAGE')
                              and not has_schema_privilege(current_user, 'public', 'CREATE')
                              and has_table_privilege(current_user, 'public.mailinbox_messages', 'SELECT')
                              and has_table_privilege(current_user, 'public.mailinbox_messages', 'INSERT')
                              and has_table_privilege(current_user, 'public.mailinbox_messages', 'UPDATE')
                              and has_table_privilege(current_user, 'public.mailinbox_messages', 'DELETE')
                              and not has_table_privilege(current_user, 'public.mailinbox_messages', 'TRUNCATE')
                              and not has_table_privilege(current_user, 'public.mailinbox_messages', 'REFERENCES')
                              and not has_table_privilege(current_user, 'public.mailinbox_messages', 'TRIGGER')
                              and has_table_privilege(current_user, 'public.mailinbox_daily_ingestion_usage', 'SELECT')
                              and has_table_privilege(current_user, 'public.mailinbox_daily_ingestion_usage', 'INSERT')
                              and has_table_privilege(current_user, 'public.mailinbox_daily_ingestion_usage', 'UPDATE')
                              and has_table_privilege(current_user, 'public.mailinbox_daily_ingestion_usage', 'DELETE')
                              and not has_table_privilege(current_user, 'public.mailinbox_daily_ingestion_usage', 'TRUNCATE')
                              and not has_table_privilege(current_user, 'public.mailinbox_daily_ingestion_usage', 'REFERENCES')
                              and not has_table_privilege(current_user, 'public.mailinbox_daily_ingestion_usage', 'TRIGGER')
                              and has_table_privilege(current_user, 'public.mailinbox_schema_migrations', 'SELECT')
                              and not has_table_privilege(current_user, 'public.mailinbox_schema_migrations', 'INSERT')
                              and not has_table_privilege(current_user, 'public.mailinbox_schema_migrations', 'UPDATE')
                              and not has_table_privilege(current_user, 'public.mailinbox_schema_migrations', 'DELETE')
                              and not has_table_privilege(current_user, 'public.mailinbox_schema_migrations', 'TRUNCATE')
                              and not has_table_privilege(current_user, 'public.mailinbox_schema_migrations', 'REFERENCES')
                              and not has_table_privilege(current_user, 'public.mailinbox_schema_migrations', 'TRIGGER')
                              and not exists (
                                  select 1
                                  from pg_class as relation
                                  join pg_namespace as schema on schema.oid = relation.relnamespace
                                  where schema.nspname = 'public'
                                    and relation.relkind in ('r', 'p', 'v', 'm', 'f')
                                    and relation.relname not in (
                                        'mailinbox_messages',
                                        'mailinbox_daily_ingestion_usage',
                                        'mailinbox_schema_migrations'
                                    )
                                    and (
                                        has_table_privilege(current_user, relation.oid, 'SELECT')
                                        or has_table_privilege(current_user, relation.oid, 'INSERT')
                                        or has_table_privilege(current_user, relation.oid, 'UPDATE')
                                        or has_table_privilege(current_user, relation.oid, 'DELETE')
                                        or has_table_privilege(current_user, relation.oid, 'TRUNCATE')
                                        or has_table_privilege(current_user, relation.oid, 'REFERENCES')
                                        or has_table_privilege(current_user, relation.oid, 'TRIGGER')
                                    )
                              )
                              and not exists (
                                  select 1
                                  from pg_class as sequence
                                  join pg_namespace as schema on schema.oid = sequence.relnamespace
                                  where schema.nspname = 'public'
                                    and sequence.relkind = 'S'
                                    and (
                                        has_sequence_privilege(current_user, sequence.oid, 'USAGE')
                                        or has_sequence_privilege(current_user, sequence.oid, 'SELECT')
                                        or has_sequence_privilege(current_user, sequence.oid, 'UPDATE')
                                    )
                              );
                           """;
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("expected_role_name", expectedRoleName);
                command.Parameters.AddWithValue("role_marker", NpgsqlMailInboxRuntimeRoleProvisioner.RoleMarker);
                object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (result is not true) {
                    throw new InvalidOperationException(
                        "MailInbox runtime database role is not provisioner-owned or does not have the required least-privilege grants.");
                }
            }
        }
    }
}
