using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonTeamBuilder
{
    public static class Debug
    {

        public static bool IsEnabled => Preferences.Get("DebugMode", false);

        public static async Task ShowCacheLocation(Page page)
        {
            await ShowAlert(page, "Cache Location", FileSystem.AppDataDirectory);
        }

        public static async Task ShowDownloadComplete(Page page)
        {
            await ShowAlert(page, "Download Complete", "All Pokémon data downloaded successfully!");
        }

        public static void LogException(Exception ex, string context = "")
        {
            if (IsEnabled)
            {
                string prefix = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";
                Debug.LogException(ex, $"{prefix}Exception");
                Debug.LogException(ex, $"StackTrace");
            }
        }

        public static void LogTeamSummary(TeamSummary summary)
        {
                Log($"Team Summary:\n" + 
                    $"- Total Score: {summary.TotalScore}\n" + 
                    $"- Strengths: {string.Join(", ", summary.Strengths)}\n" + 
                    $"- Weaknesses: {string.Join(", ", summary.Weaknesses)}");
        }

        public static void Log(string message)
        {
            if (IsEnabled)
            {
                Console.WriteLine($"[DEBUG] {message}");
            }
        }

        public static async Task ShowAlert(Page page, string title, string message, string cancel = "OK")
        {
            if (IsEnabled)
            {
                await page.DisplayAlert(title, message, cancel);
            }
        }
    }
}
