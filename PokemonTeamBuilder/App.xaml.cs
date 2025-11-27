namespace PokemonTeamBuilder
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

      
            LoadTheme();
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

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}