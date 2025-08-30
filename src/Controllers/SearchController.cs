using Microsoft.AspNetCore.Mvc;
using CopyrightDetector.MusicBackend.Models;
using CopyrightDetector.MusicBackend.Services;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace CopyrightDetector.MusicBackend.Controllers;

/// <summary>
/// Controller for music similarity search and copyright detection
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly PythonService _pythonService;
    private readonly ILogger<SearchController> _logger;
    private readonly IConfiguration _configuration;

    public SearchController(PythonService pythonService, ILogger<SearchController> logger, IConfiguration configuration)
    {
        _pythonService = pythonService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Analyze an audio file for similarity search and copyright detection
    /// </summary>
    /// <param name="request">Audio analysis request</param>
    /// <returns>Analysis results including similar tracks and copyright risk</returns>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(SimilaritySearchResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SimilaritySearchResult>> AnalyzeAudio([FromBody] AudioAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("Starting audio analysis for file: {AudioFile}", request.AudioFilePath);

            // Validate file exists
            if (!System.IO.File.Exists(request.AudioFilePath))
            {
                return BadRequest($"Audio file not found: {request.AudioFilePath}");
            }

            var stopwatch = Stopwatch.StartNew();

            // Perform complete analysis
            var (embeddingResult, searchResult) = await _pythonService.AnalyzeAudioAsync(
                request.AudioFilePath,
                request.ModelName,
                request.TopK,
                request.SimilarityThreshold
            );

            stopwatch.Stop();

            if (!embeddingResult.Success)
            {
                _logger.LogError("Embedding extraction failed: {Error}", embeddingResult.Error);
                return StatusCode(500, new SimilaritySearchResult 
                { 
                    Success = false, 
                    Error = $"Embedding extraction failed: {embeddingResult.Error}" 
                });
            }

            if (!searchResult.Success)
            {
                _logger.LogError("Similarity search failed: {Error}", searchResult.Error);
                return StatusCode(500, searchResult);
            }

            searchResult.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Analysis completed in {ElapsedMs}ms. Found {TotalMatches} similar tracks with risk level: {RiskLevel}",
                stopwatch.ElapsedMilliseconds, searchResult.TotalMatches, searchResult.CopyrightRisk);

            return Ok(searchResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio analysis");
            return StatusCode(500, new SimilaritySearchResult 
            { 
                Success = false, 
                Error = $"Internal server error: {ex.Message}" 
            });
        }
    }

    /// <summary>
    /// Upload and analyze an audio file for copyright detection
    /// </summary>
    /// <param name="request">Upload request containing file and analysis parameters</param>
    /// <returns>Complete analysis results</returns>
    [HttpPost("upload-and-analyze")]
    [ProducesResponseType(typeof(AudioUploadResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AudioUploadResponse>> UploadAndAnalyze([FromForm] AudioUploadRequest request)
    {
        try
        {
            if (request.AudioFile == null || request.AudioFile.Length == 0)
            {
                return BadRequest("No audio file provided");
            }

            // Validate file type
            var allowedExtensions = new[] { ".wav", ".mp3", ".flac", ".m4a", ".ogg" };
            var fileExtension = Path.GetExtension(request.AudioFile.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest($"Unsupported file type: {fileExtension}. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            // Validate file size
            var maxFileSizeMB = _configuration.GetValue<int>("FileUpload:MaxFileSizeMB", 50);
            if (request.AudioFile.Length > maxFileSizeMB * 1024 * 1024)
            {
                return BadRequest($"File size exceeds {maxFileSizeMB}MB limit");
            }

            var stopwatch = Stopwatch.StartNew();

            // Save uploaded file
            var uploadsDir = _configuration.GetValue<string>("FileUpload:UploadPath", "./uploads");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.AudioFile.CopyToAsync(stream);
            }

            _logger.LogInformation("File uploaded: {FileName} ({FileSize} bytes)", fileName, request.AudioFile.Length);

            try
            {
                // Analyze the uploaded file
                var (embeddingResult, searchResult) = await _pythonService.AnalyzeAudioAsync(
                    filePath, request.ModelName, request.TopK, request.SimilarityThreshold);

                var response = new AudioUploadResponse
                {
                    Success = embeddingResult.Success && searchResult.Success,
                    FileInfo = new UploadedFileInfo
                    {
                        OriginalFilename = request.AudioFile.FileName,
                        SavedFilename = fileName,
                        FileSizeBytes = request.AudioFile.Length,
                        ContentType = request.AudioFile.ContentType,
                        UploadTimestamp = DateTime.UtcNow
                    },
                    EmbeddingResult = embeddingResult,
                    SearchResult = searchResult,
                    TotalProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };

                if (!embeddingResult.Success)
                {
                    response.Error = $"Embedding extraction failed: {embeddingResult.Error}";
                }
                else if (!searchResult.Success)
                {
                    response.Error = $"Similarity search failed: {searchResult.Error}";
                }

                return Ok(response);
            }
            finally
            {
                // Clean up uploaded file after processing
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Temporary file deleted: {FilePath}", filePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during upload and analysis");
            return StatusCode(500, new AudioUploadResponse 
            { 
                Success = false, 
                Error = $"Internal server error: {ex.Message}" 
            });
        }
    }

    /// <summary>
    /// Extract embeddings from an audio file
    /// </summary>
    /// <param name="request">Audio analysis request</param>
    /// <returns>Embedding extraction result</returns>
    [HttpPost("extract-embeddings")]
    [ProducesResponseType(typeof(EmbeddingResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<EmbeddingResult>> ExtractEmbeddings([FromBody] AudioAnalysisRequest request)
    {
        try
        {
            if (!System.IO.File.Exists(request.AudioFilePath))
            {
                return BadRequest($"Audio file not found: {request.AudioFilePath}");
            }

            _logger.LogInformation("Extracting embeddings for file: {AudioFile}", request.AudioFilePath);

            var result = await _pythonService.ExtractEmbeddingsAsync(request.AudioFilePath, request.ModelName);

            if (!result.Success)
            {
                return StatusCode(500, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting embeddings");
            return StatusCode(500, new EmbeddingResult 
            { 
                Success = false, 
                Error = $"Internal server error: {ex.Message}" 
            });
        }
    }

    /// <summary>
    /// Get API status and configuration information
    /// </summary>
    /// <returns>API status information</returns>
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new
        {
            Status = "Running",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow,
            PythonPath = _configuration["PythonConfig:PythonPath"],
            EmbeddingsRepo = _configuration["PythonConfig:EmbeddingsRepoPath"],
            VectorSearchRepo = _configuration["PythonConfig:VectorSearchRepoPath"],
            SupportedFormats = new[] { ".wav", ".mp3", ".flac", ".m4a", ".ogg" },
            SupportedModels = new[] { "spectrogram", "openl3", "audioclip" }
        });
    }
}
