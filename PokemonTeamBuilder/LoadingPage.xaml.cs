namespace PokemonTeamBuilder;

public partial class LoadingPage : ContentPage
{
    private readonly PokemonBulkDownloader _downloader;

    public LoadingPage(PokemonBulkDownloader downloader)
    {
        InitializeComponent();
        _downloader = downloader;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartDownload();
    }

    private async Task StartDownload()
    {
        var progress = new Progress<DownloadProgress>(UpdateProgress);

        bool success = await _downloader.DownloadAllPokemon(progress);

        if (success)
        {
            await DisplayAlert("Download Complete", "All Pokémon data downloaded successfully!", "OK");
            // Navigate to main page
            Application.Current.MainPage = new AppShell();
        }
        else
        {
            bool retry = await DisplayAlert("Download Failed",
                "Failed to download Pokémon data. Would you like to retry?",
                "Retry", "Cancel");

            if (retry)
            {
                await StartDownload();
            }
        }
    }

    private void UpdateProgress(DownloadProgress progress)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            downloadProgressBar.Progress = progress.Percentage / 100.0;
            progressLabel.Text = $"{progress.Current} / {progress.Total}";

            if (progress.IsComplete)
            {
                statusLabel.Text = $"Download complete! ({progress.Failed} failed)";
                currentPokemonLabel.Text = "Ready to start!";
            }
            else
            {
                statusLabel.Text = $"Downloading Pokémon data... ({progress.Failed} failed)";
                currentPokemonLabel.Text = $"Current: #{progress.CurrentPokemonId}";
            }
        });
    }
}