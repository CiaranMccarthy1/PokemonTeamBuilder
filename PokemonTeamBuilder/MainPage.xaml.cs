using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace PokemonTeamBuilder
{
    [QueryProperty(nameof(TeamId), "teamId")]
    [QueryProperty(nameof(PokemonNameParam), "pokemonName")]
    public partial class MainPage : ContentPage
    {
        private readonly PokemonService pokemonService;
        private string currentPokemonName = string.Empty;
        private Pokemon? currentDisplayedPokemon;
        public ObservableCollection<PokemonGridItem> AllPokemonNames { get; } = new();
        private List<Pokemon> filterPokemonList = new();
        private bool isInitialized = false;
        private bool isLoading = false;
        private int? teamId;
        private bool isSelectingPokemon = false;
        private string? pendingPokemonName;
        private List<string> cachedFavourites = new();
        private bool isViewingFromTeam = false;
        private bool isNavigatingToTeam = false;
        private bool isNavigatingFromTeam = false;


        public MainPage(HttpClient httpClient)
        {
            InitializeComponent();
            pokemonService = new PokemonService(httpClient);
            
            generationFilterPicker.SelectedIndex = 0;
            rarityFilterPicker.SelectedIndex = 0;
            sortPicker.SelectedIndex = 0;
        }

        public string TeamId
        {
            set
            {
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int id))
                {
                    teamId = id;
                    Debug.Log($"MainPage received TeamId: {teamId}");
                }
            }
        }

        public string PokemonNameParam
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    pendingPokemonName = Uri.UnescapeDataString(value);
                    Debug.Log($"MainPage received PokemonName: {pendingPokemonName}");
                }
            }
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            if (teamId.HasValue && !string.IsNullOrEmpty(pendingPokemonName))
            {
                isViewingFromTeam = true;
                backToTeamButton.IsVisible = true;
                
                string pokemonToShow = pendingPokemonName;
                pendingPokemonName = null;
                
                try
                {
                    Pokemon? pokemon = await PokemonCache.GetCachedPokemon(pokemonToShow.ToLower());
                    if (pokemon != null)
                    {
                        bool isFavourite = await PokemonCache.IsFavourite(pokemonToShow.ToLower());
                        await DisplayPokemon(pokemon, isFavourite);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, "Failed to load Pokemon from navigation");
                }
                
                if (!isInitialized)
                {
                    _ = LoadAllPokemonForGrid().ContinueWith(_ => isInitialized = true);
                }
            }
            else
            {
                backToTeamButton.IsVisible = false;
                teamId = null;
                pendingPokemonName = null;
                isViewingFromTeam = false;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!isNavigatingFromTeam)
            {
                pokemonDetailsGrid.IsVisible = false;
            }

            backToTeamButton.IsVisible = teamId.HasValue;
            
            if (!teamId.HasValue && pokemonDetailsGrid.IsVisible)
            {
                isViewingFromTeam = false;
                searchBarFrame.IsVisible = true;
                filterFrame.IsVisible = true;
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

            if (!isNavigatingToTeam)
            {
                teamId = null;
                backToTeamButton.IsVisible = false;
                isViewingFromTeam = false;

                allPokemonCollectionView.IsVisible = true;
                pokemonDetailsGrid.IsVisible = false;
                ClearPokemonDisplay();
            }
            
            isNavigatingToTeam = false;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            pokemonSprite.Source = null;
            pendingPokemonName = null;
            isNavigatingFromTeam = false;
            isViewingFromTeam = false;
            teamId = null;
        }

        private async Task LoadAllPokemonForGrid()
        {
            if (isLoading)
                return;

            isLoading = true;

            try
            {
                filterPokemonList.Clear();
                AllPokemonNames.Clear();
                
                var index = await PokemonCache.GetPokemonIndex();
                
                foreach (var entry in index)
                {
                    filterPokemonList.Add(new Pokemon
                    {
                        Id = entry.Id,
                        Name = entry.Name,
                        Generation = entry.Generation,
                        IsLegendary = entry.IsLegendary,
                        IsMythical = entry.IsMythical,
                        Types = entry.Types.Select(t => new PokemonTypeWrapper 
                        { 
                            Type = new PokemonType { Name = t } 
                        }).ToList(),
                        Stats = entry.Stats.Select(s => new PokemonStat 
                        { 
                            BaseStat = s.Value, 
                            Stat = new StatDetail { Name = s.Key } 
                        }).ToList()
                    });

                    AllPokemonNames.Add(new PokemonGridItem
                    {
                        Name = PokemonFormatter.FormatPokemonName(entry.Name),
                        PokemonId = entry.Id
                    });
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
                    bool isFavourite = await PokemonCache.IsFavourite(query);
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

        private const int MaxBaseStat = 255;


        private void searchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isSelectingPokemon)
                return;

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
            pokemonTypeLabel.Text = $"🏷️ {types}";
            pokemonHeightLabel.Text = $"📏 {pokemon.Height / 10.0} m";
            pokemonWeightLabel.Text = $"⚖️ {pokemon.Weight / 10.0} kg";
            pokemonStrenghtLabel.Text = PokemonFormatter.FormatStrengths(pokemon.Strengths);
            pokemonWeaknessLabel.Text = PokemonFormatter.FormatWeaknesses(pokemon.Weaknesses);

            currentDisplayedPokemon = pokemon;
            levelPicker.SelectedIndex = 4; 
            

            int defaultLevel = 50;
            pokemonStatLabel.Text = $"📊 Total: {pokemon.GetTotalStatsAtLevel(defaultLevel)} (Lv.{defaultLevel})";
            UpdateStatsBarsForLevel(pokemon, defaultLevel);

            BuildAbilitiesList(pokemon);
            BuildMovesList(pokemon);

            currentPokemonName = pokemon.Name.ToLower();
            pokemonFavouriteButton.Text = isFavourite ? "★ Favourite" : "☆ Favourite";
            pokemonFavouriteButton.IsVisible = true;
            pokemonAddToTeamButton.IsVisible = teamId.HasValue && !isViewingFromTeam;
        }

        private void BuildAbilitiesList(Pokemon pokemon)
        {
            abilitiesContainer.Children.Clear();

            if (pokemon.Abilities == null || pokemon.Abilities.Count == 0)
            {
                var noAbilities = new Label
                {
                    Text = "No abilities found",
                    FontSize = 14,
                    TextColor = (Color)Application.Current!.Resources["SubTextColor"]
                };
                abilitiesContainer.Children.Add(noAbilities);
                return;
            }

            var cardBackgroundColor = (Color)Application.Current!.Resources["CardBackgroundColor"];
            var borderColor = (Color)Application.Current!.Resources["BorderColor"];
            var secondaryColor = (Color)Application.Current!.Resources["SecondaryColor"];
            var subTextColor = (Color)Application.Current!.Resources["SubTextColor"];

            foreach (var abilityWrapper in pokemon.Abilities.OrderBy(a => a.Slot))
            {
                var abilityName = abilityWrapper.Ability?.Name?.Replace("-", " ") ?? "Unknown";
                var formattedName = PokemonFormatter.FormatPokemonName(abilityName);

                var abilityFrame = new Frame
                {
                    Padding = new Thickness(16, 12),
                    CornerRadius = 12,
                    HasShadow = false,
                    BackgroundColor = abilityWrapper.IsHidden 
                        ? secondaryColor.WithAlpha(0.3f)
                        : cardBackgroundColor,
                    BorderColor = abilityWrapper.IsHidden 
                        ? secondaryColor
                        : borderColor
                };

                var abilityStack = new VerticalStackLayout { Spacing = 4 };

                var nameRow = new HorizontalStackLayout { Spacing = 8 };
        
                var abilityLabel = new Label
                {
                    Text = formattedName,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold
                };
                nameRow.Children.Add(abilityLabel);

                if (abilityWrapper.IsHidden)
                {
                    var hiddenBadge = new Frame
                    {
                        Padding = new Thickness(6, 2),
                        CornerRadius = 8,
                        HasShadow = false,
                        BackgroundColor = secondaryColor,
                        BorderColor = Colors.Transparent,
                        VerticalOptions = LayoutOptions.Center
                    };
                    hiddenBadge.Content = new Label
                    {
                        Text = "★ Hidden",
                        FontSize = 10,
                        TextColor = Colors.White
                    };
                    nameRow.Children.Add(hiddenBadge);
                }

                abilityStack.Children.Add(nameRow);

                var slotLabel = new Label
                {
                    Text = $"Slot {abilityWrapper.Slot}",
                    FontSize = 12,
                    TextColor = subTextColor
                };
                abilityStack.Children.Add(slotLabel);

                abilityFrame.Content = abilityStack;
                abilitiesContainer.Children.Add(abilityFrame);
            }
        }

        private void BuildMovesList(Pokemon pokemon)
        {
            movesContainer.Children.Clear();

            if (pokemon.Moves == null || pokemon.Moves.Count == 0)
            {
                movesCountLabel.Text = "No moves found";
                var noMoves = new Label
                {
                    Text = "No moves available",
                    FontSize = 14,
                    TextColor = (Color)Application.Current!.Resources["SubTextColor"]
                };
                movesContainer.Children.Add(noMoves);
                return;
            }

            movesCountLabel.Text = $"{pokemon.Moves.Count} moves available";

            var successColor = (Color)Application.Current!.Resources["SuccessColor"];
            var primaryColor = (Color)Application.Current!.Resources["PrimaryColor"];
            var warningColor = (Color)Application.Current!.Resources["WarningColor"];
            var secondaryColor = (Color)Application.Current!.Resources["SecondaryColor"];

            var levelUpMoves = new List<(string Name, int Level)>();
            var tmMoves = new List<string>();
            var eggMoves = new List<string>();
            var tutorMoves = new List<string>();

            foreach (var moveWrapper in pokemon.Moves)
            {
                var moveName = moveWrapper.Move?.Name?.Replace("-", " ") ?? "Unknown";
                var formattedName = PokemonFormatter.FormatPokemonName(moveName);

                var details = moveWrapper.VersionGroupDetails?.LastOrDefault();
                if (details != null)
                {
                    var method = details.MoveLearnMethod?.Name ?? "unknown";
                    switch (method)
                    {
                        case "level-up":
                            levelUpMoves.Add((formattedName, details.LevelLearnedAt));
                            break;
                        case "machine":
                            tmMoves.Add(formattedName);
                            break;
                        case "egg":
                            eggMoves.Add(formattedName);
                            break;
                        case "tutor":
                            tutorMoves.Add(formattedName);
                            break;
                        default:
                            tmMoves.Add(formattedName); 
                            break;
                    }
                }
            }

            if (levelUpMoves.Count > 0)
            {
                AddMoveSectionHeader("📈 Level-Up Moves", levelUpMoves.Count);
                foreach (var move in levelUpMoves.OrderBy(m => m.Level))
                {
                    AddMoveItem(move.Name, $"Lv. {move.Level}", successColor);
                }
            }

            if (tmMoves.Count > 0)
            {
                AddMoveSectionHeader("💿 TM/HM Moves", tmMoves.Count);
                foreach (var move in tmMoves.OrderBy(m => m))
                {
                    AddMoveItem(move, "TM", primaryColor);
                }
            }

            if (eggMoves.Count > 0)
            {
                AddMoveSectionHeader("🥚 Egg Moves", eggMoves.Count);
                foreach (var move in eggMoves.OrderBy(m => m))
                {
                    AddMoveItem(move, "Egg", warningColor);
                }
            }

            if (tutorMoves.Count > 0)
            {
                AddMoveSectionHeader("👨‍🏫 Tutor Moves", tutorMoves.Count);
                foreach (var move in tutorMoves.OrderBy(m => m))
                {
                    AddMoveItem(move, "Tutor", secondaryColor);
                }
            }
        }

        private void AddMoveSectionHeader(string title, int count)
        {
            var header = new Label
            {
                Text = $"{title} ({count})",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 12, 0, 4)
            };
            movesContainer.Children.Add(header);
        }

        private void AddMoveItem(string moveName, string badge, Color badgeColor)
        {
            var frameBackgroundColor = (Color)Application.Current!.Resources["FrameBackgroundColor"];

            var moveRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Padding = new Thickness(8, 6),
                BackgroundColor = frameBackgroundColor
            };

            var nameLabel = new Label
            {
                Text = moveName,
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(nameLabel, 0);

            var badgeFrame = new Frame
            {
                Padding = new Thickness(6, 2),
                CornerRadius = 6,
                HasShadow = false,
                BackgroundColor = badgeColor,
                BorderColor = Colors.Transparent,
                VerticalOptions = LayoutOptions.Center
            };
            badgeFrame.Content = new Label
            {
                Text = badge,
                FontSize = 10,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold
            };
            Grid.SetColumn(badgeFrame, 1);

            moveRow.Children.Add(nameLabel);
            moveRow.Children.Add(badgeFrame);

            movesContainer.Children.Add(moveRow);
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
                Debug.Log($"{currentPokemonName} is now {pokemonFavouriteButton.Text}");
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
            currentDisplayedPokemon = null;

            statsContainer.Children.Clear();
            abilitiesContainer.Children.Clear();
            movesContainer.Children.Clear();
            movesCountLabel.Text = string.Empty;
            
            levelPicker.SelectedIndex = -1;

            Debug.Log("Cleared Pokémon display");
        }

        private void AllPokemonCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PokemonGridItem selected)
            {
                searchBar.Text = selected.Name.ToLower();
                OnSearchBarPressed(null, null);
                allPokemonCollectionView.SelectedItem = null;
                searchBar.Text = string.Empty;
                allPokemonCollectionView.SelectedItem = null;
                isSelectingPokemon = false;
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            Debug.Log($"MainPage size allocated: {width}x{height}");
        }

        public async void OnBackButtonClicked(object sender, EventArgs e)
        {
            if (teamId.HasValue)
            {
                int savedTeamId = teamId.Value;
                isNavigatingToTeam = true;
                teamId = null;
                backToTeamButton.IsVisible = false;
                isViewingFromTeam = false;
                await Shell.Current.GoToAsync($"//TeamsRoute/TeamsPage?teamId={savedTeamId}");
            }
            else
            {
                searchBarFrame.IsVisible = true;
                filterFrame.IsVisible = true;
                allPokemonCollectionView.IsVisible = true;
                pokemonDetailsGrid.IsVisible = false;
                ClearPokemonDisplay();
            }
        }

        private async void OnBackToTeamClicked(object sender, EventArgs e)
        {
            if (teamId.HasValue)
            {
                int savedTeamId = teamId.Value;
                isNavigatingToTeam = true;
                teamId = null;
                backToTeamButton.IsVisible = false;
                isViewingFromTeam = false;
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
                pendingPokemonName = null;

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
            favouritesCheckBox.IsChecked = false;

            generationFilterPicker.SelectedIndex = 0;
            rarityFilterPicker.SelectedIndex = 0;
            sortPicker.SelectedIndex = 0;

            minTbsEntry.Text = string.Empty;
            maxTbsEntry.Text = string.Empty;

            typeFilterDropdown.IsVisible = false;
            typeFilterButton.Text = "Types ▼";

            ApplyFilters();
        }

        private async void OnFavouritesFilterChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                var favourites = await PokemonCache.GetFavorites();
                cachedFavourites = favourites.Distinct().ToList();
            }
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

            if (favouritesCheckBox.IsChecked)
            {
                filtered = filtered.Where(p => cachedFavourites.Contains(p.Name.ToLower()));
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

        private void OnLevelPickerChanged(object sender, EventArgs e)
        {
            if (currentDisplayedPokemon == null || levelPicker.SelectedItem == null)
                return;

            if (int.TryParse(levelPicker.SelectedItem.ToString(), out int level))
            {
                pokemonStatLabel.Text = $"📊 Total: {currentDisplayedPokemon.GetTotalStatsAtLevel(level)} (Lv.{level})";
                UpdateStatsBarsForLevel(currentDisplayedPokemon, level);
            }
        }
        private void UpdateStatsBarsForLevel(Pokemon pokemon, int level)
        {
            statsContainer.Children.Clear();

            if (pokemon.Stats == null || pokemon.Stats.Count == 0)
                return;

            var statNames = new Dictionary<string, string>
            {
                { "hp", "HP" },
                { "attack", "Attack" },
                { "defense", "Defense" },
                { "special-attack", "Sp. Atk" },
                { "special-defense", "Sp. Def" },
                { "speed", "Speed" }
            };


            int maxStat = (int)(level * 4 + 10);
            const double maxBarWidth = 100;


            var successColor = (Color)Application.Current!.Resources["SuccessColor"];
            var errorColor = (Color)Application.Current!.Resources["ErrorColor"];
            var surfaceColor = (Color)Application.Current!.Resources["SurfaceColor"];

            foreach (var stat in pokemon.Stats)
            {
                var statName = stat.Stat?.Name ?? "Unknown";
                var displayName = statNames.TryGetValue(statName, out var name) ? name : statName;

                int value = pokemon.GetStatAtLevel(statName, level);
                var percentage = Math.Min((float)value / maxStat, 1f);
                var isAboveHalf = percentage >= 0.5f;

                var statRow = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(70) },
                        new ColumnDefinition { Width = new GridLength(45) },
                        new ColumnDefinition { Width = new GridLength(maxBarWidth) }
                    },
                    ColumnSpacing = 8,
                    HorizontalOptions = LayoutOptions.Center
                };

                var nameLabel = new Label
                {
                    Text = displayName,
                    FontSize = 13,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(nameLabel, 0);

                var valueLabel = new Label
                {
                    Text = value.ToString(),
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.End,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(valueLabel, 1);

                var barBackground = new BoxView
                {
                    HeightRequest = 12,
                    CornerRadius = 6,
                    Color = surfaceColor,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Center
                };

                var barFill = new BoxView
                {
                    HeightRequest = 12,
                    CornerRadius = 6,
                    Color = isAboveHalf ? successColor : errorColor,
                    HorizontalOptions = LayoutOptions.Start,
                    WidthRequest = 0
                };

                var barContainer = new Grid();
                barContainer.Children.Add(barBackground);
                barContainer.Children.Add(barFill);
                Grid.SetColumn(barContainer, 2);

                statRow.Children.Add(nameLabel);
                statRow.Children.Add(valueLabel);
                statRow.Children.Add(barContainer);

                statsContainer.Children.Add(statRow);

                var targetWidth = percentage * maxBarWidth;
                barFill.Animate("FillBar",
                    new Animation(v => barFill.WidthRequest = v, 0, targetWidth),
                    length: 3000,
                    easing: Easing.CubicOut);
            }
        }
    }
}
