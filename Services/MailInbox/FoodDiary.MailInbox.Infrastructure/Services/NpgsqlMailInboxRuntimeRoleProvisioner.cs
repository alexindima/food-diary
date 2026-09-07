using System.Text.RegularExpressions;
using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed partial class NpgsqlMailInboxRuntimeRoleProvisioner(NpgsqlDataSource dataSource) {
    public const int MinPasswordLength = 32;
    public const int MaxPasswordLength = 256;
    internal const string RoleMarker = "fooddiary-mailinbox-runtime-role/v1";

    public static bool HasValidRoleName(string? roleName) =>
        !string.IsNullOrWhiteSpace(roleName) && RoleNamePattern().IsMatch(roleName);

    public static bool HasValidPassword(string? password) =>
        !string.IsNullOrWhiteSpace(password) &&
        password.Length is >= MinPasswordLength and <= MaxPasswordLength;

    public async Task ProvisionAsync(
        string roleName,
        string password,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (!HasValidRoleName(roleName)) {
            throw new ArgumentException("Runtime database role name is invalid.", nameof(roleName));
        }

        if (!HasValidPassword(password)) {
            throw new ArgumentException(
                $"Runtime database password must contain between {MinPasswordLength} and {MaxPasswordLength} characters.",
                nameof(password));
        }

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                bool roleExists = await RoleExistsAsync(connection, transaction, roleName, cancellationToken)
                    .ConfigureAwait(false);
                string quotedRole = new NpgsqlCommandBuilder().QuoteIdentifier(roleName);
                if (!roleExists) {
                    string createRoleSql = await FormatRoleSqlAsync(
                        connection,
                        transaction,
                        "create role %I login noinherit nosuperuser nocreatedb nocreaterole noreplication nobypassrls password %L",
                        roleName,
                        password,
                        cancellationToken).ConfigureAwait(false);
                    await ExecuteAsync(connection, transaction, createRoleSql, cancellationToken).ConfigureAwait(false);
                    string markRoleSql = await FormatRoleSqlAsync(
                        connection,
                        transaction,
                        "comment on role %I is %L",
                        roleName,
                        RoleMarker,
                        cancellationToken).ConfigureAwait(false);
                    await ExecuteAsync(connection, transaction, markRoleSql, cancellationToken).ConfigureAwait(false);
                } else {
                    await EnsureReusableRoleIsRestrictedAsync(
                        connection,
                        transaction,
                        roleName,
                        cancellationToken).ConfigureAwait(false);
                    string alterRoleSql = await FormatRoleSqlAsync(
                        connection,
                        transaction,
                        "alter role %I with login noinherit nosuperuser nocreatedb nocreaterole noreplication nobypassrls password %L",
                        roleName,
                        password,
                        cancellationToken).ConfigureAwait(false);
                    await ExecuteAsync(connection, transaction, alterRoleSql, cancellationToken).ConfigureAwait(false);
                }

                string quotedDatabase = new NpgsqlCommandBuilder().QuoteIdentifier(connection.Database);
                string privilegeSql = $"""
                                      drop owned by {quotedRole};
                                      alter role {quotedRole} reset all;
                                      revoke all privileges on database {quotedDatabase} from {quotedRole};
                                      grant connect on database {quotedDatabase} to {quotedRole};
                                      revoke create on schema public from public;
                                      revoke all privileges on schema public from {quotedRole};
                                      grant usage on schema public to {quotedRole};
                                      revoke all privileges on all tables in schema public from {quotedRole};
                                      grant select, insert, update, delete on table
                                          public.mailinbox_messages,
                                          public.mailinbox_daily_ingestion_usage
                                          to {quotedRole};
                                      grant select on table public.mailinbox_schema_migrations to {quotedRole};
                                      revoke all privileges on all sequences in schema public from {quotedRole};
                                      grant execute on function public.mailinbox_recipients_within_limits(jsonb) to {quotedRole};
                                      alter default privileges in schema public
                                          revoke all privileges on tables from {quotedRole};
                                      alter default privileges in schema public
                                          revoke all privileges on sequences from {quotedRole};
                                      """;
                await ExecuteAsync(connection, transaction, privilegeSql, cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task<bool> RoleExistsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string roleName,
        CancellationToken cancellationToken) {
        const string sql = "select exists (select 1 from pg_roles where rolname = @role_name);";
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("role_name", roleName);
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is true;
        }
    }

    private static async Task EnsureReusableRoleIsRestrictedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string roleName,
        CancellationToken cancellationToken) {
        const string sql = """
                           select exists (
                               select 1
                               from pg_roles as role
                               where role.rolname = @role_name
                                 and (
                                     shobj_description(role.oid, 'pg_authid') is distinct from @role_marker
                                     or role.rolconfig is not null
                                 )
                               union all
                               select 1
                               from pg_auth_members as membership
                               join pg_roles as member_role on member_role.oid = membership.member
                               where member_role.rolname = @role_name
                               union all
                               select 1
                               from pg_shdepend as dependency
                               join pg_roles as referenced_role on referenced_role.oid = dependency.refobjid
                               cross join lateral (
                                   select oid
                                   from pg_database
                                   where datname = current_database()
                               ) as current_database
                               where dependency.refclassid = 'pg_authid'::regclass
                                 and referenced_role.rolname = @role_name
                                 and (
                                     dependency.deptype = 'o'
                                     or (
                                         dependency.deptype = 'a'
                                         and dependency.dbid <> current_database.oid
                                         and not (
                                             dependency.dbid = 0
                                             and dependency.classid = 'pg_database'::regclass
                                             and dependency.objid = current_database.oid
                                         )
                                     )
                                 )
                           );
                           """;
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("role_name", roleName);
            command.Parameters.AddWithValue("role_marker", RoleMarker);
            if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is true) {
                throw new InvalidOperationException(
                    "Existing MailInbox runtime database role is not provisioner-owned or has unsafe settings, inherited privileges, cross-database grants, or owned objects.");
            }
        }
    }

    private static async Task<string> FormatRoleSqlAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string template,
        string roleName,
        string password,
        CancellationToken cancellationToken) {
        const string sql = "select format(@template, @role_name, @password);";
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("template", template);
            command.Parameters.AddWithValue("role_name", roleName);
            command.Parameters.AddWithValue("password", password);
            return (string)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                            ?? throw new InvalidOperationException("PostgreSQL did not format the role command."));
        }
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken) {
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    [GeneratedRegex("^mailinbox_[a-z0-9_]{0,53}$", RegexOptions.CultureInvariant, 100)]
    private static partial Regex RoleNamePattern();
}
