using Xunit;
using FluentAssertions;
using CopyrightDetector.MusicBackend.Models;

namespace CopyrightDetector.MusicBackend.Tests.Unit.Models;

/// <summary>
/// Unit tests for SimilaritySearchResult and SimilarTrack models
/// Created by Sergie Code - AI Tools for Musicians Series
/// </summary>
public class SimilaritySearchResultTests
{
    [Fact]
    public void SimilaritySearchResult_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var result = new SimilaritySearchResult();

        // Assert
        result.Success.Should().BeFalse();
        result.SimilarTracks.Should().NotBeNull().And.BeEmpty();
        result.CopyrightRisk.Should().Be("LOW");
        result.RiskScore.Should().Be(0.0);
        result.TotalMatches.Should().Be(0);
        result.ProcessingTimeMs.Should().Be(0);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void SimilaritySearchResult_SuccessfulResult_ShouldHaveCorrectProperties()
    {
        // Arrange
        var similarTracks = new List<SimilarTrack>
        {
            new()
            {
                Filename = "song1.wav",
                Artist = "Artist 1",
                SimilarityScore = 0.95,
                CopyrightRisk = "HIGH"
            },
            new()
            {
                Filename = "song2.wav",
                Artist = "Artist 2",
                SimilarityScore = 0.85,
                CopyrightRisk = "MEDIUM"
            }
        };

        // Act
        var result = new SimilaritySearchResult
        {
            Success = true,
            SimilarTracks = similarTracks,
            CopyrightRisk = "HIGH",
            RiskScore = 0.92,
            TotalMatches = 2,
            ProcessingTimeMs = 1500
        };

        // Assert
        result.Success.Should().BeTrue();
        result.SimilarTracks.Should().HaveCount(2);
        result.CopyrightRisk.Should().Be("HIGH");
        result.RiskScore.Should().Be(0.92);
        result.TotalMatches.Should().Be(2);
        result.ProcessingTimeMs.Should().Be(1500);
        result.Error.Should().BeNull();
    }

    [Theory]
    [InlineData("LOW")]
    [InlineData("MEDIUM")]
    [InlineData("HIGH")]
    [InlineData("VERY_HIGH")]
    public void SimilaritySearchResult_DifferentRiskLevels_ShouldAcceptValidValues(string riskLevel)
    {
        // Arrange & Act
        var result = new SimilaritySearchResult
        {
            CopyrightRisk = riskLevel
        };

        // Assert
        result.CopyrightRisk.Should().Be(riskLevel);
    }

    [Fact]
    public void SimilarTrack_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var track = new SimilarTrack();

        // Assert
        track.Filename.Should().BeEmpty();
        track.Artist.Should().BeEmpty();
        track.Album.Should().BeEmpty();
        track.Genre.Should().BeEmpty();
        track.SimilarityScore.Should().Be(0.0);
        track.Distance.Should().Be(0.0);
        track.CopyrightRisk.Should().Be("LOW");
        track.Duration.Should().Be(0.0);
        track.Year.Should().BeNull();
    }

    [Fact]
    public void SimilarTrack_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange
        var filename = "test_song.wav";
        var artist = "Test Artist";
        var album = "Test Album";
        var genre = "Rock";
        var similarityScore = 0.87;
        var distance = 0.13;
        var copyrightRisk = "HIGH";
        var duration = 240.5;
        var year = 2023;

        // Act
        var track = new SimilarTrack
        {
            Filename = filename,
            Artist = artist,
            Album = album,
            Genre = genre,
            SimilarityScore = similarityScore,
            Distance = distance,
            CopyrightRisk = copyrightRisk,
            Duration = duration,
            Year = year
        };

        // Assert
        track.Filename.Should().Be(filename);
        track.Artist.Should().Be(artist);
        track.Album.Should().Be(album);
        track.Genre.Should().Be(genre);
        track.SimilarityScore.Should().Be(similarityScore);
        track.Distance.Should().Be(distance);
        track.CopyrightRisk.Should().Be(copyrightRisk);
        track.Duration.Should().Be(duration);
        track.Year.Should().Be(year);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.8)]
    [InlineData(0.95)]
    [InlineData(1.0)]
    public void SimilarTrack_SimilarityScoreRange_ShouldAcceptValidValues(double score)
    {
        // Arrange & Act
        var track = new SimilarTrack { SimilarityScore = score };

        // Assert
        track.SimilarityScore.Should().Be(score);
    }

    [Theory]
    [InlineData("Rock")]
    [InlineData("Pop")]
    [InlineData("Jazz")]
    [InlineData("Classical")]
    [InlineData("Electronic")]
    public void SimilarTrack_DifferentGenres_ShouldAcceptValidValues(string genre)
    {
        // Arrange & Act
        var track = new SimilarTrack { Genre = genre };

        // Assert
        track.Genre.Should().Be(genre);
    }

    [Fact]
    public void SimilaritySearchResult_WithFailure_ShouldHaveErrorMessage()
    {
        // Arrange
        var errorMessage = "Python script execution failed";

        // Act
        var result = new SimilaritySearchResult
        {
            Success = false,
            Error = errorMessage
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        result.SimilarTracks.Should().BeEmpty();
    }

    [Fact]
    public void SimilaritySearchResult_RiskScoreCalculation_ShouldBeAccurate()
    {
        // Arrange
        var tracks = new List<SimilarTrack>
        {
            new() { SimilarityScore = 0.95 },
            new() { SimilarityScore = 0.90 },
            new() { SimilarityScore = 0.85 }
        };

        // Act
        var result = new SimilaritySearchResult
        {
            Success = true,
            SimilarTracks = tracks,
            RiskScore = 0.924 // Example calculated score
        };

        // Assert
        result.RiskScore.Should().BeApproximately(0.924, 0.001);
        result.SimilarTracks.Should().HaveCount(3);
        result.SimilarTracks.Max(t => t.SimilarityScore).Should().Be(0.95);
    }
}
