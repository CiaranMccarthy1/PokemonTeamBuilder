using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonTeamBuilder
{
    public static class PokemonFormatter
    {

        private static readonly Dictionary<string, string> TypeEmojis = new(StringComparer.OrdinalIgnoreCase)
        {
            ["normal"] = "◻️",
            ["fire"] = "🔥",
            ["water"] = "🌊",
            ["electric"] = "⚡",
            ["grass"] = "🍃",
            ["ice"] = "❄️",
            ["fighting"] = "🥊",
            ["poison"] = "🧪",
            ["ground"] = "⛰️",
            ["flying"] = "🪽",
            ["psychic"] = "🪬",
            ["bug"] = "🪲",
            ["rock"] = "🪨",
            ["ghost"] = "👻",
            ["dragon"] = "🐉",
            ["dark"] = "🌑",
            ["steel"] = "⚙️",
            ["fairy"] = "🧚🏻‍"
        };

        public static string FormatPokemonName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "N/A";

            return char.ToUpper(name[0]) + name.Substring(1);
        }

        public static string FormatStrengths(List<PokemonStrengthWrapper> strengths)
        {
            if (strengths == null || strengths.Count == 0)
                return "Strengths: None";

            var formattedList = strengths
                .OrderByDescending(s => s.Multiplier)
                .Select(s =>
                {
                    var typeName = s.Type ?? string.Empty;
                    var emoji = TypeEmojis.TryGetValue(typeName, out var e) ? e + " " : string.Empty;
                    var display = string.IsNullOrEmpty(typeName) ? "Unknown" : char.ToUpper(typeName[0]) + typeName.Substring(1);
                    return $"{emoji}{display} ({s.Multiplier:0.##}x)";
                });

            return $"Strengths: {string.Join(", ", formattedList)}";
        }

        public static string FormatWeaknesses(List<PokemonWeaknessWrapper> weaknesses)
        {
            if (weaknesses == null || weaknesses.Count == 0)
                return "Weaknesses: None";

            var formattedList = weaknesses
                .OrderByDescending(w => w.Multiplier)
                .Select(w =>
                {
                    var typeName = w.Type ?? string.Empty;
                    var emoji = TypeEmojis.TryGetValue(typeName, out var e) ? e + " " : string.Empty;
                    var display = string.IsNullOrEmpty(typeName) ? "Unknown" : char.ToUpper(typeName[0]) + typeName.Substring(1);
                    return $"{emoji}{display} ({w.Multiplier:0.##}x)";
                });

            return $"Weaknesses: {string.Join(", ", formattedList)}";
        }
    }
}
