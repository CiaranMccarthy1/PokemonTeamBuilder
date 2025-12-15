using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel; 

namespace PokemonTeamBuilder
{
    public partial class MainPage : ContentPage
    {

        private readonly PokemonService pokemonService;
        private string currentPokemonName;
        public ObservableCollection<PokemonGridItem> AllPokemonNames { get; } = new();
        private bool isInitialized = false;
        private const int pageSize = 100; 
        private int offset = 0;         
        private bool isLoading = false;

        //takes HttpClient as parameter
        public MainPage(HttpClient httpClient)
        {
            InitializeComponent();
            pokemonService = new PokemonService(httpClient);

        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!isInitialized)
            {
                await LoadAllPokemonForGrid();
                isInitialized = true;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            pokemonSprite.Source = null;
        }

        private async void OnSearchBarPressed(object sender, EventArgs e)
        {
            string query = searchBar.Text?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrEmpty(query))
            {
                await DisplayAlert("Input Error", "Please enter a Pokémon name or ID.", "OK");
                return;
            }

            try
            {
                // Check if the Pokémon is marked as a favorite and if it's cached
                var favourites = await PokemonCache.GetFavorites();
                bool isFavourite = favourites.Contains(query);
                Pokemon pokemon;

                if (isFavourite && PokemonCache.IsCached(query))
                {
                    pokemon = await PokemonCache.GetCachedPokemon(query);
                }
                else
                {
                    pokemon = await pokemonService.GetPokemon(query);
                }

                if (pokemon != null)
                {
                    await DisplayPokemon(pokemon, isFavourite);
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

        private async Task DisplayPokemon(Pokemon pokemon, bool isFavourite)
        {
            string query = pokemon.Name.ToLower();

            // Load sprite from cache if available
            var cachedSpritePath = PokemonCache.GetCachedSprite(query);
            if (!string.IsNullOrEmpty(cachedSpritePath))
            {
                pokemonSprite.Source = cachedSpritePath;
            }
            else if (!string.IsNullOrEmpty(pokemon.Sprites?.FrontDefault))
            {
                pokemonSprite.Source = pokemon.Sprites.FrontDefault;
            }
            else
            {
                await DisplayAlert("Error", "Pokemon sprite not found", "OK");
                pokemonSprite.Source = null;
                return;
            }

            // Show details, hide grid
            allPokemonCollectionView.IsVisible = false;
            pokemonDetailsGrid.IsVisible = true;

            // Format and display Pokemon data
            string formattedName = FormatPokemonName(pokemon.Name);
            string types = pokemon.Types != null
                ? string.Join(", ", pokemon.Types.Select(t => FormatPokemonName(t.Type.Name)))
                : "N/A";

            pokemonNameLabel.Text = $"Name: {formattedName}";
            pokemonHeightLabel.Text = $"Height: {pokemon.Height / 10.0} M";
            pokemonWeightLabel.Text = $"Weight: {pokemon.Weight / 10.0} KG";
            pokemonTypeLabel.Text = $"Types: {types}";
            pokemonStatLabel.Text = $"Base Stat Total: {pokemon.TotalBaseStats}";
            pokemonStrenghtLabel.Text = FormatStrengths(pokemon.Strengths);
            pokemonWeaknessLabel.Text = FormatWeaknesses(pokemon.Weaknesses);

            currentPokemonName = pokemon.Name.ToLower();
            pokemonFavouriteButton.Text = isFavourite ? "★ Favourite" : "☆ Not Favourite";
            pokemonFavouriteButton.IsVisible = true;
        }


        private void searchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = e.NewTextValue?.ToLower() ?? string.Empty;

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
                var favourites = await PokemonCache.GetFavorites();
             
                if (favourites.Contains(currentPokemonName))
                {
                    favourites.Remove(currentPokemonName);
                    PokemonCache.RemoveFromCache(currentPokemonName);
                  
                    pokemonFavouriteButton.Text = "☆ Unfavourite";
                }
                else
                {
                    favourites.Add(currentPokemonName);
                    var pokemon = await pokemonService.GetPokemon(currentPokemonName);
                    await PokemonCache.CachePokemon(pokemon);

                    pokemonFavouriteButton.Text = "★ Favourite";
                }
               
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

        private static string FormatPokemonName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "N/A";

            return char.ToUpper(name[0]) + name.Substring(1);
        }

        public class PokemonGridItem
        {
            public string Name { get; set; }
            public int PokemonId { get; set; }
            public string SpriteUrl => $"{PokemonService.SpriteBaseUrl}{PokemonId}.png";
        }

        private async Task LoadAllPokemonForGrid()
        {
            if (isLoading || offset >= PokemonService.PokemonLimit)
                return;

            isLoading = true;

            try
            {
                var newPokemonItems = new List<PokemonGridItem>();

                for (int id = offset + 1; id <= (PokemonService.PokemonLimit); id++)
                {
                    var pokemon = await PokemonCache.GetCachedPokemonById(id);

                    if (pokemon != null)
                    {
                        newPokemonItems.Add(new PokemonGridItem
                        {
                            Name = FormatPokemonName(pokemon.Name),
                            PokemonId = pokemon.Id
                        });
                    }
                }

                if (allPokemonCollectionView.ItemsSource == null)
                {
                    allPokemonCollectionView.ItemsSource = AllPokemonNames;
                }

                foreach (var item in newPokemonItems)
                {
                    AllPokemonNames.Add(item);
                }

                offset += pageSize;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error Loading Data", $"Failed to load Pokémon: {ex.Message}", "OK");
            }

            isLoading = false;
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

        private async void AllPokemonCollectionView_RemainingItemsThresholdReached(object sender, EventArgs e)
        {
            await LoadAllPokemonForGrid();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            allPokemonCollectionView.HeightRequest = height -80;
        }

        private int ExtractIdFromUrl(string url)
        {
            var parts = url.TrimEnd('/').Split('/');
            return int.Parse(parts[^1]);
        }

        public void OnBackButtonClicked(object sender, EventArgs e)
        {
            allPokemonCollectionView.IsVisible = true;
            pokemonDetailsGrid.IsVisible = false;
            ClearPokemonDisplay();
        }
    }
}
