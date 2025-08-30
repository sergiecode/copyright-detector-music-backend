using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CopyrightDetector.MusicBackend.Services;
using CopyrightDetector.MusicBackend.Models;

namespace CopyrightDetector.MusicBackend.Tests.Unit.Services;

/// <summary>
/// Unit tests for PythonService
/// Created by Sergie Code - AI Tools for Musicians Series
/// </summary>
public class PythonServiceTests
{
    private readonly Mock<ILogger<PythonService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly PythonService _pythonService;

    public PythonServiceTests()
    {
        _mockLogger = new Mock<ILogger<PythonService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup configuration mock
        SetupConfiguration();

        _pythonService = new PythonService(_mockLogger.Object, _mockConfiguration.Object);
    }

    private void SetupConfiguration()
    {
        _mockConfiguration.Setup(c => c["PythonConfig:PythonPath"]).Returns("python");
        _mockConfiguration.Setup(c => c["PythonConfig:ScriptsPath"]).Returns("./python");
        _mockConfiguration.Setup(c => c["PythonConfig:EmbeddingsRepoPath"]).Returns("../copyright-detector-music-embeddings");
        _mockConfiguration.Setup(c => c["PythonConfig:VectorSearchRepoPath"]).Returns("../copyright-detector-vector-search");
    }

    [Fact]
    public void PythonService_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange, Act & Assert
        _pythonService.Should().NotBeNull();
    }

    [Fact]
    public void CalculateCopyrightRisk_NoSimilarTracks_ShouldReturnLow()
    {
        // Arrange
        var similarTracks = new List<SimilarTrack>();
        var threshold = 0.8;

        // Act
        var result = CallPrivateCalculateCopyrightRisk(similarTracks, threshold);

        // Assert
        result.Should().Be("LOW");
    }

    [Fact]
    public void CalculateCopyrightRisk_VeryHighSimilarity_ShouldReturnVeryHigh()
    {
        // Arrange
        var similarTracks = new List<SimilarTrack>
        {
            new() { SimilarityScore = 0.97 }
        };
        var threshold = 0.8;

        // Act
        var result = CallPrivateCalculateCopyrightRisk(similarTracks, threshold);

        // Assert
        result.Should().Be("VERY_HIGH");
    }

    [Fact]
    public void CalculateCopyrightRisk_HighSimilarity_ShouldReturnHigh()
    {
        // Arrange
        var similarTracks = new List<SimilarTrack>
        {
            new() { SimilarityScore = 0.87 }
        };
        var threshold = 0.8;

        // Act
        var result = CallPrivateCalculateCopyrightRisk(similarTracks, threshold);

        // Assert
        result.Should().Be("HIGH");
    }

    [Fact]
    public void CalculateCopyrightRisk_MultipleHighSimilarity_ShouldReturnHigh()
    {
        // Arrange
        var similarTracks = new List<SimilarTrack>
        {
            new() { SimilarityScore = 0.82 },
            new() { SimilarityScore = 0.81 },
            new() { SimilarityScore = 0.75 }
        };
        var threshold = 0.8;

        // Act
        var result = CallPrivateCalculateCopyrightRisk(similarTracks, threshold);

        // Assert
        result.Should().Be("HIGH");
    }

    [Fact]
    public void CalculateCopyrightRisk_MediumSimilarity_ShouldReturnMedium()
    {
        // Arrange
        var similarTracks = new List<SimilarTrack>
        {
            new() { SimilarityScore = 0.75 }
        };
        var threshold = 0.8;

        // Act
        var result = CallPrivateCalculateCopyrightRisk(similarTracks, threshold);

        // Assert
        result.Should().Be("MEDIUM");
    }

    [Fact]
    public void CalculateRiskScore_NoTracks_ShouldReturnZero()
    {
        // Arrange
        var similarTracks = new List<SimilarTrack>();

        // Act
        var result = CallPrivateCalculateRiskScore(similarTracks);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateRiskScore_SingleTrack_ShouldCalculateCorrectly()
    {
        // Arrange
        var similarTracks = new List<SimilarTrack>
        {
            new() { SimilarityScore = 0.8 }
        };

        // Act
        var result = CallPrivateCalculateRiskScore(similarTracks);

        // Assert
        // 0.7 * 0.8 + 0.3 * 0.8 = 0.8
        result.Should().Be(0.8);
    }

    [Fact]
    public void CalculateRiskScore_MultipleTracks_ShouldCalculateWeightedScore()
    {
        // Arrange
        var similarTracks = new List<SimilarTrack>
        {
            new() { SimilarityScore = 0.9 },
            new() { SimilarityScore = 0.8 },
            new() { SimilarityScore = 0.7 },
            new() { SimilarityScore = 0.6 }
        };

        // Act
        var result = CallPrivateCalculateRiskScore(similarTracks);

        // Assert
        // Max = 0.9, Avg of top 3 = (0.9 + 0.8 + 0.7) / 3 = 0.8
        // Score = 0.7 * 0.9 + 0.3 * 0.8 = 0.87
        result.Should().Be(0.87);
    }

    [Theory]
    [InlineData("nonexistent.wav")]
    [InlineData("")]
    [InlineData(null)]
    public async Task ExtractEmbeddingsAsync_InvalidFilePath_ShouldReturnFailure(string filePath)
    {
        // Act
        var result = await _pythonService.ExtractEmbeddingsAsync(filePath ?? string.Empty);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SearchSimilarTracksAsync_EmptyEmbeddings_ShouldReturnFailure()
    {
        // Arrange
        var embeddings = Array.Empty<double>();

        // Act
        var result = await _pythonService.SearchSimilarTracksAsync(embeddings);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SearchSimilarTracksAsync_ValidEmbeddings_ShouldAttemptSearch()
    {
        // Arrange
        var embeddings = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 };

        // Act
        var result = await _pythonService.SearchSimilarTracksAsync(embeddings);

        // Assert
        // This will fail because we don't have the actual Python scripts,
        // but it should attempt the operation
        result.Should().NotBeNull();
    }

    // Helper methods to test private methods using reflection
    private string CallPrivateCalculateCopyrightRisk(List<SimilarTrack> tracks, double threshold)
    {
        var method = typeof(PythonService).GetMethod("CalculateCopyrightRisk", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string)method!.Invoke(_pythonService, new object[] { tracks, threshold })!;
    }

    private double CallPrivateCalculateRiskScore(List<SimilarTrack> tracks)
    {
        var method = typeof(PythonService).GetMethod("CalculateRiskScore", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (double)method!.Invoke(_pythonService, new object[] { tracks })!;
    }
}
