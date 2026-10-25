using System;
using System.Threading.Tasks;
using HADAL.Shared.Auth;
using Hadal.Core.Catalog;
using Hadal.Data.Config;
using UnityEngine;

namespace Hadal.Core.Network
{
    public enum PatchVerdict
    {
        Ok,
        UpdateRequired,
        Maintenance
    }

    public interface IPatchService
    {
        /// <summary>Reads /manifest/version.json and decides whether this build may connect.</summary>
        Task<(PatchVerdict Verdict, VersionManifest Manifest)> CheckAsync();

        /// <summary>Downloads the live catalog when the manifest names another version than the one in use.</summary>
        Task SyncCatalogAsync(VersionManifest manifest);
    }

    public sealed class PatchService : IPatchService
    {
        private readonly GameConfigSO _config;

        public PatchService(GameConfigSO config)
        {
            _config = config;
        }

        public async Task<(PatchVerdict, VersionManifest)> CheckAsync()
        {
            var (status, manifest) = await HttpJson.GetAsync<VersionManifest>(_config.ApiBaseUrl + AuthRoutes.Manifest);
            if (manifest == null)
                throw new ServerUnreachableException($"Version manifest unavailable (HTTP {status}).");

            if (IsOlder(Application.version, manifest.minClientVersion))
                return (PatchVerdict.UpdateRequired, manifest);

            return (manifest.maintenance ? PatchVerdict.Maintenance : PatchVerdict.Ok, manifest);
        }

        public async Task SyncCatalogAsync(VersionManifest manifest)
        {
            if (manifest == null || manifest.catalogVersion <= 0 || manifest.catalogVersion == ClientCatalog.Version)
                return;

            var (status, json, header) = await HttpJson.GetTextAsync(_config.ApiBaseUrl + AuthRoutes.Catalog, "X-Catalog-Version");
            if (json == null || !int.TryParse(header, out var version))
            {
                Debug.LogWarning($"[PatchService] Live catalog unavailable (HTTP {status}); keeping v{ClientCatalog.Version}.");
                return;
            }

            if (ClientCatalog.Apply(json, version))
                Debug.Log($"[PatchService] Live catalog v{version} applied.");
        }

        /// <summary>Numeric dotted comparison; unparsable versions never block a client.</summary>
        public static bool IsOlder(string current, string minimum)
        {
            return Version.TryParse(Normalize(current), out var have)
                   && Version.TryParse(Normalize(minimum), out var need)
                   && have < need;
        }

        // System.Version treats a missing component as -1, so "1.0" < "1.0.0". Pad to three parts.
        private static string Normalize(string version)
        {
            var parts = (version ?? string.Empty).Trim().Split('.');
            var padded = new string[3];
            for (var i = 0; i < 3; i++)
                padded[i] = i < parts.Length && parts[i].Length > 0 ? parts[i] : "0";

            return parts.Length > 3 ? version.Trim() : string.Join(".", padded);
        }
    }
}
