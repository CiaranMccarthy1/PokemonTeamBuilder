using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel; 

namespace PokemonTeamBuilder
{
    [QueryProperty(nameof(TeamId), "teamId")]
    public partial class MainPage : ContentPage
    {
        private readonly PokemonService pokemonService;
        private string currentPokemonName = string.Empty;
        public ObservableCollection<PokemonGridItem> AllPokemonNames { get; } = new();
        private bool isInitialized = false;
        private bool isLoading = false;
        private int? teamId;


        public MainPage(HttpClient httpClient)
        {
            InitializeComponent();
            pokemonService = new PokemonService(httpClient);

        }

        public string TeamId
        {
            set
            {
                if (int.TryParse(value, out int id))
                {
                    teamId = id;
                }
            }
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

        private async Task LoadAllPokemonForGrid()
        {
            if (isLoading)
                return;

            isLoading = true;

            try
            {
                for (int id = 1; id <= PokemonService.PokemonLimit; id++)
                {
                    var pokemon = await PokemonCache.GetCachedPokemonById(id);

                    if (pokemon != null)
                    {
                        AllPokemonNames.Add(new PokemonGridItem
                        {
                            Name = PokemonFormatter.FormatPokemonName(pokemon.Name),
                            PokemonId = pokemon.Id
                        });
                    }
                }

                allPokemonCollectionView.ItemsSource = AllPokemonNames;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error Loading Data", $"Failed to load Pokémon: {ex.Message}", "OK");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async void OnSearchBarPressed(object? sender, EventArgs? e)
        {
            string query = searchBar.Text?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrEmpty(query))
            {
                await DisplayAlert("Input Error", "Please enter a Pokémon name or ID.", "OK");
                return;
            }

            try
            {
                Pokemon? pokemon = await PokemonCache.GetCachedPokemon(query);

                if (pokemon != null)
                {
                    bool isFavourite = await PokemonCache.IsFavouriteAsync(query);
                    await DisplayPokemon(pokemon, isFavourite);
                }
                else
                {
                    await DisplayAlert("Error", "Pokémon not found in cache.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            
            searchBar.Text = string.Empty;
        }

        private async Task DisplayPokemon(Pokemon pokemon, bool isFavourite)
        {
            string query = pokemon.Name.ToLower();

            var cachedSpritePath = PokemonCache.GetCachedSprite(query);
            if (!string.IsNullOrEmpty(cachedSpritePath))
            {
                pokemonSprite.Source = cachedSpritePath;
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
            string formattedName = PokemonFormatter.FormatPokemonName(pokemon.Name);
            string types = pokemon.Types != null
                ? string.Join(", ", pokemon.Types.Select(t => PokemonFormatter.FormatPokemonName(t.Type.Name)))
                : "N/A";

            pokemonNameLabel.Text = formattedName;
            pokemonHeightLabel.Text = $"📏 Height: {pokemon.Height / 10.0} m";
            pokemonWeightLabel.Text = $"⚖️ Weight: {pokemon.Weight / 10.0} kg";
            pokemonTypeLabel.Text = $"🏷️ Types: {types}";
            pokemonStatLabel.Text = $"📊 Base Stats: {pokemon.TotalBaseStats}";
            pokemonStrenghtLabel.Text = PokemonFormatter.FormatStrengths(pokemon.Strengths);
            pokemonWeaknessLabel.Text = PokemonFormatter.FormatWeaknesses(pokemon.Weaknesses);

            currentPokemonName = pokemon.Name.ToLower();
            pokemonFavouriteButton.Text = isFavourite ? "★ Favourite" : "☆ Favourite";
            pokemonFavouriteButton.IsVisible = true;
            pokemonAddToTeamButton.IsVisible = teamId.HasValue;
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
            


            var suggestions = AllPokemonNames
                              .Where(p => p.Name.ToLower().StartsWith(query))
                              .Select(p => p.Name) 
                              .Take(10)
                              .ToList();


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
                    pokemonFavouriteButton.Text = "☆ Not Favourite";
                }
                else
                {
                    favourites.Add(currentPokemonName);
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

        private void AllPokemonCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PokemonGridItem selected)
            {
                searchBar.Text = selected.Name.ToLower();
                OnSearchBarPressed(null, null);
                allPokemonCollectionView.SelectedItem = null;
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            allPokemonCollectionView.HeightRequest = height -80;
        }

        public void OnBackButtonClicked(object sender, EventArgs e)
        {
            allPokemonCollectionView.IsVisible = true;
            pokemonDetailsGrid.IsVisible = false;
            ClearPokemonDisplay();
        }

        public async void OnAddTeamButtonClicked(object? sender, EventArgs? e)
        {
            if (string.IsNullOrEmpty(currentPokemonName))
                return;

            if (teamId.HasValue)
            {
              
                await Shell.Current.GoToAsync($"//TeamsRoute/TeamsPage?teamId={teamId.Value}&selectedPokemon={Uri.EscapeDataString(currentPokemonName)}");

                teamId = null;
            }
        }
    }
}
