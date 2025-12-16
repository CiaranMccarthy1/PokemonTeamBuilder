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
        public List<PokemonWeaknessWrapper> Weaknesses { get; set; }
        public List<PokemonStat> BaseTotal { get; set; }

        // Need to move to sperate class
        [JsonPropertyName("stats")]
        public List<PokemonStat> Stats { get; set; }

        [JsonIgnore]
        public bool Favourite { get; set; }

        [JsonIgnore]
        public int TotalBaseStats => Stats?.Sum(s => s.BaseStat) ?? 0;
    }

    public class PokemonSprites
    {
        [JsonPropertyName("front_default")]
        public string FrontDefault { get; set; }
        public SpritesVersions Versions { get; set; }
    }

    public class SpritesVersions
    {
        [JsonPropertyName("generation-i")]
        public GenerationI GenerationI { get; set; }

        [JsonPropertyName("generation-ii")]
        public GenerationI GenerationII { get; set; }

        [JsonPropertyName("generation-iii")]
        public GenerationI GenerationIIII { get; set; }

        [JsonPropertyName("generation-iv")]
        public GenerationI GenerationIV { get; set; }

        [JsonPropertyName("generation-v")]
        public GenerationI GenerationV { get; set; }
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

    public class GenerationII
    {
        [JsonPropertyName("crystal")]
        public CrystalSprites Crystal { get; set; }

    }

    public class CrystalSprites
    {
        [JsonPropertyName("front_default")]
        public string FrontDefault { get; set; }
    }

    public class PokemonTypeWrapper
    {
        public PokemonType Type { get; set; }
    }

    public class GenerationIII
    {
        [JsonPropertyName("emerald")]
        public EmeraldSprites Emerald { get; set; }
    }

    public class EmeraldSprites
    {
        [JsonPropertyName("front_default")]
        public string FrontDefault { get; set; }
    }

    public class GenerationIV
    {
        [JsonPropertyName("platinum")]
        public PlatinumSprites Platinum { get; set; }
    }

    public class PlatinumSprites
    {
        [JsonPropertyName("front_default")]
        public string FrontDefault { get; set; }
    }

    public class GenerationV
    {
        [JsonPropertyName("black-white")]
        public BlackWhiteSprites BlackWhite { get; set; }
    }

    public class BlackWhiteSprites
    {
        [JsonPropertyName("front_default")]
        public string FrontDefault { get; set; }
    }


    //-----------------------------------------------------------
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

    public class PokemonStrengthWrapper
    {
        public string Type { get; set; }
        public float Multiplier { get; set; }
    }

    public class PokemonWeaknessWrapper
    {
        public string Type { get; set; }
        public float Multiplier { get; set; }
    }

    public class PokemonTypeEffectiveness
    {
        /* 
         * attacker type -> defender type multiplier
         * 0f = no effect
         * 0.5f = not very effective
         * 2f = super effective
        */
        public static Dictionary<string, Dictionary<string, float>> effectivenessChart = new()
        {
            ["normal"] = new() 
            { 
                ["rock"] = 0.5f, 
                ["ghost"] = 0f, 
                ["steel"] = 0.5f 
            },

            ["fire"] = new()
            {
                ["fire"] = 0.5f,
                ["water"] = 0.5f,
                ["grass"] = 2f,
                ["ice"] = 2f,
                ["bug"] = 2f,
                ["rock"] = 0.5f,
                ["dragon"] = 0.5f,
                ["steel"] = 2f,
                ["fairy"] = 0.5f
            },

            ["water"] = new()
            {
                ["fire"] = 2f,
                ["water"] = 0.5f,
                ["grass"] = 0.5f,
                ["ground"] = 2f,
                ["rock"] = 2f,
                ["dragon"] = 0.5f
            },

            ["electric"] = new()
            {
                ["water"] = 2f,
                ["electric"] = 0.5f,
                ["grass"] = 0.5f,
                ["ground"] = 0f,
                ["flying"] = 2f,
                ["dragon"] = 0.5f
            },

            ["grass"] = new()
            {
                ["fire"] = 0.5f,
                ["water"] = 2f,
                ["grass"] = 0.5f,
                ["poison"] = 0.5f,
                ["ground"] = 2f,
                ["flying"] = 0.5f,
                ["bug"] = 0.5f,
                ["rock"] = 2f,
                ["dragon"] = 0.5f,
                ["steel"] = 0.5f
            },

            ["ice"] = new()
            {
                ["fire"] = 0.5f,
                ["water"] = 0.5f,
                ["grass"] = 2f,
                ["ice"] = 0.5f,
                ["ground"] = 2f,
                ["flying"] = 2f,
                ["dragon"] = 2f,
                ["steel"] = 0.5f
            },

            ["fighting"] = new()
            {
                ["normal"] = 2f,
                ["ice"] = 2f,
                ["rock"] = 2f,
                ["dark"] = 2f,
                ["steel"] = 2f,
                ["poison"] = 0.5f,
                ["flying"] = 0.5f,
                ["psychic"] = 0.5f,
                ["bug"] = 0.5f,
                ["ghost"] = 0f
            },

            ["poison"] = new()
            {
                ["grass"] = 2f,
                ["poison"] = 0.5f,
                ["ground"] = 0.5f,
                ["rock"] = 0.5f,
                ["ghost"] = 0.5f,
                ["steel"] = 0f,
                ["fairy"] = 2f
            },

            ["ground"] = new()
            {
                ["fire"] = 2f,
                ["electric"] = 2f,
                ["grass"] = 0.5f,
                ["poison"] = 2f,
                ["flying"] = 0f,
                ["bug"] = 0.5f,
                ["rock"] = 2f,
                ["steel"] = 2f
            },

            ["flying"] = new()
            {
                ["electric"] = 0.5f,
                ["grass"] = 2f,
                ["fighting"] = 2f,
                ["bug"] = 2f,
                ["rock"] = 0.5f,
                ["steel"] = 0.5f
            },

            ["psychic"] = new()
            {
                ["fighting"] = 2f,
                ["poison"] = 2f,
                ["psychic"] = 0.5f,
                ["dark"] = 0f,
                ["steel"] = 0.5f
            },

            ["bug"] = new()
            {
                ["fire"] = 0.5f,
                ["grass"] = 2f,
                ["fighting"] = 0.5f,
                ["poison"] = 0.5f,
                ["flying"] = 0.5f,
                ["psychic"] = 2f,
                ["ghost"] = 0.5f,
                ["dark"] = 2f,
                ["steel"] = 0.5f
            },

            ["rock"] = new()
            {
                ["fire"] = 2f,
                ["ice"] = 2f,
                ["fighting"] = 0.5f,
                ["ground"] = 0.5f,
                ["flying"] = 2f,
                ["bug"] = 2f,
                ["steel"] = 0.5f
            },

            ["ghost"] = new() 
            { 
                ["normal"] = 0f, 
                ["psychic"] = 2f,
                ["ghost"] = 2f, 
                ["dark"] = 0.5f, 
                ["steel"] = 0.5f
            },

            ["dragon"] = new() 
            { 
                ["dragon"] = 2f,
                ["steel"] = 0.5f,
                ["fairy"] = 0f  
            },

            ["dark"] = new()
            {
                ["fighting"] = 0.5f,
                ["psychic"] = 2f,
                ["ghost"] = 2f,
                ["dark"] = 0.5f,
                ["steel"] = 0.5f
            },

            ["steel"] = new()
            {
                ["fire"] = 0.5f,
                ["water"] = 0.5f,
                ["electric"] = 0.5f,
                ["ice"] = 2f,
                ["rock"] = 2f,
                ["steel"] = 0.5f,
                ["fairy"] = 2f
            },

            ["fairy"] = new()
            {
                ["fire"] = 0.5f,
                ["fighting"] = 2f,
                ["poison"] = 0.5f,
                ["dragon"] = 2f,
                ["dark"] = 2f,
                ["steel"] = 0.5f
            }
        };

        public static List<PokemonStrengthWrapper> GetStrengths(List<string> pokemonTypes)
        {
            var strengths = new Dictionary<string, float>();

            foreach (var attacker in pokemonTypes)
            {
                // Skip if attacker type not found in chart
                if (!effectivenessChart.ContainsKey(attacker))
                    continue;
                // Loop through defender types and their effectiveness
                foreach (var defender in effectivenessChart[attacker])
                {
                    // Only consider strengths (effectiveness > 1)
                    if (defender.Value > 1f)
                    {
                        // If defender type not already in strengths, add it
                        if (!strengths.ContainsKey(defender.Key))
                        {
                            strengths[defender.Key] = defender.Value;
                        }
                        else
                        {
                            strengths[defender.Key] *= defender.Value;
                        }
                    }
                }
            }

            return strengths
                .Where(s => s.Value > 1f)
                .Select(s => new PokemonStrengthWrapper
                {
                    Type = s.Key,
                    Multiplier = s.Value
                })
                .ToList();
        }

        public static List<PokemonWeaknessWrapper> GetWeaknesses(List<string> pokemonTypes)
        {
            var defenseMultipliers = new Dictionary<string, float>();


            foreach (var defender in pokemonTypes)
            {
                foreach (var attacker in effectivenessChart)
                {

                    float effectiveness = 1f;

                    if (attacker.Value.ContainsKey(defender))
                    {
                        effectiveness = attacker.Value[defender];
                    }

                    if (!defenseMultipliers.ContainsKey(attacker.Key))
                        defenseMultipliers[attacker.Key] = effectiveness;
                    else
                        defenseMultipliers[attacker.Key] *= effectiveness;
                }
            }

            return defenseMultipliers
                .Where(s => s.Value > 1f)
                .Select(s => new PokemonWeaknessWrapper
                {
                    Type = s.Key,
                    Multiplier = s.Value
                })
                .ToList();
        }


    }

    public class PokemonGridItem
    {
        public int PokemonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SpriteUrl => PokemonCache.GetCachedSpriteById(PokemonId) 
            ?? $"{PokemonService.SpriteBaseUrl}{PokemonId}.png";
    }

    public class PokemonTeam
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<TeamPokemon> Pokemon { get; set; } = new List<TeamPokemon>();
        public int PokemonCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TeamPokemon
    {
        public string Name { get; set; } = string.Empty;
        public string SpriteUrl { get; set; } = string.Empty;
        public List<string> Types { get; set; } = new List<string>();
        public int Level { get; set; }
    }


    public class TeamSummary
    {
        public int TotalScore { get; set; }
        public List<string> Weaknesses { get; set; } = new List<string>();
        public List<string> Strengths { get; set; } = new List<string>();
    }



}
