
namespace LocationSampling
{
    public partial class App : Application
    {
        public static string AppLogFolder => Path.Combine(FileSystem.Current.AppDataDirectory, "logs");

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
