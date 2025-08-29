namespace CopyrightDetector.MusicBackend.Models;

/// <summary>
/// Request for uploading and analyzing audio files
/// </summary>
public class AudioUploadRequest
{
    /// <summary>
    /// The uploaded audio file
    /// </summary>
    public IFormFile AudioFile { get; set; } = null!;

    /// <summary>
    /// Model to use for embedding extraction
    /// </summary>
    public string ModelName { get; set; } = "spectrogram";

    /// <summary>
    /// Number of similar tracks to return
    /// </summary>
    public int TopK { get; set; } = 10;

    /// <summary>
    /// Similarity threshold for copyright detection
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.8;
}

/// <summary>
/// Response for audio upload and analysis
/// </summary>
public class AudioUploadResponse
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Uploaded file information
    /// </summary>
    public UploadedFileInfo FileInfo { get; set; } = new();

    /// <summary>
    /// Embedding extraction result
    /// </summary>
    public EmbeddingResult? EmbeddingResult { get; set; }

    /// <summary>
    /// Similarity search result
    /// </summary>
    public SimilaritySearchResult? SearchResult { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Total processing time in milliseconds
    /// </summary>
    public long TotalProcessingTimeMs { get; set; }
}

/// <summary>
/// Information about uploaded file
/// </summary>
public class UploadedFileInfo
{
    /// <summary>
    /// Original filename
    /// </summary>
    public string OriginalFilename { get; set; } = string.Empty;

    /// <summary>
    /// Saved filename on server
    /// </summary>
    public string SavedFilename { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// File content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadTimestamp { get; set; } = DateTime.UtcNow;
}
