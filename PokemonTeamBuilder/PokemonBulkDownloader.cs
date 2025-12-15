using System.Text.Json;

namespace PokemonTeamBuilder
{
    public class PokemonBulkDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly PokemonService _pokemonService;
        private string _cacheFolder;
        private string _spritesFolder;
        private string _downloadCompleteFile;

        public PokemonBulkDownloader(HttpClient httpClient, PokemonService pokemonService)
        {
            _httpClient = httpClient;
            _pokemonService = pokemonService;
            InitializeFolders();
        }

        private void InitializeFolders()
        {
            _cacheFolder = Path.Combine(FileSystem.AppDataDirectory, "pokemon_cache");
            _spritesFolder = Path.Combine(_cacheFolder, "sprites");
            _downloadCompleteFile = Path.Combine(_cacheFolder, "download_complete.txt");

            if (!Directory.Exists(_cacheFolder))
            {
                Directory.CreateDirectory(_cacheFolder);
            }

            if (!Directory.Exists(_spritesFolder))
            {
                Directory.CreateDirectory(_spritesFolder);
            }
        }

        public bool IsDownloadComplete()
        {
            return File.Exists(_downloadCompleteFile);
        }

        public async Task<bool> DownloadAllPokemon(IProgress<DownloadProgress> progress = null)
        {
            try
            {
                int totalPokemon = PokemonService.PokemonLimit;
                int downloaded = 0;
                int failed = 0;

                for (int id = 1; id <= totalPokemon; id++)
                {
                    try
                    {
                        // Report progress
                        progress?.Report(new DownloadProgress
                        {
                            Current = downloaded,
                            Total = totalPokemon,
                            CurrentPokemonId = id,
                            Failed = failed
                        });

                        // Check if already cached
                        string pokemonName = id.ToString();
                        if (IsPokemonCached(id))
                        {
                            downloaded++;
                            continue;
                        }

                        // Download Pokemon data
                        var pokemon = await _pokemonService.GetPokemon(pokemonName);

                        if (pokemon != null)
                        {
                            // Save Pokemon data
                            await SavePokemonData(pokemon);

                            // Download and save sprite
                            await DownloadSprite(pokemon);

                            downloaded++;
                        }
                        else
                        {
                            failed++;
                        }

                        // Small delay to avoid overwhelming the API
                        await Task.Delay(50);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to download Pokemon #{id}: {ex.Message}");
                        failed++;
                    }
                }

                // Mark download as complete
                await File.WriteAllTextAsync(_downloadCompleteFile, DateTime.Now.ToString());

                progress?.Report(new DownloadProgress
                {
                    Current = downloaded,
                    Total = totalPokemon,
                    IsComplete = true,
                    Failed = failed
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Bulk download failed: {ex.Message}");
                return false;
            }
        }

        private bool IsPokemonCached(int id)
        {
            var dataFile = Path.Combine(_cacheFolder, $"{id}.json");
            var spriteFile = Path.Combine(_spritesFolder, $"{id}.png");
            return File.Exists(dataFile) && File.Exists(spriteFile);
        }

        private async Task SavePokemonData(Pokemon pokemon)
        {
            try
            {
                var json = JsonSerializer.Serialize(pokemon, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var filePath = Path.Combine(_cacheFolder, $"{pokemon.Id}.json");
                await File.WriteAllTextAsync(filePath, json);

                // Also save by name for easy lookup
                var nameFilePath = Path.Combine(_cacheFolder, $"{pokemon.Name.ToLower()}.json");
                await File.WriteAllTextAsync(nameFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save Pokemon data: {ex.Message}");
            }
        }

        private async Task DownloadSprite(Pokemon pokemon)
        {
            try
            {
                string spriteUrl = pokemon.Sprites?.FrontDefault;

                if (!string.IsNullOrEmpty(spriteUrl))
                {
                    var imageBytes = await _httpClient.GetByteArrayAsync(spriteUrl);

                    var spritePath = Path.Combine(_spritesFolder, $"{pokemon.Id}.png");
                    await File.WriteAllBytesAsync(spritePath, imageBytes);

                    // Also save by name
                    var namePath = Path.Combine(_spritesFolder, $"{pokemon.Name.ToLower()}.png");
                    await File.WriteAllBytesAsync(namePath, imageBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download sprite: {ex.Message}");
            }
        }

        public void ResetDownload()
        {
            if (File.Exists(_downloadCompleteFile))
            {
                File.Delete(_downloadCompleteFile);
            }
        }
    }

    public class DownloadProgress
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public int CurrentPokemonId { get; set; }
        public int Failed { get; set; }
        public bool IsComplete { get; set; }
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
    }
}