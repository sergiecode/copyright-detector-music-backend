using Xunit;
using FluentAssertions;
using CopyrightDetector.MusicBackend.Models;

namespace CopyrightDetector.MusicBackend.Tests.Unit.Models;

/// <summary>
/// Unit tests for EmbeddingResult model
/// Created by Sergie Code - AI Tools for Musicians Series
/// </summary>
public class EmbeddingResultTests
{
    [Fact]
    public void EmbeddingResult_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var result = new EmbeddingResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Embeddings.Should().BeEmpty();
        result.Shape.Should().BeEmpty();
        result.Model.Should().BeEmpty();
        result.Error.Should().BeNull();
        result.Duration.Should().Be(0);
        result.SampleRate.Should().Be(0);
    }

    [Fact]
    public void EmbeddingResult_SuccessfulResult_ShouldHaveCorrectProperties()
    {
        // Arrange
        var embeddings = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 };
        var shape = new int[] { 5 };
        var model = "spectrogram";
        var duration = 120.5;
        var sampleRate = 22050;

        // Act
        var result = new EmbeddingResult
        {
            Success = true,
            Embeddings = embeddings,
            Shape = shape,
            Model = model,
            Duration = duration,
            SampleRate = sampleRate
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Embeddings.Should().BeEquivalentTo(embeddings);
        result.Shape.Should().BeEquivalentTo(shape);
        result.Model.Should().Be(model);
        result.Duration.Should().Be(duration);
        result.SampleRate.Should().Be(sampleRate);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void EmbeddingResult_FailedResult_ShouldHaveErrorMessage()
    {
        // Arrange
        var errorMessage = "Failed to extract embeddings";

        // Act
        var result = new EmbeddingResult
        {
            Success = false,
            Error = errorMessage
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        result.Embeddings.Should().BeEmpty();
        result.Shape.Should().BeEmpty();
    }

    [Theory]
    [InlineData(128)]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    [InlineData(2048)]
    public void EmbeddingResult_DifferentEmbeddingSizes_ShouldHandleCorrectly(int embeddingSize)
    {
        // Arrange
        var embeddings = new double[embeddingSize];
        for (int i = 0; i < embeddingSize; i++)
        {
            embeddings[i] = i * 0.001; // Some test values
        }
        var shape = new int[] { embeddingSize };

        // Act
        var result = new EmbeddingResult
        {
            Success = true,
            Embeddings = embeddings,
            Shape = shape,
            Model = "spectrogram"
        };

        // Assert
        result.Embeddings.Should().HaveCount(embeddingSize);
        result.Shape.Should().BeEquivalentTo(new[] { embeddingSize });
    }

    [Theory]
    [InlineData("spectrogram")]
    [InlineData("openl3")]
    [InlineData("audioclip")]
    public void EmbeddingResult_DifferentModels_ShouldStoreModelName(string modelName)
    {
        // Arrange & Act
        var result = new EmbeddingResult
        {
            Success = true,
            Model = modelName,
            Embeddings = new double[] { 0.1, 0.2, 0.3 },
            Shape = new int[] { 3 }
        };

        // Assert
        result.Model.Should().Be(modelName);
    }
}
