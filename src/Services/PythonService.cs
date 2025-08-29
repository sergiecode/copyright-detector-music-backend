using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using CopyrightDetector.MusicBackend.Models;

namespace CopyrightDetector.MusicBackend.Services;

/// <summary>
/// Service for executing Python scripts and integrating with music-embeddings and vector-search modules
/// </summary>
public class PythonService
{
    private readonly ILogger<PythonService> _logger;
    private readonly string _pythonPath;
    private readonly string _scriptsPath;
    private readonly string _embeddingsRepoPath;
    private readonly string _vectorSearchRepoPath;

    public PythonService(ILogger<PythonService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _pythonPath = configuration["PythonConfig:PythonPath"] ?? "python";
        _scriptsPath = configuration["PythonConfig:ScriptsPath"] ?? "./python";
        _embeddingsRepoPath = configuration["PythonConfig:EmbeddingsRepoPath"] ?? "../copyright-detector-music-embeddings";
        _vectorSearchRepoPath = configuration["PythonConfig:VectorSearchRepoPath"] ?? "../copyright-detector-vector-search";
    }

    /// <summary>
    /// Extract embeddings from an audio file using the music-embeddings module
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file</param>
    /// <param name="modelName">Model to use for extraction (spectrogram, openl3, audioclip)</param>
    /// <returns>Embedding extraction result</returns>
    public async Task<EmbeddingResult> ExtractEmbeddingsAsync(string audioFilePath, string modelName = "spectrogram")
    {
        try
        {
            var scriptPath = Path.Combine(_scriptsPath, "embedding_wrapper.py");
            var arguments = $"\"{scriptPath}\" \"{audioFilePath}\" \"{modelName}\"";

            _logger.LogInformation("Extracting embeddings for {AudioFile} using model {Model}", audioFilePath, modelName);

            var result = await ExecutePythonScriptAsync(arguments);
            
            if (string.IsNullOrEmpty(result))
            {
                return new EmbeddingResult { Success = false, Error = "No output from Python script" };
            }

            var embeddingResult = JsonConvert.DeserializeObject<EmbeddingResult>(result);
            return embeddingResult ?? new EmbeddingResult { Success = false, Error = "Failed to parse Python output" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting embeddings from {AudioFile}", audioFilePath);
            return new EmbeddingResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Search for similar tracks using the vector-search module
    /// </summary>
    /// <param name="embeddings">Audio embeddings to search with</param>
    /// <param name="topK">Number of similar tracks to return</param>
    /// <param name="indexPath">Path to the FAISS index</param>
    /// <returns>Similarity search result</returns>
    public async Task<SimilaritySearchResult> SearchSimilarTracksAsync(double[] embeddings, int topK = 10, string? indexPath = null)
    {
        try
        {
            var scriptPath = Path.Combine(_scriptsPath, "search_wrapper.py");
            var embeddingsJson = JsonConvert.SerializeObject(embeddings);
            var actualIndexPath = indexPath ?? "./data/music_index.faiss";
            var arguments = $"\"{scriptPath}\" \"{embeddingsJson}\" \"{actualIndexPath}\" {topK}";

            _logger.LogInformation("Searching for similar tracks with {TopK} results", topK);

            var result = await ExecutePythonScriptAsync(arguments);
            
            if (string.IsNullOrEmpty(result))
            {
                return new SimilaritySearchResult { Success = false, Error = "No output from Python script" };
            }

            var searchResult = JsonConvert.DeserializeObject<SimilaritySearchResult>(result);
            return searchResult ?? new SimilaritySearchResult { Success = false, Error = "Failed to parse Python output" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for similar tracks");
            return new SimilaritySearchResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Perform complete analysis: extract embeddings and search for similar tracks
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file</param>
    /// <param name="modelName">Model to use for extraction</param>
    /// <param name="topK">Number of similar tracks to return</param>
    /// <param name="similarityThreshold">Threshold for copyright detection</param>
    /// <returns>Complete analysis result</returns>
    public async Task<(EmbeddingResult embeddingResult, SimilaritySearchResult searchResult)> AnalyzeAudioAsync(
        string audioFilePath, 
        string modelName = "spectrogram", 
        int topK = 10, 
        double similarityThreshold = 0.8)
    {
        var stopwatch = Stopwatch.StartNew();

        // Extract embeddings
        var embeddingResult = await ExtractEmbeddingsAsync(audioFilePath, modelName);
        
        if (!embeddingResult.Success)
        {
            return (embeddingResult, new SimilaritySearchResult { Success = false, Error = "Embedding extraction failed" });
        }

        // Search for similar tracks
        var searchResult = await SearchSimilarTracksAsync(embeddingResult.Embeddings, topK);
        
        if (searchResult.Success)
        {
            // Calculate copyright risk based on similarity scores
            searchResult.CopyrightRisk = CalculateCopyrightRisk(searchResult.SimilarTracks, similarityThreshold);
            searchResult.RiskScore = CalculateRiskScore(searchResult.SimilarTracks);
            searchResult.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return (embeddingResult, searchResult);
    }

    /// <summary>
    /// Execute a Python script and return the output
    /// </summary>
    /// <param name="arguments">Arguments to pass to Python</param>
    /// <returns>Script output</returns>
    private async Task<string> ExecutePythonScriptAsync(string arguments)
    {
        using var process = new Process();
        process.StartInfo.FileName = _pythonPath;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var errorMessage = error.ToString();
            _logger.LogError("Python script failed with exit code {ExitCode}: {Error}", process.ExitCode, errorMessage);
            throw new InvalidOperationException($"Python script failed: {errorMessage}");
        }

        return output.ToString().Trim();
    }

    /// <summary>
    /// Calculate overall copyright risk based on similarity scores
    /// </summary>
    private string CalculateCopyrightRisk(List<SimilarTrack> similarTracks, double threshold)
    {
        if (!similarTracks.Any())
            return "LOW";

        var maxSimilarity = similarTracks.Max(t => t.SimilarityScore);
        var highSimilarityCount = similarTracks.Count(t => t.SimilarityScore >= threshold);

        return maxSimilarity switch
        {
            >= 0.95 => "VERY_HIGH",
            >= 0.85 => "HIGH",
            >= 0.70 when highSimilarityCount > 1 => "HIGH",
            >= 0.70 => "MEDIUM",
            >= 0.50 => "MEDIUM",
            _ => "LOW"
        };
    }

    /// <summary>
    /// Calculate numerical risk score (0.0 to 1.0)
    /// </summary>
    private double CalculateRiskScore(List<SimilarTrack> similarTracks)
    {
        if (!similarTracks.Any())
            return 0.0;

        var maxSimilarity = similarTracks.Max(t => t.SimilarityScore);
        var avgTopSimilarity = similarTracks.Take(3).Average(t => t.SimilarityScore);
        
        // Weighted score: 70% max similarity + 30% average of top 3
        return Math.Round(0.7 * maxSimilarity + 0.3 * avgTopSimilarity, 3);
    }
}
