namespace CopyrightDetector.MusicBackend.Models;

/// <summary>
/// Result of similarity search operation
/// </summary>
public class SimilaritySearchResult
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of similar tracks found
    /// </summary>
    public List<SimilarTrack> SimilarTracks { get; set; } = new();

    /// <summary>
    /// Overall copyright risk assessment
    /// </summary>
    public string CopyrightRisk { get; set; } = "LOW";

    /// <summary>
    /// Numerical risk score (0.0 to 1.0)
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// Total number of similar tracks found
    /// </summary>
    public int TotalMatches { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Information about a similar track
/// </summary>
public class SimilarTrack
{
    /// <summary>
    /// Filename of the similar track
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Artist name
    /// </summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// Album name
    /// </summary>
    public string Album { get; set; } = string.Empty;

    /// <summary>
    /// Music genre
    /// </summary>
    public string Genre { get; set; } = string.Empty;

    /// <summary>
    /// Similarity score (0.0 to 1.0)
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Distance metric from the search
    /// </summary>
    public double Distance { get; set; }

    /// <summary>
    /// Copyright risk level for this specific match
    /// </summary>
    public string CopyrightRisk { get; set; } = "LOW";

    /// <summary>
    /// Duration of the track in seconds
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// Release year
    /// </summary>
    public int? Year { get; set; }
}
