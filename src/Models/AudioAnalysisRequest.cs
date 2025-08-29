using System.ComponentModel.DataAnnotations;

namespace CopyrightDetector.MusicBackend.Models;

/// <summary>
/// Request model for audio analysis operations
/// </summary>
public class AudioAnalysisRequest
{
    /// <summary>
    /// Path to the audio file to analyze
    /// </summary>
    [Required]
    public string AudioFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Model to use for embedding extraction (spectrogram, openl3, audioclip)
    /// </summary>
    public string ModelName { get; set; } = "spectrogram";

    /// <summary>
    /// Number of similar tracks to return
    /// </summary>
    public int TopK { get; set; } = 10;

    /// <summary>
    /// Similarity threshold for copyright detection (0.0 to 1.0)
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.8;
}
