using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Serilog;
using System.Text.Json;
using LocationSampling.Models;

namespace LocationSampling;
public partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string title;

#if IOS
    [ObservableProperty]
    private bool listenForLocationSamples = true;
#elif ANDROID
    // Default on Android is to poll for location samples as listening is very problematic
    [ObservableProperty]
    private bool listenForLocationSamples = false;
#endif

    [ObservableProperty]
    private bool canChangeSamplingMethod = true;

    public bool ShowSamplesDiagnostics => SamplesDiagnostics != string.Empty;

    private string _samplesDiagnostics = string.Empty;
    public string SamplesDiagnostics
    {
        get => _samplesDiagnostics;
        set
        {
            if (SetProperty(ref _samplesDiagnostics, value))
                OnPropertyChanged(nameof(ShowSamplesDiagnostics));
        }
    }

    private CancellationTokenSource _cts;

    private DateTimeOffset _prevPosTimeStamp = DateTimeOffset.MinValue;

    #region Location Group Selection

    // Save a course mark existing location samples allowing to restore them if the user cancels the current sampling underway.
    private readonly List<LocationSample> _savedSamples = [];

    [ObservableProperty]
    private List<LocationOfInterest> locationsOfInterest = [];

    private LocationOfInterest selectedLocationGroup;
    public LocationOfInterest SelectedLocationGroup
    {
        get => selectedLocationGroup;
        set
        {
            if (selectedLocationGroup != value && selectedLocationGroup != null && selectedLocationGroup.SamplingProgressIsVisible)
            {
                // stop the current sampling underway ...
                Stop();

                // restore the previous samples
                selectedLocationGroup.ReplaceLocationSamples(_savedSamples);
            }

            selectedLocationGroup = value;

            _savedSamples.Clear();
            _savedSamples.AddRange(value.LocationSamples);

            Start();
        }
    }

    #endregion

    public MainPageViewModel()
    {
        Title = "Samples";

        LocationsOfInterest.Add(new LocationOfInterest("Location 1"));
        LocationsOfInterest.Add(new LocationOfInterest("Location 2"));
    }

    public void OnAppearing()
    {
        if (!_locationPermissionGranted)
            RequestLocationPermission(Dispatcher.GetForCurrentThread());
    }

    public void OnDisappearing()
    {
        Stop();
    }

    [RelayCommand]
    private void ClearSamples()
    {
        Stop();

        foreach (var location in LocationsOfInterest)
        {
            location.Location = null;
            location.LocationSamples.Clear();
        }

        SamplesDiagnostics = string.Empty;
    }

    #region Location Sampling

    /// <summary>
    /// Start location sampling.
    /// </summary>
    private void Start()
    {
        if (!_locationPermissionGranted)
            return;

        SelectedLocationGroup.SamplingProgressIsVisible = true;
        SamplesDiagnostics = string.Empty;

        StartSamplingLocations();
    }

    /// <summary>
    /// Stop location sampling.
    /// </summary>
    private void Stop()
    {
        StopSamplingLocations();

        if (SelectedLocationGroup != null)
            SelectedLocationGroup.SamplingProgressIsVisible = false;
    }

    /// <summary>
    /// Validate the given location against a couple of criteria.
    /// </summary>
    /// <param name="location">The Location object to be validated</param>
    /// <returns>true if the given location is valid otherwise false</returns>
    private bool ValidLocation(Location location)
    {
        if (location.Accuracy == null)
            return false;

        // Include unique timestamp checking some model phones
        // report sampled locations but with the same timestamp. E.g. Xiamoi Redmi Note 13 Pro.
        if (location.Timestamp == _prevPosTimeStamp)
            return false;

        _prevPosTimeStamp = location.Timestamp;
        return true;
    }

    /// <summary>
    /// Select the most accurate location from the collection of location samples and add to the selected location of interest
    /// </summary>
    private async Task<bool> ProcessSampledLocations()
    {
        if (SelectedLocationGroup.LocationSamples.Count > 0)
        {
            // For now use the most accurate location recorded.
            SelectedLocationGroup.Location = SelectedLocationGroup.LocationSamples.OrderBy(l => l.Accuracy).ToList().First();

            // Capture the location precision so the quality of the location can be reported.
            SelectedLocationGroup.LocationDiagnostic = $"Accuracy: {SelectedLocationGroup.Location.Accuracy:F1}m";

            await PersistLocationOfInterest(LocationsOfInterest);
        }

        return true;
    }

    static readonly JsonSerializerOptions JsonSerializationOptions = new()
    {
        WriteIndented = true
    };

    static public async Task PersistLocationOfInterest(List<LocationOfInterest> locations)
    {
        string filePath = Path.Combine(FileSystem.Current.CacheDirectory, $"CF_{DateTime.Now:yyyyMMddHHmmss}.json");

        try
        {
            await using FileStream fs = File.Create(filePath);

            await JsonSerializer.SerializeAsync(fs, locations, JsonSerializationOptions);

            await Share.Default.RequestAsync(new ShareFileRequest { Title="Share Location of Interest", File = new ShareFile(filePath)});
        }
        catch (Exception e)
        {
            Log.Logger.Error($"Failed to persist session/course fragment! {e.Message}");
        }
    }

    #region Location Permissioning
    private bool _locationPermissionGranted = false;


    /// <summary>
    /// Setup a timer (expire immediately) to request location permission.
    /// </summary>
    private void RequestLocationPermission(IDispatcher? dispatcher)
    {
        var permissionRequestTimer = dispatcher?.CreateTimer();
        if (permissionRequestTimer != null)
        {
            permissionRequestTimer.Interval = TimeSpan.FromSeconds(0.0);
            permissionRequestTimer.IsRepeating = false;
            permissionRequestTimer.Tick += OnRequestLocationPermission;
            permissionRequestTimer.Start();
        }
    }

    /// <summary>
    /// Check the status of the location permission and request it if not already granted.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnRequestLocationPermission(object? sender, EventArgs e)
    {
        try
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status == PermissionStatus.Granted)
                {
                    _locationPermissionGranted = true;
                }
            }
            else
            {
                _locationPermissionGranted = true;
            }
        }
        catch (Exception)
        {
            Log.Error("Error requesting location permission!");
        }
    }

    #endregion

    private readonly GeolocationListeningRequest _geolocationListeningRequest = new(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1.0));

    private async void StartSamplingLocations()
    {
        _cts = new CancellationTokenSource();

        try
        {
            CanChangeSamplingMethod = false;

            if (ListenForLocationSamples)
            {
                Geolocation.LocationChanged += Geolocation_OnChange;
                Geolocation.ListeningFailed += Geolocation_OnListeningFailed;

                var success = await Geolocation.StartListeningForegroundAsync(_geolocationListeningRequest);
                if (!success)
                {
                    var toast = Toast.Make($"Failed to start GPS location sampling!", ToastDuration.Long, 20);
                    await toast.Show();
                }
            }
            else
            {
                var dispatcher = Dispatcher.GetForCurrentThread();

                // Polling for locations on a timer
                if (dispatcher != null)
                {
                    _currentLocationTimer = dispatcher.CreateTimer();
                    _currentLocationTimer.Interval = TimeSpan.FromSeconds(1.0);
                    _currentLocationTimer.IsRepeating = true;
                    _currentLocationTimer.Tick += UpdateCurrentLocation;
                    _currentLocationTimer.Start();
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error starting location sampling: {e.Message}");
        }
    }

    /// <summary>
    /// Location change when listening
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Geolocation_OnChange(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var location = e.Location;
        if (location != null)
        {
            if (ValidLocation(location))
            {
                var markLocation = new LocationSample()
                {
                    Lat = location.Latitude,
                    Lon = location.Longitude,
                    Time = location.Timestamp.DateTime,
                    Accuracy = location.Accuracy == null ? LocationOfInterest.BadPrecision : (double)location.Accuracy,
                    Speed = location.Speed ?? 0.0,
                    ReducedAccuracy = location.ReducedAccuracy
                };

                SelectedLocationGroup.LocationSamples.Add(markLocation);

                if (SelectedLocationGroup.LocationSamples.Count == LocationOfInterest.MaxLocationSamples)
                {
                    Stop();
                    _ = await ProcessSampledLocations();
                }
            }
        }
    }

    private void Geolocation_OnListeningFailed(object? sender, GeolocationListeningFailedEventArgs e)
    {
        Log.Error($"Error starting location sampling: {e}");
    }

    private void StopSamplingLocations()
    {
        _cts?.Cancel();

        if (ListenForLocationSamples)
        {
            Geolocation.StopListeningForeground();

            Geolocation.LocationChanged -= Geolocation_OnChange;
            Geolocation.ListeningFailed -= Geolocation_OnListeningFailed;
        }
        else
        {
            if (_currentLocationTimer != null)
            {
                _currentLocationTimer.Stop();
                _currentLocationTimer.Tick -= UpdateCurrentLocation;
            }
        }

        CanChangeSamplingMethod = true;
    }


    private IDispatcherTimer _currentLocationTimer;

    private readonly GeolocationRequest _geolocationRequest = new()
    {
        DesiredAccuracy = GeolocationAccuracy.Best,
        Timeout = TimeSpan.FromMilliseconds(800)
    };

    /// <summary>
    /// When not listening for location changes this is the timer call back to poll for a location sample
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void UpdateCurrentLocation(object? sender, EventArgs args)
    {
        try
        {
            Location location = await Geolocation.GetLocationAsync(_geolocationRequest, _cts.Token);
            if (location != null)
            {
                if (ValidLocation(location))
                {
                    var sample = new LocationSample()
                    {
                        Lat = location.Latitude,
                        Lon = location.Longitude,
                        Time = location.Timestamp.DateTime,
                        Accuracy = location.Accuracy == null ? LocationOfInterest.BadPrecision : (double)location.Accuracy,
                        Speed = location.Speed ?? 0.0,
                        ReducedAccuracy = location.ReducedAccuracy
                    };

                    SelectedLocationGroup.LocationSamples.Add(sample);

                    if (SelectedLocationGroup.LocationSamples.Count == LocationOfInterest.MaxLocationSamples)
                    {
                        Stop();
                        _ = await ProcessSampledLocations();
                    }
                }
            }
            else
            {
                Log.Logger.Error("Location is null");
            }
        }
        catch (Exception e)
        {
            Log.Logger.Error("Error sampling location: {0}", e.Message);
        }
    }

    #endregion
}

