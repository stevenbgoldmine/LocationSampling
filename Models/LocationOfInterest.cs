using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocationSampling.Models;
public partial class LocationOfInterest : ObservableObject
{
    static public readonly double MaxLocationSamples = 30.0;

    static public readonly double GoodPrecision = 3.0;
    static public readonly double PoorPrecision = 6.0;
    static public readonly double BadPrecision = 100.0;

    [ObservableProperty]
    private string name = string.Empty;

    #region Sampling Progress Visibility and Value Properties
    [ObservableProperty]
    private double samplingProgress = 0.0;

    private bool _samplingProgressIsVisible = false;
    
    [JsonIgnore]
    public bool SamplingProgressIsVisible
    {
        get => _samplingProgressIsVisible;
        set
        {
            if (SetProperty(ref _samplingProgressIsVisible, value))
            {
                OnPropertyChanged(nameof(ShowLocationTimeStamp));
                OnPropertyChanged(nameof(ShowLocationDiagnostic));
                OnPropertyChanged(nameof(ShowLocationPrecision));

                OnPropertyChanged(nameof(LocationPrecisionColor));
                OnPropertyChanged(nameof(LocationPrecisionIndicator));

                if (_samplingProgressIsVisible)
                {
                    LocationSamples.Clear();
                }
            }
        }
    }

    // Sample locations used to determine the best location for this course mark.
    private ObservableCollection<LocationSample> locationSamples = [];
    public ObservableCollection<LocationSample> LocationSamples
    {
        get => locationSamples;
        set
        {
            // Unsubscribe from the previous collection's CollectionChanged event
            if (value != locationSamples)
                locationSamples.CollectionChanged -= LocationSamples_CollectionChanged;

            SetProperty(ref locationSamples, value);

            // Subscribe to the new collection's CollectionChanged event
            if (locationSamples != null)
                locationSamples.CollectionChanged += LocationSamples_CollectionChanged;
        }
    }

    /// <summary>
    /// Replace the content of the LocationSamples collection with the provided samples.
    /// </summary>
    /// <param name="samples"></param>
    public void ReplaceLocationSamples(List<LocationSample> samples)
    {
        LocationSamples.CollectionChanged -= LocationSamples_CollectionChanged;

        LocationSamples.Clear();
        samples.ForEach(s => LocationSamples.Add(s));

        LocationSamples.CollectionChanged += LocationSamples_CollectionChanged;
    }

    /// <summary>
    /// Update the sampling progress value when the LocationSamples collection changes.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LocationSamples_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
        {
            SamplingProgress = LocationSamples.Count / MaxLocationSamples;
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            SamplingProgress = 0.0;
        }
    }

    #endregion

    [JsonIgnore]
    public bool ShowLocationTimeStamp => !SamplingProgressIsVisible && Location != null;

    [JsonIgnore]
    public string LocationTimeStamp => Location != null ? Location.Time.ToLocalTime().ToString() : string.Empty;

    #region Location Diagnostic Visibility and Value Properties
    [ObservableProperty]
    private string locationDiagnostic = string.Empty;

    [JsonIgnore]
    public bool ShowLocationDiagnostic => !SamplingProgressIsVisible && Location != null && Location.Accuracy != null;
    #endregion

    #region Location Precision Properties

    // This property is used to determine which text color to display in the UI
    // indicating the precision (quality) of the location data.
    [JsonIgnore]
    public string LocationPrecisionColor
    {
        get
        {
            if (Location == null || Location.Accuracy >= BadPrecision)
                return "#cc3232"; // red

            string color = "#db7b2b"; // orange -> poor precision

            if (Location.Accuracy < GoodPrecision)
                color = "#2dc937"; // green

            return color;
        }
    }

    // This property is used to determine which icon to display in the UI
    // an indication of the precision (quality) of the location data.
    [JsonIgnore]
    public string LocationPrecisionIndicator
    {
        get
        {
            if (Location == null || Location.Accuracy >= BadPrecision)
                return "cancel_red";

            string indicator = "check_orange";

            if (Location.Accuracy < GoodPrecision)
                indicator = "check_green";

            return indicator;
        }
    }

    [JsonIgnore]
    public bool ShowLocationPrecision => !SamplingProgressIsVisible && Location != null && Location.Accuracy != null;

    #endregion

    private LocationSample? _location;
    public LocationSample? Location
    {
        get => _location;
        set
        {
            if (SetProperty(ref _location, value))
            {
                OnPropertyChanged(nameof(LocationTimeStamp));
                OnPropertyChanged(nameof(ShowLocationTimeStamp));
                OnPropertyChanged(nameof(ShowLocationDiagnostic));
                OnPropertyChanged(nameof(ShowLocationPrecision));
                OnPropertyChanged(nameof(LocationPrecisionColor));
                OnPropertyChanged(nameof(LocationPrecisionIndicator));
            }
        }
    }

    [JsonIgnore]
    public Location? AsLocation => Location != null ? new Location(Location.Lat, Location.Lon) : null;

    public LocationOfInterest(string name)
    {
        Name = name;

        LocationSamples.CollectionChanged += LocationSamples_CollectionChanged;
    }

}
