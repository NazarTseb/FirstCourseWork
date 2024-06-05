using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Youtube_API
{
    public class YouTubeServiceHelper
    {
        private static readonly string _apiKey = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY");

        private static readonly string _clientSecretsJson = $@"
        {{
            ""installed"": {{
                ""client_id"": ""{Environment.GetEnvironmentVariable("439647084462-fcv02mkbrga52bsjjt7tbhbqf4c5v2tf.apps.googleusercontent.com")}"",
                ""project_id"": ""my-youtube-api-project-422006"",
                ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                ""token_uri"": ""https://oauth2.googleapis.com/token"",
                ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                ""client_secret"": ""{Environment.GetEnvironmentVariable("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")}"",
                ""redirect_uris"": [""urn:ietf:wg:oauth:2.0:oob"", ""http://localhost""]
            }}
        }}";
        private static UserCredential _credential;

        static YouTubeServiceHelper()
        {
            InitializeCredentials().GetAwaiter().GetResult();
        }

        private static async Task InitializeCredentials()
        {
            var clientSecrets = GoogleClientSecrets.FromStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_clientSecretsJson)));
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                new[] { YouTubeService.Scope.Youtube },
                "user",
                CancellationToken.None,
                new FileDataStore("YoutubeTelegramBot")
            );

            if (_credential == null)
            {
                throw new Exception("Failed to load YouTube credentials.");
            }
        }

        private static YouTubeService CreateYouTubeService()
        {
            return new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApiKey = _apiKey,
                ApplicationName = "YoutubeTelegramBot"
            });
        }

        public static async Task<IEnumerable<string>> SearchVideos(string query)
        {
            try
            {
                var youtubeService = CreateYouTubeService();
                var searchListRequest = youtubeService.Search.List("snippet");
                searchListRequest.Q = query;
                searchListRequest.MaxResults = 5;

                var searchListResponse = await searchListRequest.ExecuteAsync();
                var videos = searchListResponse.Items
                    .Where(item => item.Id.Kind == "youtube#video")
                    .Select(item => $"{item.Snippet.Title} ({item.Id.VideoId})")
                    .ToList();

                return videos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchVideos: {ex.Message}");
                return new List<string> { $"Error: {ex.Message}" };
            }
        }

        public static async Task<string> GetVideoInfo(string videoId)
        {
            try
            {
                var youtubeService = CreateYouTubeService();
                var videoRequest = youtubeService.Videos.List("snippet,contentDetails,statistics");
                videoRequest.Id = videoId;

                var videoResponse = await videoRequest.ExecuteAsync();
                if (videoResponse.Items.Count == 0)
                {
                    return $"Video with ID '{videoId}' not found.";
                }

                var video = videoResponse.Items[0];
                return FormatVideoInfo(video);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetVideoInfo: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private static string FormatVideoInfo(Google.Apis.YouTube.v3.Data.Video video)
        {
            var snippet = video.Snippet;
            var contentDetails = video.ContentDetails;
            var statistics = video.Statistics;

            return $@"
Title: {snippet.Title}

Description: {snippet.Description}

Published At: {snippet.PublishedAtDateTimeOffset}

Channel Title: {snippet.ChannelTitle}

Tags: {string.Join(", ", snippet.Tags ?? Array.Empty<string>())}

View Count: {statistics.ViewCount}
Like Count: {statistics.LikeCount}
Dislike Count: {statistics.DislikeCount}
Comment Count: {statistics.CommentCount}
";
        }

        public static async Task<string> CreatePlaylist(string playlistTitle, string videoId)
        {
            try
            {
                var youtubeService = CreateYouTubeService();
                var newPlaylist = new Playlist
                {
                    Snippet = new PlaylistSnippet
                    {
                        Title = playlistTitle,
                        Description = "A playlist created with the YouTube API v3"
                    },
                    Status = new PlaylistStatus
                    {
                        PrivacyStatus = "public"
                    }
                };

                newPlaylist = await youtubeService.Playlists.Insert(newPlaylist, "snippet,status").ExecuteAsync();

                var newPlaylistItem = new PlaylistItem
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        PlaylistId = newPlaylist.Id,
                        ResourceId = new ResourceId
                        {
                            Kind = "youtube#video",
                            VideoId = videoId
                        }
                    }
                };

                newPlaylistItem = await youtubeService.PlaylistItems.Insert(newPlaylistItem, "snippet").ExecuteAsync();

                return $"Playlist was successfully added. (playlist id: {newPlaylist.Id})";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreatePlaylist: {ex.Message}");
                return $"Internal server error: {ex.Message}";
            }
        }

        public static async Task<string> UpdatePlaylistTitle(string playlistId, string newTitle)
        {
            try
            {
                var youtubeService = CreateYouTubeService();
                var playlist = new Playlist
                {
                    Id = playlistId,
                    Snippet = new PlaylistSnippet
                    {
                        Title = newTitle
                    }
                };

                var updatePlaylistRequest = youtubeService.Playlists.Update(playlist, "snippet");
                await updatePlaylistRequest.ExecuteAsync();

                return $"Playlist Title updated to: {newTitle}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdatePlaylistTitle: {ex.Message}");
                return $"Internal server error: {ex.Message}";
            }
        }

        public static async Task<string> DeletePlaylist(string playlistId)
        {
            try
            {
                var youtubeService = CreateYouTubeService();
                await youtubeService.Playlists.Delete(playlistId).ExecuteAsync();
                return $"Playlist with ID: {playlistId} deleted successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeletePlaylist: {ex.Message}");
                return $"Internal server error: {ex.Message}";
            }
        }
    }
}
