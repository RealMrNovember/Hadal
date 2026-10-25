using Npgsql;

namespace Hadal.Gateway.Admin
{
    /// <summary>
    /// Operator commands, run inside the gateway container so they use its database settings:
    ///
    ///   docker exec -it hadal-gateway dotnet Hadal.Gateway.dll admin create &lt;username&gt; [owner|gm|viewer]
    ///   docker exec -it hadal-gateway dotnet Hadal.Gateway.dll admin list
    ///   docker exec -it hadal-gateway dotnet Hadal.Gateway.dll admin disable &lt;username&gt;
    ///
    /// "create" prints the password and TOTP secret once; nothing else stores them in clear text.
    /// </summary>
    public static class AdminCli
    {
        public static async Task<int> RunAsync(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Hadal");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.Error.WriteLine("ConnectionStrings__Hadal is not set (run this inside the hadal-gateway container).");
                return 2;
            }

            await using var dataSource = NpgsqlDataSource.Create(connectionString);
            var store = new PostgresAdminStore(dataSource);
            await store.InitializeAsync(CancellationToken.None);

            switch (args.FirstOrDefault())
            {
                case "create" when args.Length >= 2:
                {
                    var role = args.Length >= 3 ? args[2] : AdminRoles.Owner;
                    if (!AdminRoles.IsValid(role))
                    {
                        Console.Error.WriteLine("Role must be owner, gm or viewer.");
                        return 2;
                    }

                    var (admin, password) = AdminAuth.NewAdmin(args[1], role);
                    if (!await store.CreateAdminAsync(admin, CancellationToken.None))
                    {
                        Console.Error.WriteLine($"Admin '{args[1]}' already exists.");
                        return 1;
                    }

                    await store.AuditAsync("cli", "admin.create", admin.Username, role, "console", CancellationToken.None);
                    Console.WriteLine($"Overseer admin created: {admin.Username} ({role})");
                    Console.WriteLine($"  Password    : {password}");
                    Console.WriteLine($"  TOTP secret : {admin.TotpSecret}");
                    Console.WriteLine($"  TOTP URI    : {Totp.ProvisioningUri(admin.Username, admin.TotpSecret)}");
                    Console.WriteLine("Add the secret to Google Authenticator / Authy (\"enter a setup key\"), then sign in at https://hadal.cicibyte.com/admin");
                    return 0;
                }

                case "list":
                    foreach (var admin in await store.ListAdminsAsync(CancellationToken.None))
                        Console.WriteLine($"{admin.Username,-24} {admin.Role,-7} {(admin.Disabled ? "disabled" : "active"),-8} last login {admin.LastLoginAt?.ToString("u") ?? "-"}");
                    return 0;

                case "disable" when args.Length >= 2:
                case "enable" when args.Length >= 2:
                {
                    var admin = await store.FindAdminAsync(args[1], CancellationToken.None);
                    if (admin == null)
                    {
                        Console.Error.WriteLine($"No admin '{args[1]}'.");
                        return 1;
                    }

                    await store.SetAdminDisabledAsync(admin.Id, args[0] == "disable", CancellationToken.None);
                    await store.AuditAsync("cli", "admin." + args[0], admin.Username, string.Empty, "console", CancellationToken.None);
                    Console.WriteLine($"{admin.Username}: {args[0]}d");
                    return 0;
                }

                default:
                    Console.Error.WriteLine("usage: admin create <username> [owner|gm|viewer] | admin list | admin disable|enable <username>");
                    return 2;
            }
        }
    }
}
