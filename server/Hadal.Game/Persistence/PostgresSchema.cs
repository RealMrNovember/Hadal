using Npgsql;

namespace Hadal.Game.Persistence
{
    /// <summary>
    /// Applies idempotent DDL under one cluster-wide advisory lock. The player repository and the
    /// account store initialise concurrently at startup, and concurrent
    /// <c>CREATE SCHEMA/TABLE IF NOT EXISTS</c> on a fresh database fails with a unique violation
    /// on the system catalogs; the lock makes them run one after the other.
    /// </summary>
    public static class PostgresSchema
    {
        private const long LockKey = 0x4841_4441_4C00; // "HADAL"

        public static async Task ApplyAsync(NpgsqlDataSource dataSource, string ddl, CancellationToken cancellationToken)
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await using (var gate = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@k)", connection, transaction))
            {
                gate.Parameters.AddWithValue("k", LockKey);
                await gate.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var command = new NpgsqlCommand(ddl, connection, transaction))
                await command.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
    }
}
