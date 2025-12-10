using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace PokemonTeamBuilder
{
    public partial class MainPage : ContentPage
    {

        private readonly PokemonService pokemonService;
        private string currentPokemonName;
        public List<PokemonGridItem> AllPokemonNames = new();

        //takes HttpClient as parameter
        public MainPage(HttpClient httpClient)
        {
            InitializeComponent();
            pokemonService = new PokemonService(httpClient);
            _ = LoadAllPokemonForGrid();

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
                    pokemon = await pokemonService.GetPokemon(query);
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
                    allPokemonCollectionView.IsVisible = false;
                    pokemonDetailsGrid.IsVisible = true;
                    
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
            var suggestions = AllPokemonNames
                              .Where(p => p.Name.ToLower().StartsWith(query))
                              .Select(p => p.Name) 
                              .Take(10)
                              .ToList();

            // hides if search abr empty or no suggestions
            suggestionListView.IsVisible = suggestions.Count > 0;

            suggestionListView.ItemsSource = suggestions;
        }

        private void suggestionListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is not string selectedPokemon)
                return;

            searchBar.Text = selectedPokemon;
            OnSearchBarPressed(null, null);

            suggestionListView.SelectedItem = null;
            suggestionListView.IsVisible = false;
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
                    var pokemon = await pokemonService.GetPokemon(currentPokemonName);
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

        public class PokemonGridItem
        {
            public string Name { get; set; }
            public string SpriteUrl { get; set; }
        }

        private async Task LoadAllPokemonForGrid()
        {
            var names = await pokemonService.GetAllPokemonNames();
            AllPokemonNames = names
                .Select((name, idx) => new PokemonGridItem
                {
                    Name = char.ToUpper(name[0]) + name.Substring(1),
                    SpriteUrl = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{idx + 1}.png"
                })
                .ToList();
            allPokemonCollectionView.ItemsSource = null;
            allPokemonCollectionView.ItemsSource = AllPokemonNames;
        }

        private void AllPokemonCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PokemonGridItem selected)
            {
                searchBar.Text = selected.Name.ToLower();
                OnSearchBarPressed(null, null);
                allPokemonCollectionView.SelectedItem = null;
            }
        }

    }
}
