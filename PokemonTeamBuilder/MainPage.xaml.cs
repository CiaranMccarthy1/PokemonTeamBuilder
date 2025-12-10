using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace PokemonTeamBuilder
{
    public partial class MainPage : ContentPage
    {

        private readonly HttpClient _httpClient = new HttpClient();
        private List<string> allPokemonNames = GetGen1Pokemon();
        private string currentPokemonName;

        //takes HttpClient as parameter
        public MainPage(HttpClient httpClient)
        {
            InitializeComponent();
            _httpClient = httpClient;
  
        }

        private async void OnSearchBarPressed(object sender, EventArgs e)
        {
            ClearPokemonDisplay();

            string query = searchBar.Text?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrEmpty(query))
            {
                await DisplayAlert("Input Error", "Please enter a Pokémon name or ID.", "OK");
                return;
            }

            try
            {
                Pokemon pokemon;

                // Check if the Pokémon is marked as a favorite and if it's cached
                var favourite = await PokemonCache.GetFavorites();
                bool isFavourite = favourite.Contains(query);

                if (isFavourite && PokemonCache.IsCached(query))
                {
                    pokemon = await PokemonCache.GetCachedPokemon(query);
                }
                else
                {
                    // Gets pokemon data from API
                    pokemon = await PokemonData(query);
                }

                if (pokemon != null)
                {
                    // Load sprite from cache if available 
                    var cachedSpritePath = PokemonCache.GetCachedSprite(query);

                    if (!string.IsNullOrEmpty(cachedSpritePath))
                    {
                        pokemonSprite.Source = cachedSpritePath;
                    }
                    else
                    {
                        string spriteUrl = pokemon.Sprites?.FrontDefault;

                        if (!string.IsNullOrEmpty(spriteUrl))
                        {
                            pokemonSprite.Source = spriteUrl;
                        }
                        else
                        {
                            await DisplayAlert("Error", "Pokemon sprite not found", "OK");
                            pokemonSprite.Source = null;
                            return;
                        }
                    }
                    //Formats Name and types 
                    string name = char.ToUpper(pokemon.Name[0]) + pokemon.Name.Substring(1);
                    string types = pokemon.Types != null ? string.Join(", ", pokemon.Types.Select(t => t.Type.Name)) : "N/A";

                    pokemonNameLabel.Text = $"Name: {name ?? "N/A"}";
                    pokemonHeightLabel.Text = $"Height: {pokemon?.Height / 10 ?? 0} M";
                    pokemonWeightLabel.Text = $"Weight: {pokemon?.Weight / 10 ?? 0} KG";
                    pokemonTypeLabel.Text = $"Types: {types ?? "N/A"}";
                    pokemonStatLabel.Text = $"Base Stat Total: {pokemon.TotalBaseStats}";
                    currentPokemonName = pokemon.Name.ToLower();
                    pokemonFavouriteButton.Text = isFavourite ? "Favourite" : "Not favourite";
                    pokemonFavouriteButton.IsVisible = true;
                    pokemonStrenghtLabel.Text = FormatStrengths(pokemon.Strengths);
                    pokemonWeaknessLabel.Text = FormatWeaknesses(pokemon.Weaknesses);
                }
                else
                {
                    await DisplayAlert("Error", "Pokémon data could not be retrieved.", "OK");

                }
            }

            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            // Clear search bar after search
            searchBar.Text = string.Empty;
        }

        private async Task<Pokemon> PokemonData(string name)
        {
            // API endpoint to get pokemon data
            string url = $"https://pokeapi.co/api/v2/pokemon/{name}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Pokémon not found");

            // Deserialize JSON response to string
            var json = await response.Content.ReadAsStringAsync();

            var pokemon = JsonSerializer.Deserialize<Pokemon>(json, new JsonSerializerOptions
            {
                // make deserialization case insensitive
                PropertyNameCaseInsensitive = true
            });

            // Calculate strengths and weaknesses based on types
            if (pokemon != null && pokemon.Types != null)
            {
                var pokemonTypeNames = pokemon.Types.Select(t => t.Type.Name).ToList();
                pokemon.Strengths = PokemonTypeEffectiveness.GetStrengths(pokemonTypeNames);
                pokemon.Weaknesses = PokemonTypeEffectiveness.GetWeaknesses(pokemonTypeNames);
            }

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

            // gets 10 pokeomon names that start with the query
            var suggestions = allPokemonNames
                              .Where(name => name.StartsWith(query))
                              .Take(10)
                              .ToList();

            // hides if seatch abr empty or no suggestions
            suggestionListView.IsVisible = suggestions.Count > 0;

            suggestionListView.ItemsSource = suggestions;
        }

        private void suggestionListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return; 

            string selectedPokemon = e.SelectedItem.ToString();

            searchBar.Text = selectedPokemon;
            // Trigger search by passing null 
            OnSearchBarPressed(null, null);

            suggestionListView.SelectedItem = null;
        }

        // Need to move to json file
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

            try
            {
                // loads current favourites to memory
                var favourites = await PokemonCache.GetFavorites();

                // toggles favourite status
                if (favourites.Contains(currentPokemonName))
                {
                    favourites.Remove(currentPokemonName);
                    PokemonCache.RemoveFromCache(currentPokemonName);
                    pokemonFavouriteButton.Text = "Not favourite";
                }
                else
                {
                    favourites.Add(currentPokemonName);
                    var pokemon = await PokemonData(currentPokemonName);
                    await PokemonCache.CachePokemon(pokemon);
                    pokemonFavouriteButton.Text = "Favourite";
                }
                // updates favorites file
                await PokemonCache.SaveFavorites(favourites);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }


        }

        private void ClearPokemonDisplay()
        {
            // Resets all display elements
            pokemonSprite.Source = null;
            pokemonNameLabel.Text = string.Empty;
            pokemonHeightLabel.Text = string.Empty;
            pokemonWeightLabel.Text = string.Empty;
            pokemonTypeLabel.Text = string.Empty;
            pokemonStatLabel.Text = string.Empty;
            pokemonStrenghtLabel.Text = string.Empty;
            pokemonWeaknessLabel.Text = string.Empty;
            pokemonFavouriteButton.IsVisible = false;
            currentPokemonName = string.Empty;
        }

        private string FormatStrengths(List<PokemonStrengthWrapper> strengths)
        {
            if (strengths == null || strengths.Count == 0)
            {
                return "Strengths: None";
            }

            // lambda to get and format each strength type and multiplier
            var formattedList = strengths
                .OrderByDescending(s => s.Multiplier)
                .Select(s => $"{char.ToUpper(s.Type[0]) + s.Type.Substring(1)} ({s.Multiplier:0.##}x)");

            return $"Strengths: {string.Join(", ", formattedList)}";
        }

        private string FormatWeaknesses(List<PokemonWeaknessWrapper> weaknesses)
        {
            if (weaknesses == null || weaknesses.Count == 0)
            {
                return "Weaknesses: None";
            }

            // formatting each weakness type and multiplier
            var formattedList = weaknesses
                .OrderByDescending(w => w.Multiplier)
                .Select(w => $"{char.ToUpper(w.Type[0]) + w.Type.Substring(1)} ({w.Multiplier:0.##}x)");

            return $"Weakness: {string.Join(", ", formattedList)}";
        }



    }

}
