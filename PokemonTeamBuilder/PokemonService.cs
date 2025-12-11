using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PokemonTeamBuilder
{
    public class PokemonService
    {
        public const string BaseUrl = "https://pokeapi.co/api/v2/";
        public const string SpriteBaseUrl = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/";
        public const int PokemonLimit = 1025;

        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PokemonService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Pokemon> GetPokemon(string name)
        {
            string url = $"{BaseUrl}pokemon/{name}";
            var response = await _httpClient.GetAsync(url);

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

            return pokemon;
        }

        public async Task<List<string>> GetAllPokemonNames(int limit = 50, int offset = 0)
        {
            string url = $"{BaseUrl}pokemon?limit={limit}&offset={offset}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<PokemonListResponse>(json, JsonOptions);

            return data?.Results?.Select(p => p.Name).ToList() ?? new List<string>();
        }
    }

    public class PokemonListResponse
    {
        public List<PokemonListItem> Results { get; set; }
    }

    public class PokemonListItem
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}
