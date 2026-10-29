namespace Hadal.Game.Tests
{
    /// <summary>
    /// The PostgreSQL the store tests use (HADAL_TEST_PG). Without it those tests return early, as on a dev machine.
    /// server/tests/ct-tests.sh points it at a throwaway database inside hadal-postgres and sets HADAL_TEST_PG_REQUIRED=1,
    /// so on the CT a missing connection string fails the run instead of skipping every store test unnoticed.
    /// Never point HADAL_TEST_PG at the live "hadal" database: the store tests write and delete rows.
    /// </summary>
    internal static class PostgresTest
    {
        public static string? ConnectionString
        {
            get
            {
                var value = Environment.GetEnvironmentVariable("HADAL_TEST_PG");
                if (string.IsNullOrEmpty(value) && Environment.GetEnvironmentVariable("HADAL_TEST_PG_REQUIRED") == "1")
                    throw new InvalidOperationException("HADAL_TEST_PG_REQUIRED=1 but HADAL_TEST_PG is not set.");
                if (!string.IsNullOrEmpty(value) && IsLiveDatabase(value))
                    throw new InvalidOperationException("HADAL_TEST_PG points at the live database; use a throwaway one (server/tests/ct-tests.sh).");
                return value;
            }
        }

        /// <summary>True for a connection string whose database is the live one ("hadal").</summary>
        internal static bool IsLiveDatabase(string connectionString)
        {
            foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                if (pair.Length == 2 && pair[0].Trim().Replace(" ", string.Empty).ToLowerInvariant() is "database" or "db" or "initialcatalog")
                    return string.Equals(pair[1].Trim(), "hadal", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
