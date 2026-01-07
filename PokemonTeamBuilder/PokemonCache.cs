using System.IO;
using System.Text.Json;
using System.Collections.Concurrent;

namespace PokemonTeamBuilder
{
    public class PokemonCache
    {

        private static string cacheFolder;
        private static string favoritesFile;
        private static string spritesFolder;
        private static string indexFile;
        private static List<PokemonIndexEntry>? pokemonIndex;
        private static readonly object indexLock = new();

        private static void InitializeFolders()
        {
            if (string.IsNullOrEmpty(cacheFolder))
            {
                cacheFolder = System.IO.Path.Combine(FileSystem.AppDataDirectory, "pokemon_cache");
                spritesFolder = System.IO.Path.Combine(cacheFolder, "sprites");
                favoritesFile = System.IO.Path.Combine(cacheFolder, "favorites.json");
                indexFile = System.IO.Path.Combine(cacheFolder, "pokemon_index.json");

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

        public static async Task<List<PokemonIndexEntry>> GetPokemonIndex()
        {
            InitializeFolders();
            
            if (pokemonIndex != null)
                return pokemonIndex;

            lock (indexLock)
            {
                if (pokemonIndex != null)
                    return pokemonIndex;
            }

            if (File.Exists(indexFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(indexFile);
                    var index = JsonSerializer.Deserialize<List<PokemonIndexEntry>>(json);
                    if (index != null && index.Count > 0)
                    {
                        var deduplicatedIndex = index
                            .GroupBy(p => p.Id)
                            .Select(g => g.First())
                            .OrderBy(p => p.Id)
                            .ToList();
                        
                        lock (indexLock)
                        {
                            pokemonIndex = deduplicatedIndex;
                        }
                        return deduplicatedIndex;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, "Failed to load Pokemon index");
                }
            }

            var builtIndex = await BuildPokemonIndex();
            lock (indexLock)
            {
                pokemonIndex = builtIndex;
            }
            return builtIndex;
        }

        private static async Task<List<PokemonIndexEntry>> BuildPokemonIndex()
        {
            InitializeFolders();
            var index = new List<PokemonIndexEntry>();
            var seenIds = new HashSet<int>();

            var files = Directory.GetFiles(cacheFolder, "*.json")
                .Where(f => !f.EndsWith("favorites.json") && !f.EndsWith("pokemon_index.json"))
                .ToList();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var pokemon = JsonSerializer.Deserialize<Pokemon>(json, options);
                    if (pokemon != null && pokemon.Id > 0 && !seenIds.Contains(pokemon.Id))
                    {
                        seenIds.Add(pokemon.Id);
                        index.Add(new PokemonIndexEntry
                        {
                            Id = pokemon.Id,
                            Name = pokemon.Name,
                            Generation = pokemon.Generation,
                            IsLegendary = pokemon.IsLegendary,
                            IsMythical = pokemon.IsMythical,
                            TotalBaseStats = pokemon.TotalBaseStats,
                            Types = pokemon.Types?.Select(t => t.Type.Name).ToList() ?? new List<string>(),
                            Stats = pokemon.Stats?.ToDictionary(s => s.Stat?.Name ?? "", s => s.BaseStat) ?? new Dictionary<string, int>()
                        });
                    }
                }
                catch { }
            }

            index = index.OrderBy(p => p.Id).ToList();

            try
            {
                var json = JsonSerializer.Serialize(index);
                await File.WriteAllTextAsync(indexFile, json);
            }
            catch { }

            return index;
        }

        public static void InvalidateIndex()
        {
            lock (indexLock)
            {
                pokemonIndex = null;
            }
            
            InitializeFolders();
            if (File.Exists(indexFile))
            {
                try { File.Delete(indexFile); } catch { }
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
        
        public static string GetCachedShinySprite(string name)
        {
            InitializeFolders();
            var shinySpritePath = Path.Combine(spritesFolder, $"{name.ToLower()}_shiny.png");
            if (File.Exists(shinySpritePath))
            {
                return shinySpritePath;
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
        
        public static string GetCachedShinySpriteById(int id)
        {
            InitializeFolders();
            var shinySpritePath = Path.Combine(spritesFolder, $"{id}_shiny.png");
            if (File.Exists(shinySpritePath))
            {
                return shinySpritePath;
            }
            return null;
        }
    }
}
