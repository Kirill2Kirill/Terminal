using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WebHooksBridge
{
    class TradingViewWebhookServer
    {
        private static readonly HttpListener listener = new HttpListener();

        public static async Task StartAsync(int port)
        {
            listener.Prefixes.Add($"http://*:{port}/api/tradingview/signal/");
            listener.Start();

            Console.WriteLine($"✅ Webhook-сервер запущен на {GetWebhookUrl(port)}");

            while (true)
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequestAsync(context));
            }
        }

        private static async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                using var reader = new System.IO.StreamReader(context.Request.InputStream, Encoding.UTF8);
                string json = await reader.ReadToEndAsync();

                var signal = await WebhookDeserializer.DeserializeAsync(json);
                if (signal == null)
                {
                    context.Response.StatusCode = 400;
                    await Logger.LogErrorAsync("❌ Некорректный вебхук от TradingView.");
                    return;
                }

                await ExchangeService.ProcessTradingSignalAsync(signal);
                context.Response.StatusCode = 200;
                await Logger.LogSuccessAsync($"✅ Вебхук обработан: {signal.Symbol}");
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await Logger.LogErrorAsync("❌ Ошибка обработки вебхука", ex);
            }
        }

        private static string GetWebhookUrl(int port)
        {
            string localIp = GetLocalIPAddress();
            return $"http://{localIp}:{port}/api/tradingview/signal";
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip != null ? ip.ToString() : "localhost";
        }
    }


}
