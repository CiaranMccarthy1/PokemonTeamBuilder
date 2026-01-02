using Microsoft.Maui.Controls.Shapes;

namespace PokemonTeamBuilder;

public partial class PokemonCompare : ContentPage
{
    private readonly List<Pokemon> selectedPokemon = new();
    private readonly List<string> allPokemonNames = new();
    private const int MaxPokemon = 4;
    private const int MinPokemon = 2;

    private readonly Color[] pokemonColors = new[]
    {
        Colors.Red,
        Colors.Blue,
        Colors.Green,
        Colors.Yellow
    };
    private Color GetPokemonColor(int index)
    {
        return pokemonColors[index % pokemonColors.Length];
    }

    public PokemonCompare()
    {
        InitializeComponent();
        LoadPokemonNames();
    }

    private async void LoadPokemonNames()
    {
        try
        {
            var index = await PokemonCache.GetPokemonIndex();
            allPokemonNames.Clear();
            foreach (var entry in index)
            {
                allPokemonNames.Add(entry.Name.ToLower());
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, "Failed to load Pokemon names for compare");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (isAddingPokemon)
            return;

        string query = e.NewTextValue?.ToLower() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            suggestionsFrame.IsVisible = false;
            suggestionListView.ItemsSource = null;
            return;
        }

        var suggestions = allPokemonNames
            .Where(p => p.StartsWith(query))
            .Where(p => !selectedPokemon.Any(s => s.Name.ToLower() == p))
            .Take(8)
            .Select(p => PokemonFormatter.FormatPokemonName(p))
            .ToList();

        if (suggestions.Count > 0)
        {
            suggestionListView.ItemsSource = suggestions;
            suggestionsFrame.IsVisible = true;
        }
        else
        {
            suggestionsFrame.IsVisible = false;
        }
    }

