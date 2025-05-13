using System;
using Telegram.Bot;
using System.Threading.Tasks;


namespace WebHooksBridge
{
    public static class TelegramNotifier
    {
        private static readonly string botToken = "YOUR_BOT_TOKEN";
        private static readonly long chatId = 111;
        private static readonly TelegramBotClient botClient = new TelegramBotClient(botToken);

        public static async Task SendMessageAsync(string message)
        {
            try
            {
                await botClient.SendMessage(chatId, message);
                Console.WriteLine($"✅ Сообщение отправлено в Telegram: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки сообщения в Telegram: {ex.Message}");
            }
        }
    }


}
