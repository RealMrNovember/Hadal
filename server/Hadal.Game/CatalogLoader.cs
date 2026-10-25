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

            return Parse(File.ReadAllText(path));
        }

        /// <summary>Parses and validates catalog JSON; FormatException (with every problem listed) when invalid.</summary>
        public static GameCatalog Parse(string json)
        {
            GameCatalogData? data;
            try
            {
                data = JsonSerializer.Deserialize<GameCatalogData>(json, Options);
            }
            catch (JsonException exception)
            {
                throw new FormatException("Catalog is not valid JSON: " + exception.Message, exception);
            }

            return GameCatalog.Create(data ?? throw new FormatException("Catalog is empty."));
        }
    }
}
