using System;
using System.Collections.Generic;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;

namespace treciProjekat
{
    public class YouTubeExample
    {
        public static async Task SearchVideosAsync()
        {
            // Čitaj API key iz appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            string apiKey = config["YouTube:ApiKey"]
                ?? throw new Exception("API key nije pronađen u appsettings.json!");

            // Initialize the YouTube Service
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "YouTube Search Example"
            });

            // Create the search request
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = "C# .NET Tutorial";
            searchListRequest.MaxResults = 5;

            // Execute the request
            var searchListResponse = await searchListRequest.ExecuteAsync();

            // Process results
            List<string> videos = new List<string>();
            foreach (var searchResult in searchListResponse.Items)
            {
                if (searchResult.Id.Kind == "youtube#video")
                {
                    videos.Add($"{searchResult.Snippet.Title} ({searchResult.Id.VideoId})");
                }
            }

            Console.WriteLine("Search Results:");
            foreach (var video in videos)
            {
                Console.WriteLine(video);
            }
        }
    }
}