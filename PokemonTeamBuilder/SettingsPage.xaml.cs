namespace PokemonTeamBuilder
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            bool isDarkMode = Preferences.Get("DarkMode", false);
            darkModeSwitch.IsToggled = isDarkMode;

            string trainerName = Preferences.Get("TrainerName", "Ash Ketchum");
            trainerNameEntry.Text = trainerName;
        }

        private void OnDarkModeToggled(object sender, ToggledEventArgs e)
        {
            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

            if (mergedDictionaries.Count > 0)
            {
                mergedDictionaries.Clear();
            }

            if (e.Value) 
            {
                mergedDictionaries.Add(new Themes.DarkTheme()); 
            }
            else 
            {
                mergedDictionaries.Add(new Themes.LightTheme()); 
            }

            Preferences.Set("DarkMode", e.Value);
        }

        private async void OnSaveSettings(object sender, EventArgs e)
        {
            Preferences.Set("TrainerName", trainerNameEntry.Text);
            await DisplayAlert("Success", "Settings saved!", "OK");
        }

    }
}