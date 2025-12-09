using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO;

namespace PokemonTeamBuilder;

public partial class TeamsPage : ContentPage
{
    private ObservableCollection<PokemonTeam> teams;
    private string teamsFolder;

    public TeamsPage()
    {
        InitializeComponent();
        InitializeTeamsFolder();
        LoadTeams();
        DisplayTeams();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadTeams();
        DisplayTeams();
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

    private int CountPokemonInJson(string json)
    {
      
        if (string.IsNullOrEmpty(json) || json == "[]") return 0;
        return json.Split(',').Length;
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


        var nameLabel = new Label
        {
            Text = team.Name,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Colors.Black
        };
        mainLayout.Children.Add(nameLabel);

        
        if (team.Id == 1 && team.PokemonCount > 0)
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

            mainLayout.Children.Add(pokemonGrid);
        }

        border.Content = mainLayout;

        
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => OnTeamCardTapped(team);
        border.GestureRecognizers.Add(tapGesture);

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
        };


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

        return border;
    }

    private async void OnTeamCardTapped(PokemonTeam team)
    {
        await DisplayAlert("Team Selected", $"You selected {team.Name}", "OK");
       
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
    public string Name { get; set; }
    public List<TeamPokemon> Pokemon { get; set; } = new List<TeamPokemon>();
    public int PokemonCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class TeamPokemon
{
    public string Name { get; set; }
    public string SpriteUrl { get; set; }
    public List<string> Types { get; set; }
    public int Level { get; set; }
}


