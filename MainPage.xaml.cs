using LocationSampling.Models;

namespace LocationSampling
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is MainPageViewModel vm)
            {
                vm.OnAppearing();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (BindingContext is MainPageViewModel vm)
            {
                vm.OnDisappearing();
            }
        }

        private void LocationGroup_Tapped(object sender, TappedEventArgs e)
        {
            if (BindingContext is MainPageViewModel vm)
            {
                vm.SelectedLocationGroup = (LocationOfInterest)e.Parameter;
            }
        }

        private async void CourseMark_ShowOnMap(object sender, TappedEventArgs e)
        {
            if (BindingContext is MainPageViewModel)
            {
                var tappedMark = e.Parameter as LocationOfInterest;
                if (!tappedMark.SamplingProgressIsVisible)
                {
                    var mapLaunchOptions = new MapLaunchOptions { Name = tappedMark.Name };
                    try
                    {
                        // await Map.Default.OpenAsync(tappedMark.AsLocation, mapLaunchOptions);
                        await LocationExtensions.OpenMapsAsync(tappedMark.AsLocation, mapLaunchOptions);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }


    }
}
