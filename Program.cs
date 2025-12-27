using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace WhisperTranscriber;

class Program
{
    private static readonly string apiKey = Environment.GetEnvironmentVariable("API-KEY") || "";

    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Whisper Audio Transcriber ===");

        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Error: API key is not set. Please set the API-KEY environment variable.");
            return;
        }

        Console.Write("Enter the path to the audio file: ");
        string filePath = Console.ReadLine();

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Error: File does not exist.");
            return;
        }

        try
        {
            Console.WriteLine("\nTranscribing audio...");
            string transcription = await TranscribeAudioAsync(filePath, apiKey);

            Console.WriteLine("Transcription Result:");
            Console.WriteLine(transcription);

            string outputFilePath = Path.ChangeExtension(filePath, ".txt");
            await File.WriteAllTextAsync(outputFilePath, transcription);
            Console.WriteLine($"\nTranscription saved to: {outputFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task<string> TranscribeAudioAsync(string filePath, string apiKey)
    {
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(filePath);

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        form.Add(fileContent, "file", Path.GetFileName(filePath));
        form.Add(new StringContent("whisper-1"), "model");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"API request failed with status code {response.StatusCode}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(jsonResponse);
        return document.RootElement.GetProperty("text").GetString();
    }

}