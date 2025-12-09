
using System.Text.Json.Serialization;

namespace PokemonTeamBuilder
{
    public class Pokemon
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BaseExperience { get; set; }
        public float Height { get; set; }
        public float Weight { get; set; }
        public PokemonSprites Sprites { get; set; }
        public List<PokemonTypeWrapper> Types { get; set; }

        [JsonIgnore]
        public bool Favourite { get; set; }
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
}
