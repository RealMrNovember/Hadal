using System.Text.Json;
using HADAL.Shared.Catalog;

namespace Hadal.Game
{
    public static class CatalogLoader
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            IncludeFields = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static GameCatalog Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Game catalog not found.", path);

            var data = JsonSerializer.Deserialize<GameCatalogData>(File.ReadAllText(path), Options)
                       ?? throw new FormatException($"Game catalog '{path}' is empty.");

            return GameCatalog.Create(data);
        }
    }
}
