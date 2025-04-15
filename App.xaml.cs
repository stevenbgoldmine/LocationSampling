namespace LocationSampling
{
    public partial class App : Application
    {
        public static string AppLogFolder => Path.Combine(FileSystem.Current.AppDataDirectory, "logs");

        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}
