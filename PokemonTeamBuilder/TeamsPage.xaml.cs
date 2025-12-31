using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;

namespace PokemonTeamBuilder;
[QueryProperty(nameof(SelectedPokemon), "selectedPokemon")]
[QueryProperty(nameof(TeamIdParam), "teamId")]
public partial class TeamsPage : ContentPage
{
    private ObservableCollection<PokemonTeam> teams = new();
    private string teamsFolder = string.Empty;
    private PokemonTeam? currentTeam;
    private string selectedPokemon = string.Empty;
    private int? pendingTeamId;

    public string SelectedPokemon
    {
        set => selectedPokemon = Uri.UnescapeDataString(value ?? string.Empty);
    }

    public string TeamIdParam
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                pendingTeamId = id;
            }
        }
    }

    public TeamsPage()
    {
        InitializeComponent();
        InitializeTeamsFolder();
        LoadTeams();
        DisplayTeams();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadTeams();

        if (!teamDetailView.IsVisible)
        {
            DisplayTeams();
        }
        

        if (!string.IsNullOrEmpty(selectedPokemon) && pendingTeamId.HasValue)
        {
            var team = teams.FirstOrDefault(t => t.Id == pendingTeamId.Value);
            if (team != null)
            {
                currentTeam = team;

                if (team.PokemonCount < 6)
                {
                    await AddPokemonToTeam(selectedPokemon);
                    DisplayTeams();
                }

                await LoadTeamDetail(team);
            }

            selectedPokemon = string.Empty;
            pendingTeamId = null;
        }
        else if (currentTeam != null && teamDetailView.IsVisible)
        {
            var reloadedTeam = teams.FirstOrDefault(t => t.Id == currentTeam.Id);
            if (reloadedTeam != null)
            {
                currentTeam = reloadedTeam;
                await LoadTeamDetail(currentTeam);
            }
        }
    }

    private void InitializeTeamsFolder()
    {
        teamsFolder = System.IO.Path.Combine(FileSystem.AppDataDirectory, "teams");

        if (!Directory.Exists(teamsFolder))
        {
            Directory.CreateDirectory(teamsFolder);
        }
    }

    private void LoadTeams()
    {
        teams = new ObservableCollection<PokemonTeam>();

        try
        {
            var teamFiles = Directory.GetFiles(teamsFolder, "*.json");

            foreach (var file in teamFiles)
            {
                var json = File.ReadAllText(file);
                var team = JsonSerializer.Deserialize<PokemonTeam>(json);

                if (team != null)
                {
                    teams.Add(team);
                }
            }
            teams = new ObservableCollection<PokemonTeam>(teams.OrderBy(t => t.Id));
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to load teams: {ex.Message}", "OK");
        }
    }

    private void DisplayTeams()
    {
        teamGrid.Children.Clear();

        if (teams.Count == 0)
        {
            emptyTeamsMessage.IsVisible = true;
            floatingAddButton.IsVisible = false;
            return;
        }

        emptyTeamsMessage.IsVisible = false;
        floatingAddButton.IsVisible = true;

        int row = 0;
        int col = 0;

        foreach (var team in teams)
        {
            var teamCard = CreateTeamCard(team);

            Grid.SetRow(teamCard, row);
            Grid.SetColumn(teamCard, col);
            teamGrid.Children.Add(teamCard);

            col++;
            if (col >= 3)
            {
                col = 0;
                row++;
            }
        }
    }

    private Border CreateTeamCard(PokemonTeam team)
    {
        var border = new Border
        {
            BackgroundColor = (Color)Application.Current.Resources["CardBackgroundColor"],
            Stroke = (Color)Application.Current.Resources["BorderColor"],
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(16)
            },
            Padding = 16,
            HeightRequest = 220
        };

        var mainLayout = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            RowSpacing = 12
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var nameLabel = new Label
        {
            Text = team.Name,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center
        };
        headerGrid.Children.Add(nameLabel);

        var deleteButton = new Button
        {
            Text = "×",
            FontSize = 18,
            WidthRequest = 32,
            HeightRequest = 32,
            CornerRadius = 16,
            Padding = 0,
            BackgroundColor = (Color)Application.Current.Resources["ErrorColor"],
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start
        };
        deleteButton.Clicked += async (s, e) =>
        {
            bool delete = await DisplayAlert("Delete Team", $"Delete {team.Name}?", "Yes", "No");
            if (delete)
            {
                DeleteTeam(team);
                LoadTeams();
                DisplayTeams();
            }
        };
        Grid.SetColumn(deleteButton, 1);
        headerGrid.Children.Add(deleteButton);

        Grid.SetRow(headerGrid, 0);
        mainLayout.Children.Add(headerGrid);

        var pokemonGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(60) },
                new ColumnDefinition { Width = new GridLength(60) },
                new ColumnDefinition { Width = new GridLength(60) }
            },
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = new GridLength(60) },
                new RowDefinition { Height = new GridLength(60) }
            },
            ColumnSpacing = 10,
            RowSpacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };

        for (int i = 0; i < 6; i++)
        {
            if (team.Pokemon != null && i < team.Pokemon.Count)
            {
                var spriteSource = team.Pokemon[i].SpriteUrl;
                if (string.IsNullOrEmpty(spriteSource) || !File.Exists(spriteSource))
                {
                    spriteSource = PokemonCache.GetCachedSprite(team.Pokemon[i].Name.ToLower());
                }

                if (!string.IsNullOrEmpty(spriteSource))
                {
                    var image = new Image
                    {
                        Source = spriteSource,
                        WidthRequest = 50,
                        HeightRequest = 50,
                        Aspect = Aspect.AspectFit
                    };
                    Grid.SetRow(image, i / 3);
                    Grid.SetColumn(image, i % 3);
                    pokemonGrid.Children.Add(image);
                }
                else
                {
                    var box = new BoxView
                    {
                        WidthRequest = 50,
                        HeightRequest = 50,
                        CornerRadius = 5
                    };
                    box.SetDynamicResource(BoxView.ColorProperty, "SurfaceColor");
                    Grid.SetRow(box, i / 3);
                    Grid.SetColumn(box, i % 3);
                    pokemonGrid.Children.Add(box);
                }
            }
            else
            {
                var box = new BoxView
                {
                    WidthRequest = 50,
                    HeightRequest = 50,
                    CornerRadius = 5
                };
                box.SetDynamicResource(BoxView.ColorProperty, "SurfaceColor");
                Grid.SetRow(box, i / 3);
                Grid.SetColumn(box, i % 3);
                pokemonGrid.Children.Add(box);
            }
        }

        Grid.SetRow(pokemonGrid, 1);
        mainLayout.Children.Add(pokemonGrid);

        border.Content = mainLayout;

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => OnTeamCardTapped(team);
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    private async void OnTeamCardTapped(PokemonTeam team)
    {
        await LoadTeamDetail(team);
    }

    private async Task LoadTeamDetail(PokemonTeam team)
    {
        try
        {
            currentTeam = team;

            teamsListView.IsVisible = false;
            teamDetailView.IsVisible = true;

            teamNameLabel.Text = team.Name;

            teamMembersContainer.Children.Clear();

            if (team.Pokemon == null || team.Pokemon.Count == 0)
            {
                emptyTeamMessage.IsVisible = true;

                totalScoreLabel.Text = "Total Score: N/A";
                teamWeaknessesLabel.Text = "Team Weaknesses: N/A";
                teamStrengthsLabel.Text = "Team Strengths: N/A";

                return;
            }

            emptyTeamMessage.IsVisible = false;

            var fullTeamMembers = new List<Pokemon>();

            foreach (var teamPokemon in team.Pokemon)
            {
                Pokemon? pokemon = await PokemonCache.GetCachedPokemon(teamPokemon.Name.ToLower());

                if (pokemon != null)
                {
                    fullTeamMembers.Add(pokemon);

                    var memberView = CreateTeamMemberView(pokemon);
                    teamMembersContainer.Children.Add(memberView);
                }
            }

            var summary = PokemonTeamCalculator.CalculateTeamSummary(fullTeamMembers);

            totalScoreLabel.Text = $"Total Score: {summary.TotalScore:N0}";
            teamWeaknessesLabel.Text = $"Team Weaknesses: {string.Join(", ", summary.Weaknesses)}";
            teamStrengthsLabel.Text = $"Team Strengths: {string.Join(", ", summary.Strengths)}";

        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load team details: {ex.Message}", "OK");
        }
    }

    private Grid CreateTeamMemberView(Pokemon pokemon)
    {
        var grid = new Grid
        {
            ColumnSpacing = 20,
            Padding = 15,
            Margin = new Thickness(0, 0, 0, 10)
        };
        grid.SetDynamicResource(Grid.BackgroundColorProperty, "CardBackgroundColor");

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180, GridUnitType.Absolute) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        var spriteFrame = new Microsoft.Maui.Controls.Frame
        {
            WidthRequest = 160,
            HeightRequest = 160,
            CornerRadius = 80,
            Padding = 10,
            HasShadow = false,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        spriteFrame.SetDynamicResource(Microsoft.Maui.Controls.Frame.BackgroundColorProperty, "SurfaceColor");
        spriteFrame.SetDynamicResource(Microsoft.Maui.Controls.Frame.BorderColorProperty, "BorderColor");

        var sprite = new Image
        {
            Source = PokemonCache.GetCachedSprite(pokemon.Name.ToLower()),
            WidthRequest = 140,
            HeightRequest = 140,
            Aspect = Aspect.AspectFit
        };
        spriteFrame.Content = sprite;
        Grid.SetColumn(spriteFrame, 0);

        // Pokemon Details
        var detailsStack = new VerticalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center
        };

        var nameLabel = new Label
        {
            Text = PokemonFormatter.FormatPokemonName(pokemon.Name),
            FontSize = 20,
            FontAttributes = FontAttributes.Bold
        };
        detailsStack.Children.Add(nameLabel);

        detailsStack.Children.Add(new Label
        {
            Text = $"Height: {pokemon.Height / 10.0} M",
            FontSize = 14
        });

        detailsStack.Children.Add(new Label
        {
            Text = $"Weight: {pokemon.Weight / 10.0} KG",
            FontSize = 14
        });

        string types = pokemon.Types != null
            ? string.Join(", ", pokemon.Types.Select(t => PokemonFormatter.FormatPokemonName(t.Type.Name)))
            : "N/A";
        detailsStack.Children.Add(new Label
        {
            Text = $"Types: {types}",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold
        });

        detailsStack.Children.Add(new Label
        {
            Text = $"Base Stat Total: {pokemon.TotalBaseStats}",
            FontSize = 14
        });

        var divider = new BoxView
        {
            HeightRequest = 1,
            Margin = new Thickness(0, 5)
        };
        divider.SetDynamicResource(BoxView.ColorProperty, "DividerColor");
        detailsStack.Children.Add(divider);

        var strengthLabel = new Label
        {
            Text = PokemonFormatter.FormatStrengths(pokemon.Strengths),
            FontSize = 14
        };
        strengthLabel.SetDynamicResource(Label.TextColorProperty, "SuccessColor");
        detailsStack.Children.Add(strengthLabel);

        var weaknessLabel = new Label
        {
            Text = PokemonFormatter.FormatWeaknesses(pokemon.Weaknesses),
            FontSize = 14
        };
        weaknessLabel.SetDynamicResource(Label.TextColorProperty, "ErrorColor");
        detailsStack.Children.Add(weaknessLabel);

        var removeButton = new Button
        {
            Text = "Remove from Team",
            HorizontalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 10, 0, 0)
        };
        removeButton.SetDynamicResource(Button.BackgroundColorProperty, "ErrorColor");
        removeButton.TextColor = Colors.White;
        removeButton.Clicked += async (s, e) => await RemovePokemonFromTeam(pokemon.Name);
        detailsStack.Children.Add(removeButton);

        Grid.SetColumn(detailsStack, 1);

        grid.Children.Add(spriteFrame);
        grid.Children.Add(detailsStack);

        return grid;
    }

    private void OnBackToTeamsClicked(object sender, EventArgs e)
    {
        teamDetailView.IsVisible = false;
        teamsListView.IsVisible = true;
        currentTeam = null; 
    }

    private async Task RemovePokemonFromTeam(string pokemonName)
    {
        try
        {
            bool confirm = await DisplayAlert("Remove Pokémon",
                $"Remove {PokemonFormatter.FormatPokemonName(pokemonName)} from team?",
                "Yes", "No");

            if (!confirm)
                return;

            var pokemon = currentTeam.Pokemon.FirstOrDefault(p =>
                p.Name.Equals(pokemonName, StringComparison.OrdinalIgnoreCase));

            if (pokemon != null)
            {
                currentTeam.Pokemon.Remove(pokemon);
                currentTeam.PokemonCount = currentTeam.Pokemon.Count;

                SaveTeam(currentTeam);
                LoadTeams();
                DisplayTeams();
                var reloadedTeam = teams.FirstOrDefault(t => t.Id == currentTeam.Id);
                if (reloadedTeam != null)
                {
                    currentTeam = reloadedTeam;
                    await LoadTeamDetail(currentTeam);
                }
                await LoadTeamDetail(currentTeam);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to remove Pokémon: {ex.Message}", "OK");
        }
    }

    private async void OnAddTeamClicked(object sender, EventArgs e)
    { 
        string teamName = await DisplayPromptAsync("New Team", "Enter team name:", "Create", "Cancel", "Team Name");

        if (!string.IsNullOrWhiteSpace(teamName))
        {
            try
            {
                int nextId = teams.Count > 0 ? teams.Max(t => t.Id) + 1 : 1;

                var newTeam = new PokemonTeam
                {
                    Id = nextId,
                    Name = teamName,
                    Pokemon = new List<TeamPokemon>(),
                    PokemonCount = 0,
                    CreatedDate = DateTime.Now
                };

                SaveTeam(newTeam);
                LoadTeams();
                DisplayTeams();

                currentTeam = newTeam;
                await LoadTeamDetail(newTeam);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to create team: {ex.Message}", "OK");
            }
        }
    }

    private void SaveTeam(PokemonTeam team)
    {
        try
        {
            team.PokemonCount = team.Pokemon?.Count ?? 0;

            var json = JsonSerializer.Serialize(team, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var filePath = System.IO.Path.Combine(teamsFolder, $"team_{team.Id}.json");
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to save team: {ex.Message}", "OK");
        }
    }

    private void DeleteTeam(PokemonTeam team)
    {
        try
        {
            var filePath = System.IO.Path.Combine(teamsFolder, $"team_{team.Id}.json");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to delete team: {ex.Message}", "OK");
        }
    }

    private async Task AddPokemonToTeam(string pokemonName)
    {
        if (currentTeam == null)
            return;


        if (currentTeam.PokemonCount >= 6)
        {
            await DisplayAlert("Error", "Team is full (max 6 Pokémon)", "OK");
            return;
        }

        try
        {
            var pokemon = await PokemonCache.GetCachedPokemon(pokemonName);

            if (pokemon == null)
            {
                await DisplayAlert("Error", "Pokémon not found!", "OK");
                return;
            }

            var teamPokemon = new TeamPokemon
            {
                Name = pokemon.Name,
                SpriteUrl = PokemonCache.GetCachedSprite(pokemon.Name.ToLower()) ?? string.Empty,
                Types = pokemon.Types?.Select(t => t.Type.Name).ToList() ?? new List<string>(),
                Level = 50
            };

            currentTeam.Pokemon.Add(teamPokemon);
            currentTeam.PokemonCount = currentTeam.Pokemon.Count;

            SaveTeam(currentTeam);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to add Pokémon: {ex.Message}", "OK");
        }
    }

    private async void OnAddPokemonToTeamClicked(object sender, EventArgs e)
    {
        if (currentTeam == null)
            return;
        if (currentTeam.PokemonCount >= 6)
        {
            await DisplayAlert("Error", "Team is full", "OK");
            return;
        }
            
        await Shell.Current.GoToAsync($"//SearchRoute/MainPage?teamId={currentTeam.Id}&fromTeamsPage=true");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        teamDetailView.IsVisible = false;
        teamsListView.IsVisible = true;
        currentTeam = null;
    }
}

