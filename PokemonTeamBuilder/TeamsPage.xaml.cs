using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;

namespace PokemonTeamBuilder;

public partial class TeamsPage : ContentPage
{
    private ObservableCollection<PokemonTeam> teams;
    private string teamsFolder;
    private PokemonTeam currentTeam;
    private readonly PokemonService pokemonService;

    public TeamsPage()
    {
        InitializeComponent();
        pokemonService = new PokemonService(new HttpClient());
        InitializeTeamsFolder();
        LoadTeams();
        DisplayTeams();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadTeams();
        DisplayTeams();

        if (currentTeam != null && teamDetailView.IsVisible)
        {
            LoadTeamDetail(currentTeam);
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
            BackgroundColor = Color.FromArgb("#D3D3D3"),
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(40, 0, 0, 40)
            },
            Padding = 15,
            HeightRequest = 250
        };

        var mainLayout = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
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
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Colors.Black
        };
        headerGrid.Children.Add(nameLabel);

        var deleteButton = new Button
        {
            Text = "×",
            FontSize = 24,
            WidthRequest = 30,
            HeightRequest = 30,
            CornerRadius = 15,
            Padding = 0,
            BackgroundColor = Colors.Red,
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

        // Display Pokemon sprites grid
        if (team.PokemonCount > 0)
        {
            var pokemonGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(60) },
                    new ColumnDefinition { Width = new GridLength(60) }
                },
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = new GridLength(60) },
                    new RowDefinition { Height = new GridLength(60) },
                    new RowDefinition { Height = new GridLength(60) }
                },
                ColumnSpacing = 10,
                RowSpacing = 10,
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(10, 10, 0, 0)
            };

            for (int i = 0; i < 6; i++)
            {
                if (i < team.Pokemon.Count)
                {
                    var image = new Image
                    {
                        Source = team.Pokemon[i].SpriteUrl,
                        WidthRequest = 60,
                        HeightRequest = 60,
                        Aspect = Aspect.AspectFit
                    };
                    Grid.SetRow(image, i / 2);
                    Grid.SetColumn(image, i % 2);
                    pokemonGrid.Children.Add(image);
                }
                else
                {
                    var box = new BoxView
                    {
                        Color = Color.FromArgb("#808080"),
                        WidthRequest = 60,
                        HeightRequest = 60,
                        CornerRadius = 5
                    };
                    Grid.SetRow(box, i / 2);
                    Grid.SetColumn(box, i % 2);
                    pokemonGrid.Children.Add(box);
                }
            }

            Grid.SetRow(pokemonGrid, 1);
            mainLayout.Children.Add(pokemonGrid);
        }

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
                Pokemon pokemon;

                if (PokemonCache.IsCached(teamPokemon.Name.ToLower()))
                {
                    pokemon = await PokemonCache.GetCachedPokemon(teamPokemon.Name.ToLower());
                }
                else
                {
                    pokemon = await pokemonService.GetPokemon(teamPokemon.Name.ToLower());
                }

                if (pokemon != null)
                {
                    fullTeamMembers.Add(pokemon);

                    var memberView = CreateTeamMemberView(pokemon);
                    teamMembersContainer.Children.Add(memberView);
                }
            }

            var summary = pokemonService.CalculateTeamSummary(fullTeamMembers);

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
            BackgroundColor = Color.FromArgb("#F5F5F5"),
            Margin = new Thickness(0, 0, 0, 10)
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250, GridUnitType.Absolute) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        // Pokemon Sprite
        var sprite = new Image
        {
            Source = pokemon.Sprites?.FrontDefault ?? PokemonCache.GetCachedSprite(pokemon.Name.ToLower()),
            WidthRequest = 250,
            HeightRequest = 250,
            Aspect = Aspect.AspectFit,
            BackgroundColor = Colors.White
        };
        Grid.SetColumn(sprite, 0);

        // Pokemon Details
        var detailsStack = new VerticalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center
        };

        detailsStack.Children.Add(new Label
        {
            Text = FormatPokemonName(pokemon.Name),
            FontSize = 20,
            FontAttributes = FontAttributes.Bold
        });

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
            ? string.Join(", ", pokemon.Types.Select(t => FormatPokemonName(t.Type.Name)))
            : "N/A";
        detailsStack.Children.Add(new Label
        {
            Text = $"Types: {types}",
            FontSize = 14
        });

        detailsStack.Children.Add(new Label
        {
            Text = $"Base Stat Total: {pokemon.TotalBaseStats}",
            FontSize = 14
        });

        detailsStack.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Colors.Gray,
            Margin = new Thickness(0, 5)
        });

        detailsStack.Children.Add(new Label
        {
            Text = FormatStrengths(pokemon.Strengths),
            FontSize = 14
        });

        detailsStack.Children.Add(new Label
        {
            Text = FormatWeaknesses(pokemon.Weaknesses),
            FontSize = 14
        });

        // Remove from team button
        var removeButton = new Button
        {
            Text = "Remove from Team",
            HorizontalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 10, 0, 0),
            BackgroundColor = Colors.Red,
            TextColor = Colors.White
        };
        removeButton.Clicked += async (s, e) => await RemovePokemonFromTeam(pokemon.Name);
        detailsStack.Children.Add(removeButton);

        Grid.SetColumn(detailsStack, 1);

        grid.Children.Add(sprite);
        grid.Children.Add(detailsStack);

        return grid;
    }

    private string FormatStrengths(List<PokemonStrengthWrapper> strengths)
    {
        if (strengths == null || strengths.Count == 0)
            return "Strengths: None";

        var formattedList = strengths
            .OrderByDescending(s => s.Multiplier)
            .Select(s => $"{char.ToUpper(s.Type[0]) + s.Type.Substring(1)} ({s.Multiplier:0.##}x)");

        return $"Strengths: {string.Join(", ", formattedList)}";
    }

    private string FormatWeaknesses(List<PokemonWeaknessWrapper> weaknesses)
    {
        if (weaknesses == null || weaknesses.Count == 0)
            return "Weaknesses: None";

        var formattedList = weaknesses
            .OrderByDescending(w => w.Multiplier)
            .Select(w => $"{char.ToUpper(w.Type[0]) + w.Type.Substring(1)} ({w.Multiplier:0.##}x)");

        return $"Weaknesses: {string.Join(", ", formattedList)}";
    }

    private static string FormatPokemonName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "N/A";

        return char.ToUpper(name[0]) + name.Substring(1);
    }

    private void OnBackToTeamsClicked(object sender, EventArgs e)
    {
        teamDetailView.IsVisible = false;
        teamsListView.IsVisible = true;
        currentTeam = null;
    }

    private async void OnAddPokemonToTeamClicked(object sender, EventArgs e)
    {
        if (currentTeam == null)
            return;

        if (currentTeam.Pokemon.Count >= 6)
        {
            await DisplayAlert("Team Full", "A team can only have 6 Pokémon!", "OK");
            return;
        }

        string pokemonName = await DisplayPromptAsync("Add Pokémon", "Enter Pokémon name:", "Add", "Cancel", "Team Name");

        if (!string.IsNullOrWhiteSpace(pokemonName))
        {
            await AddPokemonToTeam(pokemonName.ToLower());
        }
    }

    private async Task AddPokemonToTeam(string pokemonName)
    {
        try
        {
            var pokemon = await pokemonService.GetPokemon(pokemonName);

            if (pokemon == null)
            {
                await DisplayAlert("Error", "Pokémon not found!", "OK");
                return;
            }

            var teamPokemon = new TeamPokemon
            {
                Name = pokemon.Name,
                SpriteUrl = pokemon.Sprites?.FrontDefault,
                Types = pokemon.Types?.Select(t => t.Type.Name).ToList() ?? new List<string>(),
                Level = 50
            };

            currentTeam.Pokemon.Add(teamPokemon);
            currentTeam.PokemonCount = currentTeam.Pokemon.Count;

            SaveTeam(currentTeam);
            await LoadTeamDetail(currentTeam);

            await DisplayAlert("Success", $"{FormatPokemonName(pokemon.Name)} added to team!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to add Pokémon: {ex.Message}", "OK");
        }
    }

    private async Task RemovePokemonFromTeam(string pokemonName)
    {
        try
        {
            bool confirm = await DisplayAlert("Remove Pokémon",
                $"Remove {FormatPokemonName(pokemonName)} from team?",
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
                await LoadTeamDetail(currentTeam);

                await DisplayAlert("Success", $"{FormatPokemonName(pokemonName)} removed from team", "OK");
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
}

public class PokemonTeam
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; 
    public List<TeamPokemon> Pokemon { get; set; } = new List<TeamPokemon>();
    public int PokemonCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class TeamPokemon
{
    public string Name { get; set; } = string.Empty; 
    public string SpriteUrl { get; set; } = string.Empty; 
    public List<string> Types { get; set; } = new List<string>();
    public int Level { get; set; }
}


public class TeamSummary
{
    public int TotalScore { get; set; }
    public List<string> Weaknesses { get; set; } = new List<string>();
    public List<string> Strengths { get; set; } = new List<string>();
}