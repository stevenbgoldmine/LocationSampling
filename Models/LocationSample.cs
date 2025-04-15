using System.Text.Json.Serialization;

namespace LocationSampling.Models;
public class LocationSample
{
    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; } = 0.0;

    [JsonPropertyName("lon")]
    public double Lon { get; set; } = 0.0;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; } = string.Empty;

    [JsonPropertyName("accuracy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Accuracy { get; set; }

    [JsonPropertyName("speed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Speed { get; set; }

    [JsonPropertyName("reducedaccuracy")]
    public bool ReducedAccuracy { get; set; } = false;

    public LocationSample()
    {
        Time = DateTime.Now;
    }
}
