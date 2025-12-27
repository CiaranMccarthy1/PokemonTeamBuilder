namespace PokemonTeamBuilder
{
    public static class PokemonTeamCalculator
    {
        //TeamScore(0, 1000) = Offense(450) + Defense(300) + BaseStats(200) − WeaknessPenalty(150)

        private const int MaxTeamBST = 720 * 6;
        public static TeamSummary CalculateTeamSummary(List<Pokemon> teamMembers)
        {
            var summary = new TeamSummary();
            
            if (teamMembers == null || teamMembers.Count == 0)
                return summary;

            var allTypes = PokemonTypeEffectiveness.effectivenessChart.Keys.ToList();

            var offensiveCoverage = new Dictionary<string, float>();

            foreach (var defendingType in allTypes)
            {
                float bestMultiplier = 1f;

                foreach (var pokemon in teamMembers)
                {
                    if (pokemon.Types == null) continue;
                    
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

            var bestDefense = new Dictionary<string, float>();

            foreach (var attackingType in allTypes)
            {
                float best = float.MaxValue;

                foreach (var pokemon in teamMembers)
                {
                    if (pokemon.Types == null) continue;
                    
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

                    best = Math.Min(best, multiplier);
                }

                bestDefense[attackingType] = best == float.MaxValue ? 1f : best;
            }

            var worstDefense = new Dictionary<string, float>();

            foreach (var attackingType in allTypes)
            {
                float worst = 0f;

                foreach (var pokemon in teamMembers)
                {
                    if (pokemon.Types == null) continue;
                    
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
            foreach (var v in bestDefense.Values)
            {
                if (v == 0f) defensiveScore += 20;      
                else if (v <= 0.25f) defensiveScore += 18; 
                else if (v <= 0.5f) defensiveScore += 15;  
                else if (v <= 1f) defensiveScore += 10;    
                else if (v <= 2f) defensiveScore += 5;    
                else defensiveScore += 0;                   
            }
            defensiveScore = Math.Min(defensiveScore, 300);

            int totalBST = teamMembers.Sum(p => p.TotalBaseStats);
            int bstScore = (int)(Math.Min(totalBST / MaxTeamBST, 1f) * 200);

            int weaknessPenalty = 0;
            foreach (var type in allTypes)
            {
                int weakCount = teamMembers.Count(p =>
                {
                    if (p.Types == null) return false;
                    
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

            var strengthTypes = offensiveCoverage
                .Where(d => d.Value > 1f)
                .Select(d => d.Key)
                .ToHashSet();

            var weaknessTypes = worstDefense
                .Where(d => d.Value > 1f)
                .Select(d => d.Key)
                .ToHashSet();

            var cancelledTypes = strengthTypes.Intersect(weaknessTypes).ToHashSet();

            summary.Strengths = offensiveCoverage
                .Where(d => d.Value > 1f && !cancelledTypes.Contains(d.Key))
                .OrderByDescending(d => d.Value)
                .Select(d => $"{PokemonFormatter.FormatPokemonName(d.Key)} ({d.Value:0.#}x)")
                .ToList();

            summary.Weaknesses = worstDefense
                .Where(d => d.Value > 1f && !cancelledTypes.Contains(d.Key))
                .OrderByDescending(d => d.Value)
                .Select(d => $"{PokemonFormatter.FormatPokemonName(d.Key)} ({d.Value:0.#}x)")
                .ToList();

            if (summary.Strengths.Count == 0) summary.Strengths.Add("None");
            if (summary.Weaknesses.Count == 0) summary.Weaknesses.Add("None");

            //System.Diagnostics.Debug.WriteLine($"=== Team Score Breakdown ===");
            //System.Diagnostics.Debug.WriteLine($"Offensive Score: {offensiveScore}");
            //System.Diagnostics.Debug.WriteLine($"Defensive Score: {defensiveScore}");
            //System.Diagnostics.Debug.WriteLine($"BST Score: {bstScore} (Total BST: {totalBST})");
            //System.Diagnostics.Debug.WriteLine($"Weakness Penalty: -{weaknessPenalty}");
            //System.Diagnostics.Debug.WriteLine($"Final Score: {summary.TotalScore}");

            return summary;
        }
    }
}
