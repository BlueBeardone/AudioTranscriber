using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using DotNetEnv;

namespace WhisperTranscriber;

class Program
{
    //private static readonly string apiKey = Environment.GetEnvironmentVariable("API-KEY");

    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        Env.Load(); 

        Console.WriteLine("=== Whisper Audio Transcriber ===");

        string apiKey = Environment.GetEnvironmentVariable("API-KEY");

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
        

        string model = Environment.GetEnvironmentVariable("WHISPER_MODEL") ?? "whisper-1";
        form.Add(new StringContent(model), "model");

        string language = Environment.GetEnvironmentVariable("TRANSCRIPTION_LANGUAGE") ?? "en";
        if (!string.IsNullOrEmpty(language))
        {
            form.Add(new StringContent(language), "language");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API request failed with status code {response.StatusCode}: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        return jsonDoc.RootElement.GetProperty("text").GetString();
    }

}