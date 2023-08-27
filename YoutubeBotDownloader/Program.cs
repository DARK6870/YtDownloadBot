using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeExplode;

internal class Program
{
    static Dictionary<long, string> VideoDownloadRequests = new Dictionary<long, string>(); // Объявляем словарь

    static async Task Main(string[] args)
    {
        try
        {
            var client = new TelegramBotClient("5918018675:AAGSx7bN6xtEX74yA-CaADsiNBQdBBk0uFw");
            client.StartReceiving(Update, HandleError);
            Console.WriteLine("Bot started. Press Enter to exit.");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while starting the bot: {ex}");
        }
    }


    async static Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        try
        {
            var message = update.Message;

            if (message.Text != null)
            {
                if (message.Text.ToLower() == "привет" || message.Text.ToLower() == "/start")
                {
                    var replyKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[] { new KeyboardButton("Скачать видео") }
                    });

                    var messageToSend = "Привет, я бот, который может скачивать видео с ТТ. " +
                                        "Нажми кнопку 'Скачать видео' для начала скачивания.";

                    await botClient.SendTextMessageAsync(message.Chat.Id, messageToSend,
                                                         replyMarkup: replyKeyboard);
                }
                else if (message.Text.ToLower() == "скачать видео")
                {
                    VideoDownloadRequests[message.Chat.Id] = "waiting_for_url";
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введите ссылку на видео:");
                }

                else if (VideoDownloadRequests.TryGetValue(message.Chat.Id, out string state) && state == "waiting_for_url")
                {
                    VideoDownloadRequests.Remove(message.Chat.Id);


                    string videoUrl = message.Text;
                    try
                    {
                    string downloadedVideoPath = await DownloadVideo(videoUrl);

                    if (!string.IsNullOrEmpty(downloadedVideoPath) && System.IO.File.Exists(downloadedVideoPath))
                    {
                        using var videoStream = new FileStream(downloadedVideoPath, FileMode.Open);
                        try
                        {
                            await botClient.SendVideoAsync(message.Chat.Id, videoStream);
                        }
                        catch
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Что-то пошло не так, возможно размер видео превышает допустимые лимиты.");
                        }

                        videoStream.Close();
                        System.IO.File.Delete(downloadedVideoPath);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Не удалось загрузить видео.");
                    }
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Что-то пошло не так, проверьте исправность ссылки и повторите попытку");
                    }

                    
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Неизвестная команда");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while processing update: {ex}");
        }


    async Task<String> DownloadVideo(string videoUrl)
    {
        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(videoUrl);

        string sanitizedTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));


        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
        var muxedStreams = streamManifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality).ToList();

        if (muxedStreams.Any())
        {
            var streamInfo = muxedStreams.First();
            using var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(streamInfo.Url);
            var datetime = DateTime.Now;

            string outputFilePath = Path.Combine("D:/", $"{sanitizedTitle}.{streamInfo.Container}");
            using var outputStream = System.IO.File.Create(outputFilePath);
            await stream.CopyToAsync(outputStream);

            Console.WriteLine("Download completed!");
            Console.WriteLine($"Video saved as: {outputFilePath}{datetime}");
            return outputFilePath;
        }
        else
        {
            Console.WriteLine($"No suitable video stream found for {video.Title}.");
            return "error";
        }
    }
    }

    

    private static Task HandleError(ITelegramBotClient botClient, Exception ex, CancellationToken token)
    {
        Console.WriteLine($"Error occurred: {ex}");
        return Task.CompletedTask;
    }
}
