using System.IO;
using System.Text.Json;

namespace PokemonTeamBuilder
{
    public class PokemonCache
    {

        private static string cacheFolder;
        private static string favoritesFile;
        private static string spritesFolder;
        

        private static void InitializeFolders()
        {
            if (string.IsNullOrEmpty(cacheFolder))
            {
                cacheFolder = System.IO.Path.Combine(FileSystem.AppDataDirectory, "pokemon_cache");
                spritesFolder = System.IO.Path.Combine(cacheFolder, "sprites");
                favoritesFile = System.IO.Path.Combine(cacheFolder, "favorites.json");

                if (!Directory.Exists(cacheFolder))
                {
                    Directory.CreateDirectory(cacheFolder);
                }

                if (!Directory.Exists(spritesFolder))
                {
                    Directory.CreateDirectory(spritesFolder);
                }
            }
        }

        public static async Task<List<string>> GetFavorites()
        {
            InitializeFolders();
            try
            {
                if (File.Exists(favoritesFile))
                {
                    var json = await File.ReadAllTextAsync(favoritesFile);
                    return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, "Failed to get favorites");
            }

            return new List<string>();
        }
        public static bool IsCached(string name)
        {
            InitializeFolders();   
            var filePath = System.IO.Path.Combine(cacheFolder, $"{name.ToLower()}.json");
            return File.Exists(filePath);
        }

        public static async Task<Pokemon?> GetCachedPokemon(string name)
        {
            InitializeFolders();
            try
            {
                var filePath = System.IO.Path.Combine(cacheFolder, $"{name.ToLower()}.json");

                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    return JsonSerializer.Deserialize<Pokemon>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, "Failed to get cached Pokemon");
            }

            return null;
        }

        public static async Task SaveFavorites(List<string> favorites)
        {
            InitializeFolders();
            try
            {
                var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(favoritesFile, json);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, "Failed to save favorites");
            }
        }

        public static string GetCachedSprite(string name)
        {
            InitializeFolders();
            var spritePath = Path.Combine(spritesFolder, $"{name.ToLower()}.png");
            if (File.Exists(spritePath))
            {
                return spritePath;
            }
            return null;
        }

        public static async Task<bool> IsFavourite(string pokemonName)
        {
            var favourites = await GetFavorites();
            return favourites.Contains(pokemonName);
        }

        public static async Task<Pokemon?> GetCachedPokemonById(int id)
        {
            InitializeFolders();
            try
            {
                var filePath = Path.Combine(cacheFolder, $"{id}.json");

                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    return JsonSerializer.Deserialize<Pokemon>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, "Failed to get cached Pokemon by ID");
            }

            return null;
        }

        public static string GetCachedSpriteById(int id)
        {
            InitializeFolders();
            var spritePath = Path.Combine(spritesFolder, $"{id}.png");
            if (File.Exists(spritePath))
            { 
                return spritePath;
            }
            return null;
        }
    }
}
