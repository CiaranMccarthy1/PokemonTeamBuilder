namespace PokemonTeamBuilder
{
    public partial class AppShell : Shell
    {
        public static bool IsNavigatingFromFlyout { get; set; }

        public AppShell()
        {
            InitializeComponent();
            Navigating += OnShellNavigating;
        }

        private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
        {
            var target = e.Target.Location.ToString();
            var source = e.Source;
            Debug.Log($"Shell Navigating: Source={source}, Target={target}");

            if ((source == ShellNavigationSource.ShellItemChanged || source == ShellNavigationSource.ShellContentChanged) &&
                    (target.Contains("MainPage") || target.Contains("SearchRoute") || target.Contains("TeamsPage")) &&
                    !target.Contains("?"))
            {
                IsNavigatingFromFlyout = true;
                Debug.Log("Flyout navigation detected - clear params");
            }
            else
            {
                IsNavigatingFromFlyout = false;
            }
        }
    }
}
