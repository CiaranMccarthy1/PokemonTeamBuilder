using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PokemonTeamBuilder
{
    public class PokemonService
    {
        public const string BaseUrl = "https://pokeapi.co/api/v2/";
        public const string SpriteBaseUrl = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/";
        public const int PokemonLimit = 1025;

        private readonly HttpClient httpClient;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PokemonService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<Pokemon?> GetPokemon(string name)
        {
            string url = $"{BaseUrl}pokemon/{name}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Pokémon not found");

            var json = await response.Content.ReadAsStringAsync();
            var pokemon = JsonSerializer.Deserialize<Pokemon>(json, JsonOptions);

            if (pokemon?.Types != null)
            {
                var typeNames = pokemon.Types.Select(t => t.Type.Name).ToList();
                pokemon.Strengths = PokemonTypeEffectiveness.GetStrengths(typeNames);
                pokemon.Weaknesses = PokemonTypeEffectiveness.GetWeaknesses(typeNames);
            }

            await FetchSpeciesData(pokemon);

            return pokemon;
        }

        private async Task FetchSpeciesData(Pokemon? pokemon)
        {
            if (pokemon == null) return;

            try
            {
                string speciesUrl = $"{BaseUrl}pokemon-species/{pokemon.Id}";
                var response = await httpClient.GetAsync(speciesUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var speciesData = JsonSerializer.Deserialize<PokemonSpeciesResponse>(json, JsonOptions);

                    if (speciesData != null)
                    {
                        pokemon.IsLegendary = speciesData.IsLegendary;
                        pokemon.IsMythical = speciesData.IsMythical;
                        
                      
                        if (speciesData.Generation?.Url != null)
                        {
                            var genUrl = speciesData.Generation.Url.TrimEnd('/');
                            var genNumber = genUrl.Split('/').LastOrDefault();
                            if (int.TryParse(genNumber, out int gen))
                            {
                                pokemon.Generation = gen;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch species data for {pokemon.Name}: {ex.Message}");
            }
        }

        public async Task<List<PokemonListItem>> GetAllPokemonNames(int limit = 50, int offset = 0)
        {
            string url = $"{BaseUrl}pokemon?limit={limit}&offset={offset}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<PokemonListItem>();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<PokemonListResponse>(json, JsonOptions);

            return data?.Results ?? new List<PokemonListItem>();
        }
    }
}

public class PokemonSpeciesResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("is_legendary")]
    public bool IsLegendary { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("is_mythical")]
    public bool IsMythical { get; set; }

    public GenerationReference? Generation { get; set; }
}

public class GenerationReference
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class PokemonListResponse
{
    public List<PokemonListItem> Results { get; set; } = new();
}

public class PokemonListItem
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}