    private async void OnSuggestionSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is not string pokemonName)
            return;

        suggestionListView.SelectedItem = null;
        suggestionsFrame.IsVisible = false;

        isAddingPokemon = true;
        searchBar.Text = string.Empty;  
        isAddingPokemon = false;

        await AddPokemon(pokemonName.ToLower());
    }

    private async void OnAddPokemonClicked(object sender, EventArgs e)
    {
        string query = searchBar.Text?.Trim().ToLower() ?? string.Empty;

        if (string.IsNullOrEmpty(query))
            return;

        isAddingPokemon = true;
        searchBar.Text = string.Empty;
        suggestionsFrame.IsVisible = false;
        isAddingPokemon = false;

        await AddPokemon(query);
    }

    private bool isAddingPokemon = false;
    private async Task AddPokemon(string name)
    {
        if (selectedPokemon.Count >= MaxPokemon)
        {
            await DisplayAlert("Limit Reached", $"You can only compare up to {MaxPokemon} Pokémon.", "OK");
            return;
        }

        if (selectedPokemon.Any(p => p.Name.ToLower() == name))
        {
            await DisplayAlert("Already Added", "This Pokémon is already in the comparison.", "OK");
            return;
        }

        try
        {
            isAddingPokemon = true;
            var pokemon = await PokemonCache.GetCachedPokemon(name);
            if (pokemon != null)
            {
                selectedPokemon.Add(pokemon);
                suggestionsFrame.IsVisible = false;
                UpdateDisplay();
            }
            else
            {
                await DisplayAlert("Not Found", "Pokémon not found in cache.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load Pokémon: {ex.Message}", "OK");
        }
        finally
        {
            isAddingPokemon = false;
        }
    }

    private void OnClearAllClicked(object sender, EventArgs e)
    {
        selectedPokemon.Clear();
        UpdateDisplay();
    }

    private void RemovePokemon(Pokemon pokemon)
    {
        selectedPokemon.Remove(pokemon);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        UpdateSelectedPokemonCards();
        UpdateComparison();
    }

    private void UpdateSelectedPokemonCards()
    {
        selectedPokemonContainer.Children.Clear();

        if (selectedPokemon.Count == 0)
        {
            selectionHintLabel.IsVisible = true;
            selectionHintLabel.Text = "Add 2-4 Pokémon to compare";
            return;
        }

        selectionHintLabel.IsVisible = selectedPokemon.Count < MinPokemon;
        if (selectedPokemon.Count < MinPokemon)
        {
            selectionHintLabel.Text = $"Add {MinPokemon - selectedPokemon.Count} more Pokémon to compare";
        }

        for (int i = 0; i < selectedPokemon.Count; i++)
        {
            var pokemon = selectedPokemon[i];
            var color = GetPokemonColor(i);

            var card = CreatePokemonCard(pokemon, color, i);
            selectedPokemonContainer.Children.Add(card);
        }
    }

    private Border CreatePokemonCard(Pokemon pokemon, Color accentColor, int index)
    {
        var border = new Border
        {
            Padding = 12,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Stroke = accentColor,
            StrokeThickness = 2,
        };

        var stack = new VerticalStackLayout { Spacing = 8 };

        var removeButton = new Button
        {
            Text = "×",
            FontSize = 14,
            WidthRequest = 24,
            HeightRequest = 24,
            CornerRadius = 12,
            Padding = 0,
            BackgroundColor = Colors.Red,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.End
        };
        var pokemonToRemove = pokemon;
        removeButton.Clicked += (s, e) => RemovePokemon(pokemonToRemove);
        removeButton.SetDynamicResource(Button.BackgroundColorProperty, "ErrorColor");

        var spriteFrame = new Frame
        {
            WidthRequest = 70,
            HeightRequest = 70,
            CornerRadius = 35,
            Padding = 5,
            HasShadow = false,
            BackgroundColor = accentColor.WithAlpha(0.2f),
            BorderColor = accentColor,
            HorizontalOptions = LayoutOptions.Center
        };

        var sprite = new Image
        {
            Source = PokemonCache.GetCachedSprite(pokemon.Name.ToLower()),
            WidthRequest = 60,
            HeightRequest = 60,
            Aspect = Aspect.AspectFit
        };
        spriteFrame.Content = sprite;

        var nameLabel = new Label
        {
            Text = PokemonFormatter.FormatPokemonName(pokemon.Name),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var typesLabel = new Label
        {
            Text = pokemon.Types != null
                ? string.Join("/", pokemon.Types.Select(t => PokemonFormatter.FormatPokemonName(t.Type.Name)))
                : "N/A",
            FontSize = 10,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };
        typesLabel.SetDynamicResource(Label.TextColorProperty, "SubTextColor");

        stack.Children.Add(removeButton);
        stack.Children.Add(spriteFrame);
        stack.Children.Add(nameLabel);
        stack.Children.Add(typesLabel);

        border.Content = stack;
        return border;
    }

    private void UpdateComparison()
    {
        bool canCompare = selectedPokemon.Count >= MinPokemon;
        comparisonFrame.IsVisible = canCompare;
        typeComparisonFrame.IsVisible = canCompare;

        if (!canCompare)
            return;

        UpdateLegend();
        UpdateStatsComparison();
        UpdateTotalStats();
        UpdateTypeComparison();
    }

    private void UpdateLegend()
    {
        legendContainer.Children.Clear();

        for (int i = 0; i < selectedPokemon.Count; i++)
        {
            var pokemon = selectedPokemon[i];
            var color = GetPokemonColor(i);

            var legendItem = new HorizontalStackLayout { Spacing = 6 };

            var colorBox = new BoxView
            {
                WidthRequest = 16,
                HeightRequest = 16,
                CornerRadius = 4,
                Color = color
            };

            var nameLabel = new Label
            {
                Text = PokemonFormatter.FormatPokemonName(pokemon.Name),
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
            };

            legendItem.Children.Add(colorBox);
            legendItem.Children.Add(nameLabel);
            legendContainer.Children.Add(legendItem);
        }
    }

    private void UpdateStatsComparison()
    {
        statsComparisonContainer.Children.Clear();

        var statNames = new[] { "hp", "attack", "defense", "special-attack", "special-defense", "speed" };
        var statDisplayNames = new Dictionary<string, string>
        {
            { "hp", "HP" },
            { "attack", "Attack" },
            { "defense", "Defense" },
            { "special-attack", "Sp. Atk" },
            { "special-defense", "Sp. Def" },
            { "speed", "Speed" }
        };

        int maxStatValue = 255;

        foreach (var statName in statNames)
        {
            var statSection = new VerticalStackLayout { Spacing = 6 };

            var headerLabel = new Label
            {
                Text = statDisplayNames[statName],
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            };
            statSection.Children.Add(headerLabel);

            for (int i = 0; i < selectedPokemon.Count; i++)
            {
                var pokemon = selectedPokemon[i];
                var color = GetPokemonColor(i);

                var stat = pokemon.Stats?.FirstOrDefault(s => s.Stat?.Name == statName);
                int statValue = stat?.BaseStat ?? 0;
                double percentage = (double)statValue / maxStatValue;

                var barRow = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(80) },
                        new ColumnDefinition { Width = new GridLength(40) },
                        new ColumnDefinition { Width = GridLength.Star }
                    },
                    ColumnSpacing = 8
                };

                var nameLabel = new Label
                {
                    Text = PokemonFormatter.FormatPokemonName(pokemon.Name),
                    FontSize = 11,
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                Grid.SetColumn(nameLabel, 0);

                var valueLabel = new Label
                {
                    Text = statValue.ToString(),
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.End,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(valueLabel, 1);

                var barBackground = new BoxView
                {
                    HeightRequest = 14,
                    CornerRadius = 7,
                    HorizontalOptions = LayoutOptions.Fill
                };
                barBackground.SetDynamicResource(BoxView.ColorProperty, "SurfaceColor");

                var barFill = new BoxView
                {
                    HeightRequest = 14,
                    CornerRadius = 7,
                    Color = color,
                    HorizontalOptions = LayoutOptions.Start,
                    WidthRequest = 0
                };

                var barContainer = new Grid();
                barContainer.Children.Add(barBackground);
                barContainer.Children.Add(barFill);
                Grid.SetColumn(barContainer, 2);

                barRow.Children.Add(nameLabel);
                barRow.Children.Add(valueLabel);
                barRow.Children.Add(barContainer);

                statSection.Children.Add(barRow);

                double targetWidth = percentage * 200;
                barFill.Animate("FillBar",
                    new Animation(v => barFill.WidthRequest = v, 0, targetWidth),
                    length: 400,
                    easing: Easing.CubicOut);
            }

            statsComparisonContainer.Children.Add(statSection);
        }
    }

    private void UpdateTotalStats()
    {
        totalStatsContainer.Children.Clear();

        var headerLabel = new Label
        {
            Text = "Base Stat Total",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold
        };
        totalStatsContainer.Children.Add(headerLabel);

        var sortedPokemon = selectedPokemon
            .Select((p, i) => new { Pokemon = p, Index = i, Total = p.TotalBaseStats })
            .OrderByDescending(x => x.Total)
            .ToList();

        int maxTotal = sortedPokemon.Max(x => x.Total);

        foreach (var item in sortedPokemon)
        {
            var color = GetPokemonColor(item.Index);
            double percentage = (double)item.Total / maxTotal;

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(100) },
                    new ColumnDefinition { Width = new GridLength(50) },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 8
            };

            var nameLabel = new Label
            {
                Text = PokemonFormatter.FormatPokemonName(item.Pokemon.Name),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(nameLabel, 0);

            var valueLabel = new Label
            {
                Text = item.Total.ToString(),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.End,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(valueLabel, 1);

            var barBackground = new BoxView
            {
                HeightRequest = 20,
                CornerRadius = 10,
                HorizontalOptions = LayoutOptions.Fill
            };
            barBackground.SetDynamicResource(BoxView.ColorProperty, "SurfaceColor");

            var barFill = new BoxView
            {
                HeightRequest = 20,
                CornerRadius = 10,
                Color = color,
                HorizontalOptions = LayoutOptions.Start,
                WidthRequest = 0
            };

            var barContainer = new Grid();
            barContainer.Children.Add(barBackground);
            barContainer.Children.Add(barFill);
            Grid.SetColumn(barContainer, 2);

            row.Children.Add(nameLabel);
            row.Children.Add(valueLabel);
            row.Children.Add(barContainer);

            totalStatsContainer.Children.Add(row);

            double targetWidth = percentage * 200;
            barFill.Animate("FillTotal",
                new Animation(v => barFill.WidthRequest = v, 0, targetWidth),
                length: 500,
                easing: Easing.CubicOut);
        }
    }

    private void UpdateTypeComparison()
    {
        typeComparisonGrid.Children.Clear();
        typeComparisonGrid.ColumnDefinitions.Clear();
        typeComparisonGrid.RowDefinitions.Clear();

        typeComparisonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        foreach (var _ in selectedPokemon)
        {
            typeComparisonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        typeComparisonGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        typeComparisonGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        typeComparisonGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var headerLabel = new Label { Text = "", FontAttributes = FontAttributes.Bold };
        Grid.SetRow(headerLabel, 0);
        Grid.SetColumn(headerLabel, 0);
        typeComparisonGrid.Children.Add(headerLabel);

        for (int i = 0; i < selectedPokemon.Count; i++)
        {
            var pokemon = selectedPokemon[i];
            var label = new Label
            {
                Text = PokemonFormatter.FormatPokemonName(pokemon.Name),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            };
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, i + 1);
            typeComparisonGrid.Children.Add(label);
        }

        var typesLabel = new Label { Text = "Types", FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };
        Grid.SetRow(typesLabel, 1);
        Grid.SetColumn(typesLabel, 0);
        typeComparisonGrid.Children.Add(typesLabel);

        for (int i = 0; i < selectedPokemon.Count; i++)
        {
            var pokemon = selectedPokemon[i];
            var types = pokemon.Types != null
                ? string.Join("\n", pokemon.Types.Select(t => PokemonFormatter.FormatPokemonName(t.Type.Name)))
                : "N/A";
            var label = new Label
            {
                Text = types,
                FontSize = 11,
                HorizontalTextAlignment = TextAlignment.Center
            };
            Grid.SetRow(label, 1);
            Grid.SetColumn(label, i + 1);
            typeComparisonGrid.Children.Add(label);
        }

        var weaknessLabel = new Label { Text = "Weaknesses", FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Start };
        weaknessLabel.TextColor = Colors.Red;
        Grid.SetRow(weaknessLabel, 2);
        Grid.SetColumn(weaknessLabel, 0);
        typeComparisonGrid.Children.Add(weaknessLabel);

        for (int i = 0; i < selectedPokemon.Count; i++)
        {
            var pokemon = selectedPokemon[i];
            var weaknesses = pokemon.Weaknesses?.Take(4).Select(w => PokemonFormatter.FormatPokemonName(w.Type)) ?? new List<string>();
            var label = new Label
            {
                Text = string.Join("\n", weaknesses),
                FontSize = 10,
                HorizontalTextAlignment = TextAlignment.Center
            };
            label.SetDynamicResource(Label.TextColorProperty, "ErrorColor");
            Grid.SetRow(label, 2);
            Grid.SetColumn(label, i + 1);
            typeComparisonGrid.Children.Add(label);
        }
    }
}