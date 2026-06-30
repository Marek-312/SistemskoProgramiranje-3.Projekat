using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;

namespace treciProjekat
{
    public class YouTubeExample
    {
        // Metoda sada prima videoId za koji želiš komentare i vraća listu tekstova
        public static async Task<List<string>> GetVideoCommentsAsync(string videoId)
        {
            // Čitaj API key iz appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            string apiKey = config["YouTube:ApiKey"]
                ?? throw new Exception("API key nije pronađen u appsettings.json!");

            // Inicijalizacija YouTube servisa
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "YouTube Sentiment Analyzer"
            });

            // Kreiranje zahteva za komentare (CommentThreads) umesto Search-a
            var commentListRequest = youtubeService.CommentThreads.List("snippet");
            commentListRequest.VideoId = videoId; // Tražimo komentare za konkretan video
            commentListRequest.MaxResults = 50;   // Broj komentara po stranici (maksimum je 100)

            List<string> komentari = new List<string>();

            try
            {
                // Izvršavanje API zahteva
                var commentListResponse = await commentListRequest.ExecuteAsync();

                // Prolazak kroz pristigle komentare i izvlačenje čistog teksta
                foreach (var thread in commentListResponse.Items)
                {
                    var komentarTekst = thread.Snippet.TopLevelComment.Snippet.TextDisplay;
                    if (!string.IsNullOrWhiteSpace(komentarTekst))
                    {
                        komentari.Add(komentarTekst);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom povlačenja komentara sa YouTube-a: {ex.Message}");
            }

            return komentari;
        }
    }
}