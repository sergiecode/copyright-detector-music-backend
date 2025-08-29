namespace CopyrightDetector.MusicBackend.Models;

/// <summary>
/// Result of embedding extraction operation
/// </summary>
public class EmbeddingResult
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Extracted embeddings as array of doubles
    /// </summary>
    public double[] Embeddings { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Shape of the embedding vector
    /// </summary>
    public int[] Shape { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Model used for extraction
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Duration of the processed audio in seconds
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// Sample rate of the processed audio
    /// </summary>
    public int SampleRate { get; set; }
}
