using System.Net.Http;
using System.Text.Json;

namespace PokemonTeamBuilder
{
    public partial class MainPage : ContentPage
    {

        private readonly HttpClient _httpClient = new HttpClient();

        public MainPage()
        {
            InitializeComponent();
  
        }

        private async void OnSearchBarPressed(object sender, EventArgs e)
        {
            string query = searchBar.Text?.Trim().ToLower() ?? string.Empty;
            //DisplayAlert("Search", $"You searched for: {query}", "OK");

            if (string.IsNullOrEmpty(query))
            {
                await DisplayAlert("Input Error", "Please enter a Pokémon name or ID.", "OK");
                return;
            }

            try
            {
                var pokemon = await PokemonData(query);

                string spriteUrl = pokemon?.Sprites?
                                           .Versions?
                                           .GenerationI?
                                           .RedBlue?
                                           .FrontDefault;

                if (!string.IsNullOrEmpty(spriteUrl))
                {
                    pokemonSprite.Source = spriteUrl;
                }
                else
                {
                    DisplayAlert("Not Found", "Sprite not found for this Pokémon.", "OK");
                    pokemonSprite.Source = null;
                }
            }

            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task<Pokemon> PokemonData(string name)
        {
            string url = $"https://pokeapi.co/api/v2/pokemon/{name}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Pokémon not found");

            var json = await response.Content.ReadAsStringAsync();

            var pokemon = JsonSerializer.Deserialize<Pokemon>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return pokemon;
        }
    }

}
