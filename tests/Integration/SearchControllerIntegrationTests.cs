using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using CopyrightDetector.MusicBackend.Models;

namespace CopyrightDetector.MusicBackend.Tests.Integration;

/// <summary>
/// Integration tests for SearchController
/// Created by Sergie Code - AI Tools for Musicians Series
/// </summary>
public class SearchControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SearchControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Should().BeSuccessful();
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
        content.Should().Contain("Copyright Detector Music Backend");
    }

    [Fact]
    public async Task StatusEndpoint_ShouldReturnApiInformation()
    {
        // Act
        var response = await _client.GetAsync("/api/search/status");

        // Assert
        response.Should().BeSuccessful();
        
        var content = await response.Content.ReadAsStringAsync();
        var statusResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        statusResponse.GetProperty("Status").GetString().Should().Be("Running");
        statusResponse.GetProperty("Version").GetString().Should().Be("1.0.0");
        
        // Should have supported formats
        var supportedFormats = statusResponse.GetProperty("SupportedFormats");
        supportedFormats.GetArrayLength().Should().BeGreaterThan(0);
        
        // Should have supported models
        var supportedModels = statusResponse.GetProperty("SupportedModels");
        supportedModels.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AnalyzeEndpoint_WithInvalidPath_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new AudioAnalysisRequest
        {
            AudioFilePath = "nonexistent_file.wav",
            ModelName = "spectrogram",
            TopK = 10,
            SimilarityThreshold = 0.8
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search/analyze", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task ExtractEmbeddingsEndpoint_WithInvalidPath_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new AudioAnalysisRequest
        {
            AudioFilePath = "nonexistent_file.wav",
            ModelName = "spectrogram"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search/extract-embeddings", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AnalyzeEndpoint_WithEmptyPath_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new AudioAnalysisRequest
        {
            AudioFilePath = "",
            ModelName = "spectrogram"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search/analyze", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAndAnalyzeEndpoint_WithoutFile_ShouldReturnBadRequest()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("spectrogram"), "modelName");
        form.Add(new StringContent("10"), "topK");

        // Act
        var response = await _client.PostAsync("/api/search/upload-and-analyze", form);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("AudioFile");
        content.Should().Contain("required");
    }

    [Fact]
    public async Task UploadAndAnalyzeEndpoint_WithInvalidFileType_ShouldReturnBadRequest()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "audioFile", "test.txt");
        form.Add(new StringContent("spectrogram"), "modelName");

        // Act
        var response = await _client.PostAsync("/api/search/upload-and-analyze", form);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Unsupported file type");
    }

    [Theory]
    [InlineData("test.wav")]
    [InlineData("test.mp3")]
    [InlineData("test.flac")]
    [InlineData("test.m4a")]
    [InlineData("test.ogg")]
    public async Task UploadAndAnalyzeEndpoint_WithSupportedFileTypes_ShouldAcceptFile(string filename)
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        
        // Create a small fake audio file (will fail processing but should pass validation)
        var fakeAudioData = new byte[1024]; // 1KB fake audio
        for (int i = 0; i < fakeAudioData.Length; i++)
        {
            fakeAudioData[i] = (byte)(i % 256);
        }
        
        var fileContent = new ByteArrayContent(fakeAudioData);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        form.Add(fileContent, "audioFile", filename);
        form.Add(new StringContent("spectrogram"), "modelName");
        form.Add(new StringContent("5"), "topK");

        // Act
        var response = await _client.PostAsync("/api/search/upload-and-analyze", form);

        // Assert
        // The request should be accepted (not 400 Bad Request for file type)
        // It will likely fail during processing due to fake audio data, but that's expected
        response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAndAnalyzeEndpoint_WithOversizedFile_ShouldReturnBadRequest()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        
        // Create a file larger than 50MB (default limit)
        var oversizedData = new byte[51 * 1024 * 1024]; // 51MB
        
        var fileContent = new ByteArrayContent(oversizedData);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        form.Add(fileContent, "audioFile", "large_file.wav");
        form.Add(new StringContent("spectrogram"), "modelName");

        // Act
        var response = await _client.PostAsync("/api/search/upload-and-analyze", form);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("exceeds");
    }

    [Theory]
    [InlineData("spectrogram")]
    [InlineData("openl3")]
    [InlineData("audioclip")]
    public async Task StatusEndpoint_ShouldIncludeSupportedModel(string modelName)
    {
        // Act
        var response = await _client.GetAsync("/api/search/status");
        var content = await response.Content.ReadAsStringAsync();
        var statusResponse = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        var supportedModels = statusResponse.GetProperty("SupportedModels");
        var models = supportedModels.EnumerateArray().Select(x => x.GetString()).ToList();
        
        models.Should().Contain(modelName);
    }

    [Theory]
    [InlineData(".wav")]
    [InlineData(".mp3")]
    [InlineData(".flac")]
    [InlineData(".m4a")]
    [InlineData(".ogg")]
    public async Task StatusEndpoint_ShouldIncludeSupportedFormat(string format)
    {
        // Act
        var response = await _client.GetAsync("/api/search/status");
        var content = await response.Content.ReadAsStringAsync();
        var statusResponse = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        var supportedFormats = statusResponse.GetProperty("SupportedFormats");
        var formats = supportedFormats.EnumerateArray().Select(x => x.GetString()).ToList();
        
        formats.Should().Contain(format);
    }
}
