using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static PokemonTeamBuilder.MainPage;

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

        public async Task<Pokemon?> GetPokemon(string name)
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

        public async Task<List<PokemonListItem>> GetAllPokemonNames(int limit = 50, int offset = 0)
        {
            string url = $"{BaseUrl}pokemon?limit={limit}&offset={offset}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<PokemonListItem>();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<PokemonListResponse>(json, JsonOptions);

            return data?.Results ?? new List<PokemonListItem>();
        }


        //TeamScore(0, 1000) = Offense(450) + Defense(300) + BaseStats(200) − WeaknessPenalty(150)
        public TeamSummary CalculateTeamSummary(List<Pokemon> teamMembers)
        {
            var summary = new TeamSummary();
            var allTypes = PokemonTypeEffectiveness.effectivenessChart.Keys.ToList();

            var offensiveCoverage = new Dictionary<string, float>();

            foreach (var defendingType in allTypes)
            {
                float bestMultiplier = 1f;

                foreach (var pokemon in teamMembers)
                {
                    foreach (var type in pokemon.Types)
                    {
                        string attackType = type.Type.Name.ToLower();

                        if (PokemonTypeEffectiveness.effectivenessChart
                            .TryGetValue(attackType, out var dict) &&
                            dict.TryGetValue(defendingType, out float mult))
                        {
                            bestMultiplier = Math.Max(bestMultiplier, mult);
                        }
                    }
                }

                offensiveCoverage[defendingType] = bestMultiplier;
            }

            var worstDefense = new Dictionary<string, float>();

            foreach (var attackingType in allTypes)
            {
                float worst = 0f;

                foreach (var pokemon in teamMembers)
                {
                    float multiplier = 1f;

                    foreach (var type in pokemon.Types)
                    {
                        string defenseType = type.Type.Name.ToLower();

                        if (PokemonTypeEffectiveness.effectivenessChart
                            .TryGetValue(attackingType, out var dict) &&
                            dict.TryGetValue(defenseType, out float mult))
                        {
                            multiplier *= mult;
                        }
                    }

                    worst = Math.Max(worst, multiplier);
                }

                worstDefense[attackingType] = worst;
            }

            int offensiveScore = 0;
            foreach (var v in offensiveCoverage.Values)
            {
                if (v >= 4f) offensiveScore += 25;
                else if (v >= 2f) offensiveScore += 20;
                else if (v >= 1f) offensiveScore += 10;
                else offensiveScore += 5;
            }
            offensiveScore = Math.Min(offensiveScore, 450);

            int defensiveScore = 0;
            foreach (var v in worstDefense.Values)
            {
                if (v == 0f) defensiveScore += 20;
                else if (v <= 0.25f) defensiveScore += 18;
                else if (v <= 0.5f) defensiveScore += 15;
                else if (v <= 1f) defensiveScore += 8;
            }

            int totalBST = teamMembers.Sum(p => p.TotalBaseStats);
            int bstScore = (int)(Math.Min(totalBST / 3000f, 1f) * 200);

            int weaknessPenalty = 0;
            foreach (var type in allTypes)
            {
                int weakCount = teamMembers.Count(p =>
                {
                    float mult = 1f;
                    foreach (var t in p.Types)
                    {
                        if (PokemonTypeEffectiveness.effectivenessChart
                            .TryGetValue(type, out var d) &&
                            d.TryGetValue(t.Type.Name.ToLower(), out float m))
                        {
                            mult *= m;
                        }
                    }
                    return mult > 1f;
                });

                if (weakCount >= 3) weaknessPenalty += 25;
                if (weakCount >= 4) weaknessPenalty += 35;
                if (weakCount >= 5) weaknessPenalty += 45;
            }

            weaknessPenalty = Math.Min(weaknessPenalty, 150);

            summary.TotalScore = Math.Clamp(
                offensiveScore + defensiveScore + bstScore - weaknessPenalty, 0, 1000
            );

            summary.Strengths = offensiveCoverage
                .Where(d => d.Value > 1f)
                .OrderByDescending(d => d.Value)
                .Select(d => $"{FormatPokemonName(d.Key)} ({d.Value:0.#}x)")
                .ToList();

            summary.Weaknesses = worstDefense
                .Where(d => d.Value > 1f)
                .OrderByDescending(d => d.Value)
                .Select(d => $"{FormatPokemonName(d.Key)} ({d.Value:0.#}x)")
                .ToList();

            if (summary.Strengths.Count == 0) summary.Strengths.Add("None");
            if (summary.Weaknesses.Count == 0) summary.Weaknesses.Add("None");

            return summary;
        }

        private static string FormatPokemonName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "N/A";

            return char.ToUpper(name[0]) + name.Substring(1);
        }
    }
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



