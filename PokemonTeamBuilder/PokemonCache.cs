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
                Console.WriteLine($"Failed to get favorites: {ex.Message}");
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
                Console.WriteLine($"Failed to get cached Pokemon: {ex.Message}");
            }

            return null;
        }

        public static async Task CachePokemon(Pokemon pokemon)
        {

            InitializeFolders();
            try
            {
            
                var json = JsonSerializer.Serialize(pokemon, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var filePath = System.IO.Path.Combine(cacheFolder, $"{pokemon.Name.ToLower()}.json");
                await File.WriteAllTextAsync(filePath, json);

             
                await CacheSprite(pokemon);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cache Pokemon: {ex.Message}");
            }
        }

        public static async Task CacheSprite(Pokemon pokemon)
        {
            InitializeFolders();
            try
            {
                string spriteUrl = pokemon.Sprites?.Versions?.GenerationI?.RedBlue?.FrontDefault;

                if (!string.IsNullOrEmpty(spriteUrl))
                {
                    using var httpClient = new HttpClient();
                    var imageBytes = await httpClient.GetByteArrayAsync(spriteUrl);

                    var spritePath = System.IO.Path.Combine(spritesFolder, $"{pokemon.Name.ToLower()}.png");
                    await File.WriteAllBytesAsync(spritePath, imageBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cache sprite: {ex.Message}");
            }
        }

        public static void RemoveFromCache(string name)
        {
            InitializeFolders();
            try
            {
                var filePath = System.IO.Path.Combine(cacheFolder, $"{name.ToLower()}.json");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var spritePath = System.IO.Path.Combine(spritesFolder, $"{name.ToLower()}.png");
                if (File.Exists(spritePath))
                {
                    File.Delete(spritePath);
                }
            }
            catch(Exception ex) 
            { 
                Console.WriteLine($"Failed to delete file: {ex.Message}");
            }
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
                Console.WriteLine($"Failed to save favorites: {ex.Message}");
            }
        }

        public static string GetCachedSprite(string name)
        {
            var spritePath = System.IO.Path.Combine(spritesFolder, $"{name.ToLower()}.png");
            if (File.Exists(spritePath))
            {
                return spritePath;
            }
            return null;
        }

        public static async Task<bool> IsFavouriteAsync(string pokemonName)
        {
            var favourites = await GetFavorites();
            return favourites.Contains(pokemonName);
        }
    }
}
