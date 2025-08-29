using System.Net.Http.Json;
using CopyrightDetector.MusicBackend.Models;

namespace CopyrightDetector.MusicBackend.Examples;

/// <summary>
/// Example client demonstrating how to use the Copyright Detector Music Backend API
/// Created by Sergie Code - AI Tools for Musicians Series
/// </summary>
public class ExampleClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ExampleClient(string baseUrl = "https://localhost:7000")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Test the API health and status
    /// </summary>
    public async Task TestHealthAndStatusAsync()
    {
        Console.WriteLine("🔍 Testing API Health and Status...\n");

        try
        {
            // Test health endpoint
            var healthResponse = await _httpClient.GetAsync($"{_baseUrl}/health");
            if (healthResponse.IsSuccessStatusCode)
            {
                var healthContent = await healthResponse.Content.ReadAsStringAsync();
                Console.WriteLine("✅ Health Check: " + healthContent);
            }
            else
            {
                Console.WriteLine("❌ Health Check Failed");
            }

            // Test status endpoint
            var statusResponse = await _httpClient.GetAsync($"{_baseUrl}/api/search/status");
            if (statusResponse.IsSuccessStatusCode)
            {
                var statusContent = await statusResponse.Content.ReadAsStringAsync();
                Console.WriteLine("✅ API Status: " + statusContent);
            }
            else
            {
                Console.WriteLine("❌ API Status Check Failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error testing health/status: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Example: Extract embeddings from an audio file
    /// </summary>
    public async Task ExtractEmbeddingsExampleAsync(string audioFilePath)
    {
        Console.WriteLine($"🎵 Extracting embeddings from: {audioFilePath}\n");

        try
        {
            var request = new AudioAnalysisRequest
            {
                AudioFilePath = audioFilePath,
                ModelName = "spectrogram"
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/search/extract-embeddings", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EmbeddingResult>();
                
                if (result?.Success == true)
                {
                    Console.WriteLine("✅ Embedding extraction successful!");
                    Console.WriteLine($"   Model: {result.Model}");
                    Console.WriteLine($"   Shape: [{string.Join(", ", result.Shape)}]");
                    Console.WriteLine($"   Duration: {result.Duration:F2} seconds");
                    Console.WriteLine($"   Sample Rate: {result.SampleRate} Hz");
                    Console.WriteLine($"   Embeddings preview: [{string.Join(", ", result.Embeddings.Take(5).Select(e => e.ToString("F3")))}...]");
                }
                else
                {
                    Console.WriteLine($"❌ Embedding extraction failed: {result?.Error}");
                }
            }
            else
            {
                Console.WriteLine($"❌ HTTP Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error extracting embeddings: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Example: Analyze audio file for copyright similarities
    /// </summary>
    public async Task AnalyzeAudioExampleAsync(string audioFilePath)
    {
        Console.WriteLine($"⚖️ Analyzing copyright similarities for: {audioFilePath}\n");

        try
        {
            var request = new AudioAnalysisRequest
            {
                AudioFilePath = audioFilePath,
                ModelName = "spectrogram",
                TopK = 10,
                SimilarityThreshold = 0.8
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/search/analyze", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SimilaritySearchResult>();
                
                if (result?.Success == true)
                {
                    Console.WriteLine("✅ Copyright analysis completed!");
                    Console.WriteLine($"   Copyright Risk: {result.CopyrightRisk}");
                    Console.WriteLine($"   Risk Score: {result.RiskScore:F3}");
                    Console.WriteLine($"   Total Matches: {result.TotalMatches}");
                    Console.WriteLine($"   Processing Time: {result.ProcessingTimeMs}ms");
                    
                    if (result.SimilarTracks.Any())
                    {
                        Console.WriteLine("\n📊 Top Similar Tracks:");
                        foreach (var track in result.SimilarTracks.Take(5))
                        {
                            Console.WriteLine($"   • {track.Filename}");
                            Console.WriteLine($"     Artist: {track.Artist}");
                            Console.WriteLine($"     Similarity: {track.SimilarityScore:P1}");
                            Console.WriteLine($"     Risk: {track.CopyrightRisk}");
                            Console.WriteLine();
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Analysis failed: {result?.Error}");
                }
            }
            else
            {
                Console.WriteLine($"❌ HTTP Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error analyzing audio: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Example: Upload and analyze an audio file
    /// </summary>
    public async Task UploadAndAnalyzeExampleAsync(string audioFilePath)
    {
        Console.WriteLine($"📤 Uploading and analyzing: {audioFilePath}\n");

        try
        {
            if (!File.Exists(audioFilePath))
            {
                Console.WriteLine($"❌ File not found: {audioFilePath}");
                return;
            }

            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(audioFilePath);
            using var fileContent = new StreamContent(fileStream);
            
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            form.Add(fileContent, "audioFile", Path.GetFileName(audioFilePath));
            form.Add(new StringContent("spectrogram"), "modelName");
            form.Add(new StringContent("10"), "topK");
            form.Add(new StringContent("0.8"), "similarityThreshold");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/search/upload-and-analyze", form);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AudioUploadResponse>();
                
                if (result?.Success == true)
                {
                    Console.WriteLine("✅ Upload and analysis completed!");
                    Console.WriteLine($"   Original Filename: {result.FileInfo.OriginalFilename}");
                    Console.WriteLine($"   File Size: {result.FileInfo.FileSizeBytes / 1024.0:F1} KB");
                    Console.WriteLine($"   Total Processing Time: {result.TotalProcessingTimeMs}ms");
                    
                    if (result.SearchResult != null)
                    {
                        Console.WriteLine($"   Copyright Risk: {result.SearchResult.CopyrightRisk}");
                        Console.WriteLine($"   Risk Score: {result.SearchResult.RiskScore:F3}");
                        Console.WriteLine($"   Similar Tracks Found: {result.SearchResult.TotalMatches}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Upload/analysis failed: {result?.Error}");
                }
            }
            else
            {
                Console.WriteLine($"❌ HTTP Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error uploading/analyzing: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Run all examples
    /// </summary>
    public async Task RunAllExamplesAsync()
    {
        Console.WriteLine("🎵 Copyright Detector Music Backend - Example Client");
        Console.WriteLine("Created by Sergie Code - AI Tools for Musicians Series");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine();

        // Test health and status
        await TestHealthAndStatusAsync();

        // Note: These examples require actual audio files to work
        // Replace with actual file paths in your environment
        var exampleAudioFile = @"C:\music\test_song.wav";
        
        Console.WriteLine("📝 Note: The following examples require actual audio files.");
        Console.WriteLine($"   Please ensure you have an audio file at: {exampleAudioFile}");
        Console.WriteLine("   Or modify the path to point to an existing audio file.");
        Console.WriteLine();

        if (File.Exists(exampleAudioFile))
        {
            // Example 1: Extract embeddings
            await ExtractEmbeddingsExampleAsync(exampleAudioFile);

            // Example 2: Analyze for copyright
            await AnalyzeAudioExampleAsync(exampleAudioFile);

            // Example 3: Upload and analyze
            await UploadAndAnalyzeExampleAsync(exampleAudioFile);
        }
        else
        {
            Console.WriteLine("⚠️ Skipping audio file examples - test audio file not found.");
            Console.WriteLine("   To test with audio files:");
            Console.WriteLine("   1. Place an audio file (.wav, .mp3, .flac) in C:\\music\\test_song.wav");
            Console.WriteLine("   2. Or modify the exampleAudioFile variable in the code");
            Console.WriteLine();
        }

        Console.WriteLine("✅ Example client demonstration completed!");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
