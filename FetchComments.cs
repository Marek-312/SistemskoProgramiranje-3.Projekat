using System;
using System.Collections.Generic;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace treciProjekat
{
    public class YouTubeExample
    {
        public static async Task SearchVideosAsync()
        {
            // Replace with your actual API Key
            string apiKey = "AIzaSyDAbxO1dUqzgiv7ZXtESVfYAWdxYbtixfk";

            // Initialize the YouTube Service
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "YouTube Search Example"
            });

            // Create the search request
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = "C# .NET Tutorial"; // Replace with your search term
            searchListRequest.MaxResults = 5; // Limit results for simplicity

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