using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebHooksBridge
{
    public static class WebHooksTester
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string webhookUrl = "http://localhost:5000/api/tradingview/signal";

        public static async Task TestWebhookAsync()
        {
            var testSignal = new TradingSignal
            {
                Symbol = "BTCUSDT",
                Exchanges = new[] { "binance" },
                EntryMarketOrder = new MarketOrder { Action = OrderAction.Buy, Amount = 0.01M, ClientId = "test123" }
            };

            string json = JsonConvert.SerializeObject(testSignal);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(webhookUrl, content);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    Console.WriteLine($"✅ Вебхук успешно отправлен: {result}");
                else
                    Console.WriteLine($"❌ Ошибка отправки вебхука: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка теста вебхука: {ex.Message}");
            }
        }

        public static async Task TestLoggingAsync()
        {
            await Logger.LogSuccessAsync("✅ Тест успешного логирования!");
            await Logger.LogErrorAsync("❌ Тест ошибки логирования!", new Exception("Тестовая ошибка"));
        }

        public static async Task TestTelegramAsync()
        {
            await TelegramNotifier.SendMessageAsync("✅ Тестовое сообщение от WebHooksTester!");
        }
    }

}
