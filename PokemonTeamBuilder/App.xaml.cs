namespace PokemonTeamBuilder
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            LoadTheme();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
     
            var httpClient = new HttpClient();
            var pokemonService = new PokemonService(httpClient);
            var downloader = new PokemonBulkDownloader(httpClient, pokemonService); 

            Page initialPage;

       
            if (!downloader.IsDownloadComplete())
            {

                initialPage = new LoadingPage(downloader); 
            }
            else
            { 

                initialPage = new AppShell();
            }

  
            return new Window(initialPage);
        }

        private void LoadTheme()
        {

            bool isDarkMode = Preferences.Get("DarkMode", false);
            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

            if (mergedDictionaries.Count > 0)
            {
                mergedDictionaries.Clear();
            }

            if (isDarkMode)
            {
                mergedDictionaries.Add(new Themes.DarkTheme());
            }
            else
            {
                mergedDictionaries.Add(new Themes.LightTheme());
            }
        }
    }
}