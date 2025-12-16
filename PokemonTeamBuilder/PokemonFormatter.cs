using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonTeamBuilder
{
    public static class PokemonFormatter
    {

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
                .Select(s => $"{char.ToUpper(s.Type[0]) + s.Type.Substring(1)} ({s.Multiplier:0.##}x)");

            return $"Strengths: {string.Join(", ", formattedList)}";
        }

        public static string FormatWeaknesses(List<PokemonWeaknessWrapper> weaknesses)
        {
            if (weaknesses == null || weaknesses.Count == 0)
                return "Weaknesses: None";

            var formattedList = weaknesses
                .OrderByDescending(w => w.Multiplier)
                .Select(w => $"{char.ToUpper(w.Type[0]) + w.Type.Substring(1)} ({w.Multiplier:0.##}x)");

            return $"Weaknesses: {string.Join(", ", formattedList)}";
        }
    }
}
