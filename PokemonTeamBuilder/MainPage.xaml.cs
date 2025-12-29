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
        private List<Pokemon> filterPokemonList = new();
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
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int id))
                {
                    teamId = id;
                }
                else
                {
                    teamId = null;
                }
            }
        }

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            backToTeamButton.IsVisible = teamId.HasValue;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            

            backToTeamButton.IsVisible = teamId.HasValue;
            

            if (!teamId.HasValue && pokemonDetailsGrid.IsVisible)
            {
                allPokemonCollectionView.IsVisible = true;
                pokemonDetailsGrid.IsVisible = false;
                ClearPokemonDisplay();
            }
            
            if (!isInitialized)
            {
                await LoadAllPokemonForGrid();
                isInitialized = true;
            }
        }

        protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);

            teamId = null;
            backToTeamButton.IsVisible = false;
            
            allPokemonCollectionView.IsVisible = true;
            pokemonDetailsGrid.IsVisible = false;
            ClearPokemonDisplay();
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
                        filterPokemonList.Add(pokemon);

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

            // Hide search bar, filters, and grid
            searchBarFrame.IsVisible = false;
            filterFrame.IsVisible = false;
            typeFilterDropdown.IsVisible = false;
            allPokemonCollectionView.IsVisible = false;
            pokemonDetailsGrid.IsVisible = true;

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
            pokemonAddToTeamButton.IsVisible = false;
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
        }

        public void OnBackButtonClicked(object sender, EventArgs e)
        {
            // Show search bar, filters, and grid
            searchBarFrame.IsVisible = true;
            filterFrame.IsVisible = true;
            allPokemonCollectionView.IsVisible = true;
            pokemonDetailsGrid.IsVisible = false;
            // Keep backToTeamButton visible if teamId is set (don't hide it when going back to grid)
            ClearPokemonDisplay();
        }

        private async void OnBackToTeamClicked(object sender, EventArgs e)
        {
            if (teamId.HasValue)
            {
                int savedTeamId = teamId.Value;
                teamId = null;
                backToTeamButton.IsVisible = false;
                await Shell.Current.GoToAsync($"//TeamsRoute/TeamsPage?teamId={savedTeamId}");
            }
        }

        public async void OnAddTeamButtonClicked(object? sender, EventArgs? e)
        {
            if (string.IsNullOrEmpty(currentPokemonName))
                return;

            if (teamId.HasValue)
            {
                int savedTeamId = teamId.Value;
                teamId = null;
                backToTeamButton.IsVisible = false;
                pokemonAddToTeamButton.IsVisible = false;
                
                await Shell.Current.GoToAsync($"//TeamsRoute/TeamsPage?teamId={savedTeamId}&selectedPokemon={Uri.EscapeDataString(currentPokemonName)}");
            }
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void OnTypeFilterChanged(object sender, CheckedChangedEventArgs e)
        {
            UpdateTypeFilterButtonText();
            ApplyFilters();
        }

        private void OnTypeFilterButtonClicked(object sender, EventArgs e)
        {
            typeFilterDropdown.IsVisible = !typeFilterDropdown.IsVisible;
            typeFilterButton.Text = typeFilterDropdown.IsVisible ? "Types ▲" : "Types ▼";
        }

        private void OnTypeFilterDoneClicked(object sender, EventArgs e)
        {
            typeFilterDropdown.IsVisible = false;
            typeFilterButton.Text = "Types ▼";
        }

        private void UpdateTypeFilterButtonText()
        {
            var selectedTypes = GetSelectedTypes();
            if (selectedTypes.Count == 0)
            {
                typeFilterButton.Text = typeFilterDropdown.IsVisible ? "Types ▲" : "Types ▼";
            }
            else if (selectedTypes.Count <= 2)
            {
                string types = string.Join(", ", selectedTypes.Select(t => char.ToUpper(t[0]) + t.Substring(1)));
                typeFilterButton.Text = types + (typeFilterDropdown.IsVisible ? " ▲" : " ▼");
            }
            else
            {
                typeFilterButton.Text = $"{selectedTypes.Count} types" + (typeFilterDropdown.IsVisible ? " ▲" : " ▼");
            }
        }

        private void OnClearFiltersClicked(object sender, EventArgs e)
        {
            fireCheckBox.IsChecked = false;
            waterCheckBox.IsChecked = false;
            grassCheckBox.IsChecked = false;
            electricCheckBox.IsChecked = false;
            psychicCheckBox.IsChecked = false;
            iceCheckBox.IsChecked = false;
            dragonCheckBox.IsChecked = false;
            darkCheckBox.IsChecked = false;
            fairyCheckBox.IsChecked = false;
            normalCheckBox.IsChecked = false;
            fightingCheckBox.IsChecked = false;
            flyingCheckBox.IsChecked = false;
            poisonCheckBox.IsChecked = false;
            groundCheckBox.IsChecked = false;
            rockCheckBox.IsChecked = false;
            bugCheckBox.IsChecked = false;
            ghostCheckBox.IsChecked = false;
            steelCheckBox.IsChecked = false;

            generationFilterPicker.SelectedIndex = -1;
            rarityFilterPicker.SelectedIndex = -1;
            sortPicker.SelectedIndex = -1;

            minTbsEntry.Text = string.Empty;
            maxTbsEntry.Text = string.Empty;

            typeFilterDropdown.IsVisible = false;
            typeFilterButton.Text = "Types ▼";

            ApplyFilters();
        }

        private void OnSortChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private List<string> GetSelectedTypes()
        {
            var selectedTypes = new List<string>();

            if (fireCheckBox.IsChecked) selectedTypes.Add("fire");
            if (waterCheckBox.IsChecked) selectedTypes.Add("water");
            if (grassCheckBox.IsChecked) selectedTypes.Add("grass");
            if (electricCheckBox.IsChecked) selectedTypes.Add("electric");
            if (psychicCheckBox.IsChecked) selectedTypes.Add("psychic");
            if (iceCheckBox.IsChecked) selectedTypes.Add("ice");
            if (dragonCheckBox.IsChecked) selectedTypes.Add("dragon");
            if (darkCheckBox.IsChecked) selectedTypes.Add("dark");
            if (fairyCheckBox.IsChecked) selectedTypes.Add("fairy");
            if (normalCheckBox.IsChecked) selectedTypes.Add("normal");
            if (fightingCheckBox.IsChecked) selectedTypes.Add("fighting");
            if (flyingCheckBox.IsChecked) selectedTypes.Add("flying");
            if (poisonCheckBox.IsChecked) selectedTypes.Add("poison");
            if (groundCheckBox.IsChecked) selectedTypes.Add("ground");
            if (rockCheckBox.IsChecked) selectedTypes.Add("rock");
            if (bugCheckBox.IsChecked) selectedTypes.Add("bug");
            if (ghostCheckBox.IsChecked) selectedTypes.Add("ghost");
            if (steelCheckBox.IsChecked) selectedTypes.Add("steel");

            return selectedTypes;
        }

        private void ApplyFilters()
        {
            var filtered = filterPokemonList.AsEnumerable();

            var selectedTypes = GetSelectedTypes();
            if (selectedTypes.Count > 0)
            {
                filtered = filtered.Where(p =>
                    p.Types != null && 
                    selectedTypes.All(selectedType => 
                        p.Types.Any(t => t.Type.Name.ToLower() == selectedType)));
            }

            if (int.TryParse(minTbsEntry.Text, out int minTbs))
            {
                filtered = filtered.Where(p => p.TotalBaseStats >= minTbs);
            }

            if (int.TryParse(maxTbsEntry.Text, out int maxTbs))
            {
                filtered = filtered.Where(p => p.TotalBaseStats <= maxTbs);
            }

            string selectedGen = generationFilterPicker.SelectedItem?.ToString() ?? "All";
            if (selectedGen != "All")
            {
                if (int.TryParse(selectedGen.Replace("Gen ", ""), out int gen))
                {
                    filtered = filtered.Where(p => p.Generation == gen);
                }
            }

            string rarity = rarityFilterPicker.SelectedItem?.ToString() ?? "All";
            filtered = rarity switch
            {
                "Legendary" => filtered.Where(p => p.IsLegendary),
                "Mythical" => filtered.Where(p => p.IsMythical),
                "Regular" => filtered.Where(p => !p.IsLegendary && !p.IsMythical),
                _ => filtered
            };

            // Apply sorting
            string sortOption = sortPicker.SelectedItem?.ToString() ?? "Dex # ↑";
            filtered = sortOption switch
            {
                "Dex # ↑" => filtered.OrderBy(p => p.Id),
                "Dex # ↓" => filtered.OrderByDescending(p => p.Id),
                "A-Z" => filtered.OrderBy(p => p.Name),
                "Z-A" => filtered.OrderByDescending(p => p.Name),
                "BST ↑" => filtered.OrderBy(p => p.TotalBaseStats),
                "BST ↓" => filtered.OrderByDescending(p => p.TotalBaseStats),
                "HP ↑" => filtered.OrderBy(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "hp")?.BaseStat ?? 0),
                "HP ↓" => filtered.OrderByDescending(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "hp")?.BaseStat ?? 0),
                "Attack ↑" => filtered.OrderBy(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "attack")?.BaseStat ?? 0),
                "Attack ↓" => filtered.OrderByDescending(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "attack")?.BaseStat ?? 0),
                "Defense ↑" => filtered.OrderBy(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "defense")?.BaseStat ?? 0),
                "Defense ↓" => filtered.OrderByDescending(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "defense")?.BaseStat ?? 0),
                "Sp. Atk ↑" => filtered.OrderBy(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "special-attack")?.BaseStat ?? 0),
                "Sp. Atk ↓" => filtered.OrderByDescending(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "special-attack")?.BaseStat ?? 0),
                "Sp. Def ↑" => filtered.OrderBy(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "special-defense")?.BaseStat ?? 0),
                "Sp. Def ↓" => filtered.OrderByDescending(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "special-defense")?.BaseStat ?? 0),
                "Speed ↑" => filtered.OrderBy(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "speed")?.BaseStat ?? 0),
                "Speed ↓" => filtered.OrderByDescending(p => p.Stats?.FirstOrDefault(s => s.Stat.Name == "speed")?.BaseStat ?? 0),
                _ => filtered.OrderBy(p => p.Id)
            };

            AllPokemonNames.Clear();
            foreach (var pokemon in filtered)
            {
                AllPokemonNames.Add(new PokemonGridItem
                {
                    Name = PokemonFormatter.FormatPokemonName(pokemon.Name),
                    PokemonId = pokemon.Id
                });
            }
        }
    }
}
