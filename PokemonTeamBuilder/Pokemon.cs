
using System.Text.Json.Serialization;

namespace PokemonTeamBuilder
{
    class Pokemon
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BaseExperience { get; set; }
        public int Height { get; set; }
        public bool IsDefault { get; set; }
        public int Order { get; set; }
        public int Weight { get; set; }

        public PokemonSprites Sprites { get; set; }
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
}
