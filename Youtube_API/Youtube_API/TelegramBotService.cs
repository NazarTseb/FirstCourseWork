using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace Youtube_API
{
    public class TelegramBotService
    {
        private static readonly string _botToken = "6961710064:AAFN6w1D5G775JKYZeeq6dfzvBN-QqaNsZo";
        private ITelegramBotClient _botClient;

        public TelegramBotService()
        {
            _botClient = new TelegramBotClient(_botToken);
        }

        public async Task StartAsync()
        {
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"{me.Username} is running. . .");

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();

            cts.Cancel();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update?.Message is not { } message)
                {
                    Console.WriteLine("Update or Message is null.");
                    return;
                }

                var chatId = message.Chat.Id;

                if (message.Type != MessageType.Text)
                {
                    Console.WriteLine("Message type is not text.");
                    return;
                }

                var messageText = message.Text;

                if (messageText == null)
                {
                    Console.WriteLine("Message text is null.");
                    return;
                }

                if (messageText.Equals("/start"))
                {
                    var welcomeMessage = @"Welcome to YouTube Telegram Bot!

Here are the commands you can use:

/search <query> - Search for YouTube videos by query
/videoinfo <VideoID> - Get full information about a YouTube video
/createplaylist <PlaylistTitle> <VideoID> - Create a playlist with a video
/updateplaylist <PlaylistId> <NewTitle> - Update the title of a playlist
/deleteplaylist <PlaylistId> - Delete a playlist";
                    await botClient.SendTextMessageAsync(chatId, welcomeMessage, cancellationToken: cancellationToken);
                }
                else if (messageText.StartsWith("/search "))
                {
                    var query = messageText.Substring(8);
                    var videos = await YouTubeServiceHelper.SearchVideos(query);
                    await botClient.SendTextMessageAsync(chatId, string.Join("\n\n", videos), cancellationToken: cancellationToken);
                }
                else if (messageText.StartsWith("/videoinfo "))
                {
                    var videoId = messageText.Substring(11);
                    var videoInfo = await YouTubeServiceHelper.GetVideoInfo(videoId);
                    await SendLongMessage(botClient, chatId, videoInfo, cancellationToken);
                }
                else if (messageText.StartsWith("/createplaylist "))
                {
                    var args = messageText.Substring(16).Split(' ');
                    if (args.Length < 2)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Usage: /createplaylist <PlaylistTitle> <VideoID>", cancellationToken: cancellationToken);
                        return;
                    }
                    var title = args[0];
                    var videoId = args[1];
                    var result = await YouTubeServiceHelper.CreatePlaylist(title, videoId);
                    await botClient.SendTextMessageAsync(chatId, result, cancellationToken: cancellationToken);
                }
                else if (messageText.StartsWith("/updateplaylist "))
                {
                    var args = messageText.Substring(16).Split(' ');
                    if (args.Length < 2)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Usage: /updateplaylist <PlaylistId> <NewTitle>", cancellationToken: cancellationToken);
                        return;
                    }
                    var playlistId = args[0];
                    var newTitle = args[1];
                    var result = await YouTubeServiceHelper.UpdatePlaylistTitle(playlistId, newTitle);
                    await botClient.SendTextMessageAsync(chatId, result, cancellationToken: cancellationToken);
                }
                else if (messageText.StartsWith("/deleteplaylist "))
                {
                    var playlistId = messageText.Substring(16);
                    var result = await YouTubeServiceHelper.DeletePlaylist(playlistId);
                    await botClient.SendTextMessageAsync(chatId, result, cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Unknown command.", cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleUpdateAsync: {ex.Message}");
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Error: {exception.Message}");
            return Task.CompletedTask;
        }

        private static async Task SendLongMessage(ITelegramBotClient botClient, long chatId, string message, CancellationToken cancellationToken)
        {
            const int MaxMessageLength = 4096;
            if (message.Length <= MaxMessageLength)
            {
                await botClient.SendTextMessageAsync(chatId, message, cancellationToken: cancellationToken);
            }
            else
            {
                for (int i = 0; i < message.Length; i += MaxMessageLength)
                {
                    var part = message.Substring(i, Math.Min(MaxMessageLength, message.Length - i));
                    await botClient.SendTextMessageAsync(chatId, part, cancellationToken: cancellationToken);
                }
            }
        }
    }
}
