using System.Diagnostics;
using System.Text.Json;

namespace PokemonTeamBuilder
{
    public class PokemonBulkDownloader
    {
        private readonly HttpClient httpClient;
        private readonly PokemonService pokemonService;
        private string cacheFolder;
        private string spritesFolder;
        private string downloadCompleteFile;

        public PokemonBulkDownloader(HttpClient httpClient, PokemonService pokemonService)
        {
            this.httpClient = httpClient;
            this.pokemonService = pokemonService;
            InitializeFolders();
        }

        private void InitializeFolders()
        {
            cacheFolder = Path.Combine(FileSystem.AppDataDirectory, "pokemon_cache");
            spritesFolder = Path.Combine(cacheFolder, "sprites");
            downloadCompleteFile = Path.Combine(cacheFolder, "download_complete.txt");

            if (!Directory.Exists(cacheFolder))
            {
                Debug.Log($"Creating cache folder at {cacheFolder}");
                Directory.CreateDirectory(cacheFolder);
            }

            if (!Directory.Exists(spritesFolder))
            {
                Debug.Log($"Creating sprites folder at {spritesFolder}");
                Directory.CreateDirectory(spritesFolder);
            }
        }

        public bool IsDownloadComplete()
        {
            Debug.Log($"Checking download completion: {downloadCompleteFile}");
            return File.Exists(downloadCompleteFile);
        }

        public async Task<bool> DownloadAllPokemon(IProgress<DownloadProgress> progress = null)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                int totalPokemon = PokemonService.PokemonLimit;
                int downloaded = 0;
                int failed = 0;

                for (int id = 1; id <= totalPokemon; id++)
                {
                    try
                    {

                        progress?.Report(new DownloadProgress
                        {
                            Current = downloaded,
                            Total = totalPokemon,
                            CurrentPokemonId = id,
                            Failed = failed
                        });


                        string pokemonName = id.ToString();
                        if (IsPokemonCached(id))
                        {
                            downloaded++;
                            continue;
                        }


                        var pokemon = await pokemonService.GetPokemon(pokemonName);

                        if (pokemon != null)
                        {

                            await SavePokemonData(pokemon);


                            await DownloadSprite(pokemon);

                            downloaded++;
                        }
                        else
                        {
                            failed++;
                        }

                        await Task.Delay(10);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, $"Failed to download Pokemon #{id}");
                        failed++;
                    }
                }
                timer.Stop();
                

                await File.WriteAllTextAsync(downloadCompleteFile, DateTime.Now.ToString());

                progress?.Report(new DownloadProgress
                {
                    Current = downloaded,
                    Total = totalPokemon,
                    IsComplete = true,
                    Failed = failed,
                    ElapsedTime = timer.Elapsed
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, $"Bulk download failed");
                return false;
            }
        }

        private bool IsPokemonCached(int id)
        {
            var dataFile = Path.Combine(cacheFolder, $"{id}.json");
            var spriteFile = Path.Combine(spritesFolder, $"{id}.png");
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

                var filePath = Path.Combine(cacheFolder, $"{pokemon.Id}.json");
                await File.WriteAllTextAsync(filePath, json);

                var nameFilePath = Path.Combine(cacheFolder, $"{pokemon.Name.ToLower()}.json");
                Debug.Log($"Saving Pokemon data for {pokemon.Name}");
                await File.WriteAllTextAsync(nameFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, "Failed to save Pokemon data");
            }
        }

        private async Task DownloadSprite(Pokemon pokemon)
        {
            try
            {
                // Download normal sprite
                string spriteUrl = pokemon.Sprites?.FrontDefault;

                if (!string.IsNullOrEmpty(spriteUrl))
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(spriteUrl);

                    var spritePath = Path.Combine(spritesFolder, $"{pokemon.Id}.png");
                    await File.WriteAllBytesAsync(spritePath, imageBytes);

                    var namePath = Path.Combine(spritesFolder, $"{pokemon.Name.ToLower()}.png");
                    await File.WriteAllBytesAsync(namePath, imageBytes);
                }
                
                // Download shiny sprite
                string shinySpriteUrl = pokemon.Sprites?.FrontShiny;
                
                if (!string.IsNullOrEmpty(shinySpriteUrl))
                {
                    var shinyImageBytes = await httpClient.GetByteArrayAsync(shinySpriteUrl);
                    
                    var shinySpritePath = Path.Combine(spritesFolder, $"{pokemon.Id}_shiny.png");
                    await File.WriteAllBytesAsync(shinySpritePath, shinyImageBytes);
                    
                    var shinyNamePath = Path.Combine(spritesFolder, $"{pokemon.Name.ToLower()}_shiny.png");
                    await File.WriteAllBytesAsync(shinyNamePath, shinyImageBytes);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, $"Failed to download sprite");
            }
        }

        public void ResetDownload()
        {
            try
            {
                if (File.Exists(downloadCompleteFile))
                {
                    File.Delete(downloadCompleteFile);
                }

                if (Directory.Exists(cacheFolder))
                {
                    foreach (var file in Directory.GetFiles(cacheFolder, "*.json"))
                    {
                        File.Delete(file);
                    }
                }

                if (Directory.Exists(spritesFolder))
                {
                    foreach (var file in Directory.GetFiles(spritesFolder, "*.png"))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, "Failed to reset download");
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
        public TimeSpan ElapsedTime { get; set; }
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
    }
}