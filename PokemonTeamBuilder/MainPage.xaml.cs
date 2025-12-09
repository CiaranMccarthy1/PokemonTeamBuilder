using System.Net.Http;
using System.Text.Json;

namespace PokemonTeamBuilder
{
    public partial class MainPage : ContentPage
    {

        private readonly HttpClient _httpClient = new HttpClient();
        private List<string> allPokemonNames = GetGen1Pokemon();
        private string currentPokemonName;

        public MainPage()
        {
            InitializeComponent();
  
        }

        private async void OnSearchBarPressed(object sender, EventArgs e)
        {
            string query = searchBar.Text?.Trim().ToLower() ?? string.Empty;
            await DisplayAlert("Search", $"You searched for: {query}", "OK");

            if (string.IsNullOrEmpty(query))
            {
                await DisplayAlert("Input Error", "Please enter a Pokémon name or ID.", "OK");
                return;
            }

            try
            {
                Pokemon pokemon;

                var favourite = await PokemonCache.GetFavorites();
                bool isFavourite = favourite.Contains(query);

                if (isFavourite && PokemonCache.IsCached(query)) {
                    pokemon = await PokemonCache.GetCachedPokemon(query);
                }
                else
                {
                    pokemon = await PokemonData(query);
                }

                if (pokemon != null)
                {
                    string spriteUrl = pokemon.Sprites?.Versions?.GenerationI?.RedBlue?.FrontDefault;

                    if (!string.IsNullOrEmpty(spriteUrl))
                    {
                        pokemonSprite.Source = spriteUrl;

                        string name = char.ToUpper(pokemon.Name[0]) + pokemon.Name.Substring(1);
                        string types = string.Join(", ", pokemon.Types.Select(t => t.Type.Name)) ?? "N/A";

                        pokemonNameLabel.Text = $"Name: {name ?? "N/A"}";
                        pokemonHeightLabel.Text = $"Height: {pokemon?.Height / 10 ?? 0} M";
                        pokemonWeightLabel.Text = $"Weight: {pokemon?.Weight / 10 ?? 0} KG";
                        pokemonTypeLabel.Text = $"Types: {types ?? "N/A"}";
                    }
                    else
                    {
                        await DisplayAlert("Error", "Pokemon sprite not found", "OK");
                        pokemonSprite.Source = null;
                    }
                    currentPokemonName = pokemon.Name.ToLower();
                }
            }

            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }

            searchBar.Text = string.Empty;
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

        private void searchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = e.NewTextValue.ToLower();

            if (string.IsNullOrWhiteSpace(query))
            {
                suggestionListView.ItemsSource = null; 
                suggestionListView.IsVisible = false;   
                return;
            }

            var suggestions = allPokemonNames
                              .Where(name => name.StartsWith(query))
                              .Take(10)
                              .ToList();

            suggestionListView.IsVisible = suggestions.Count > 0;

            suggestionListView.ItemsSource = suggestions;
        }

        private void suggestionListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return; 

            string selectedPokemon = e.SelectedItem.ToString();

            searchBar.Text = selectedPokemon;

            OnSearchBarPressed(null, null);

            suggestionListView.SelectedItem = null;
        }


        public static List<string> GetGen1Pokemon()
        {
            return new List<string>
                {
                    "bulbasaur","ivysaur","venusaur","charmander","charmeleon","charizard",
                    "squirtle","wartortle","blastoise","caterpie","metapod","butterfree",
                    "weedle","kakuna","beedrill","pidgey","pidgeotto","pidgeot",
                    "rattata","raticate","spearow","fearow","ekans","arbok",
                    "pikachu","raichu","sandshrew","sandslash","nidoran-f","nidorina",
                    "nidoqueen","nidoran-m","nidorino","nidoking","clefairy","clefable",
                    "vulpix","ninetales","jigglypuff","wigglytuff","zubat","golbat",
                    "oddish","gloom","vileplume","paras","parasect","venonat","venomoth",
                    "diglett","dugtrio","meowth","persian","psyduck","golduck","mankey",
                    "primeape","growlithe","arcanine","poliwag","poliwhirl","poliwrath",
                    "abra","kadabra","alakazam","machop","machoke","machamp","bellsprout",
                    "weepinbell","victreebel","tentacool","tentacruel","geodude","graveler",
                    "golem","ponyta","rapidash","slowpoke","slowbro","magnemite","magneton",
                    "farfetchd","doduo","dodrio","seel","dewgong","grimer","muk","shellder",
                    "cloyster","gastly","haunter","gengar","onix","drowzee","hypno","krabby",
                    "kingler","voltorb","electrode","exeggcute","exeggutor","cubone","marowak",
                    "hitmonlee","hitmonchan","lickitung","koffing","weezing","rhyhorn","rhydon",
                    "chansey","tangela","kangaskhan","horsea","seadra","goldeen","seaking",
                    "staryu","starmie","mr-mime","scyther","jynx","electabuzz","magmar",
                    "pinsir","tauros","magikarp","gyarados","lapras","ditto","eevee",
                    "vaporeon","jolteon","flareon","porygon","omanyte","omastar","kabuto",
                    "kabutops","aerodactyl","snorlax","articuno","zapdos","moltres",
                    "dratini","dragonair","dragonite","mewtwo","mew"
            };
        }

        private async void OnFavouriteClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentPokemonName))
            {
                return;
            }

            var favourites = await PokemonCache.GetFavorites();

            if (favourites.Contains(currentPokemonName))
            {
                favourites.Remove(currentPokemonName);
                PokemonCache.RemoveFromCache(currentPokemonName);
                pokemonFavouriteButton.Text = "★";
            }
            else
            {
                favourites.Add(currentPokemonName);
                var pokemon = await PokemonData(currentPokemonName);
                await PokemonCache.CachePokemon(pokemon);
                pokemonFavouriteButton.Text = "⭐";
            }
        }



    }

}
