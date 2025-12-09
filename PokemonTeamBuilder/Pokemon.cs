
using System.Text.Json.Serialization;

namespace PokemonTeamBuilder
{
    public class Pokemon
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Height { get; set; }
        public float Weight { get; set; }
        public PokemonSprites Sprites { get; set; }
        public List<PokemonTypeWrapper> Types { get; set; }
        public List<PokemonStrengthWrapper> Strengths { get; set; }
        public List<PokemonStat> BaseTotal { get; set; }

        [JsonPropertyName("stats")]
        public List<PokemonStat> Stats { get; set; }

        [JsonIgnore]
        public bool Favourite { get; set; }

        [JsonIgnore]
        public int TotalBaseStats => Stats?.Sum(s => s.BaseStat) ?? 0;
    }

    public class PokemonSprites
    {
        public SpritesVersions Versions { get; set; }
    }

    public class SpritesVersions
    {
        [JsonPropertyName("generation-i")]
        public GenerationI GenerationI { get; set; }
    }

    public class GenerationI
    {
        [JsonPropertyName("red-blue")]
        public RedBlueSprites RedBlue { get; set; }
    }

    public class RedBlueSprites
    {
        [JsonPropertyName("front_default")]
        public string FrontDefault { get; set; }
    }

    public class PokemonTypeWrapper
    {
        public PokemonType Type { get; set; }
    }

    public class PokemonType
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class PokemonStat
    {
        [JsonPropertyName("base_stat")]
        public int BaseStat { get; set; }

        [JsonPropertyName("effort")]
        public int Effort { get; set; }

        [JsonPropertyName("stat")]
        public StatDetail Stat { get; set; }
    }

    public class StatDetail
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class PokemonTypeEffectiveness
    {
        /* 
         * attacker type -> defender type multiplier
         * 0f = no effect
         * 0.5f = not very effective
         * 2f = super effective
        */
        private static Dictionary<string, Dictionary<string, float>> effectivenessChart = new()
        {
            ["normal"] = new() { ["rock"] = 0.5f, ["ghost"] = 0f, ["steel"] = 0.5f },

            ["fire"] = new() { ["fire"] = 0.5f, ["water"] = 0.5f, ["grass"] = 2f, ["ice"] = 2f, 
            ["bug"] = 2f, ["rock"] = 0.5f, ["dragon"] = 0.5f, ["steel"] = 2f },

            ["water"] = new() { ["fire"] = 2f, ["water"] = 0.5f, ["grass"] = 0.5f, ["ground"] = 2f, 
            ["rock"] = 2f, ["dragon"] = 0.5f },

            ["electric"] = new() { ["water"] = 2f, ["electric"] = 0.5f, ["grass"] = 0.5f, 
            ["ground"] = 0f, ["flying"] = 2f, ["dragon"] = 0.5f },

            ["grass"] = new() { ["fire"] = 0.5f, ["water"] = 2f, ["grass"] = 0.5f, ["poison"] = 0.5f,
            ["ground"] = 2f, ["flying"] = 0.5f, ["bug"] = 0.5f, ["rock"] = 2f, ["dragon"] = 0.5f, 
            ["steel"] = 0.5f },

            ["ice"] = new() { ["fire"] = 0.5f, ["water"] = 0.5f, ["grass"] = 2f, ["ice"] = 0.5f,
            ["ground"] = 2f, ["flying"] = 2f, ["dragon"] = 2f, ["steel"] = 0.5f },

            ["fighting"] = new() { ["normal"] = 2f, ["ice"] = 2f, ["rock"] = 2f, ["dark"] = 2f,
            ["steel"] = 2f, ["poison"] = 0.5f, ["flying"] = 0.5f, ["psychic"] = 0.5f, 
            ["bug"] = 0.5f, ["ghost"] = 0f },

            ["poison"] = new() { ["grass"] = 2f, ["poison"] = 0.5f, ["ground"] = 0.5f, 
            ["rock"] = 0.5f, ["ghost"] = 0.5f, ["steel"] = 0f },

            ["ground"] = new() { ["fire"] = 2f, ["electric"] = 2f, ["grass"] = 0.5f, ["poison"] = 2f, 
            ["flying"] = 0f, ["bug"] = 0.5f, ["rock"] = 2f, ["steel"] = 2f },

            ["flying"] = new() { ["electric"] = 0.5f, ["grass"] = 2f, ["fighting"] = 2f, ["bug"] = 2f, 
            ["rock"] = 0.5f, ["steel"] = 0.5f },

            ["psychic"] = new() { ["fighting"] = 2f, ["poison"] = 2f, ["psychic"] = 0.5f, ["dark"] = 0f, 
            ["steel"] = 0.5f },

            ["bug"] = new() { ["fire"] = 0.5f, ["grass"] = 2f, ["fighting"] = 0.5f, ["poison"] = 0.5f, 
            ["flying"] = 0.5f, ["psychic"] = 2f, ["ghost"] = 0.5f, ["dark"] = 2f, ["steel"] = 0.5f },

            ["rock"] = new() { ["fire"] = 2f, ["ice"] = 2f, ["fighting"] = 0.5f, ["ground"] = 0.5f, 
            ["flying"] = 2f, ["bug"] = 2f, ["steel"] = 0.5f },

            ["ghost"] = new() { ["normal"] = 0f, ["psychic"] = 2f, ["ghost"] = 2f, ["dark"] = 0.5f, ["steel"] = 0.5f },

            ["dragon"] = new() { ["dragon"] = 2f, ["steel"] = 0.5f },

            ["dark"] = new() { ["fighting"] = 0.5f, ["psychic"] = 2f, ["ghost"] = 2f, ["dark"] = 0.5f, 
            ["steel"] = 0.5f },

            ["steel"] = new() { ["fire"] = 0.5f, ["water"] = 0.5f, ["electric"] = 0.5f, ["ice"] = 2f, 
            ["rock"] = 2f, ["steel"] = 0.5f }
        };

    }
}
