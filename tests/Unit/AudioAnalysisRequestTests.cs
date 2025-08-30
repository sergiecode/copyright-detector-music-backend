using Xunit;
using FluentAssertions;
using CopyrightDetector.MusicBackend.Models;

namespace CopyrightDetector.MusicBackend.Tests.Unit.Models;

/// <summary>
/// Unit tests for AudioAnalysisRequest model
/// Created by Sergie Code - AI Tools for Musicians Series
/// </summary>
public class AudioAnalysisRequestTests
{
    [Fact]
    public void AudioAnalysisRequest_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var request = new AudioAnalysisRequest();

        // Assert
        request.AudioFilePath.Should().BeEmpty();
        request.ModelName.Should().Be("spectrogram");
        request.TopK.Should().Be(10);
        request.SimilarityThreshold.Should().Be(0.8);
    }

    [Fact]
    public void AudioAnalysisRequest_WithCustomValues_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var audioPath = "C:\\music\\test.wav";
        var modelName = "openl3";
        var topK = 15;
        var threshold = 0.75;

        // Act
        var request = new AudioAnalysisRequest
        {
            AudioFilePath = audioPath,
            ModelName = modelName,
            TopK = topK,
            SimilarityThreshold = threshold
        };

        // Assert
        request.AudioFilePath.Should().Be(audioPath);
        request.ModelName.Should().Be(modelName);
        request.TopK.Should().Be(topK);
        request.SimilarityThreshold.Should().Be(threshold);
    }

    [Theory]
    [InlineData("spectrogram")]
    [InlineData("openl3")]
    [InlineData("audioclip")]
    public void AudioAnalysisRequest_ValidModelNames_ShouldAcceptAllSupportedModels(string modelName)
    {
        // Arrange & Act
        var request = new AudioAnalysisRequest { ModelName = modelName };

        // Assert
        request.ModelName.Should().Be(modelName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void AudioAnalysisRequest_ValidTopKValues_ShouldAcceptReasonableRanges(int topK)
    {
        // Arrange & Act
        var request = new AudioAnalysisRequest { TopK = topK };

        // Assert
        request.TopK.Should().Be(topK);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.8)]
    [InlineData(0.95)]
    [InlineData(1.0)]
    public void AudioAnalysisRequest_ValidThresholdValues_ShouldAcceptValidRange(double threshold)
    {
        // Arrange & Act
        var request = new AudioAnalysisRequest { SimilarityThreshold = threshold };

        // Assert
        request.SimilarityThreshold.Should().Be(threshold);
    }
}
