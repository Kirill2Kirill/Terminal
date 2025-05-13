using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;


namespace WebHooksBridge
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += async (sender, e) =>
                await GlobalExceptionHandler(e);

            Console.WriteLine("✅ WebHooksBridge запущен!");
            await StartWebhookListenerAsync(5000);

            await WebHooksTester.TestWebhookAsync();
            await WebHooksTester.TestLoggingAsync();
            await WebHooksTester.TestTelegramAsync();

        }

        static async Task StartWebhookListenerAsync(int port)
        {
            HttpListener listener = new HttpListener();
            //listener.Prefixes.Add($"http://*:{port}/api/tradingview/signal/");
            listener.Prefixes.Add($"http://localhost:{port}/api/tradingview/signal/");
            listener.Start();

            Console.WriteLine($"🔗 Webhook-сервер запущен: {GetWebhookUrl(port)}");

            while (true)
            {
                var context = await listener.GetContextAsync();
                await ProcessWebhookAsync(context);
            }
        }

        static async Task ProcessWebhookAsync(HttpListenerContext context)
        {
            try
            {
                using var reader = new System.IO.StreamReader(context.Request.InputStream);
                string json = await reader.ReadToEndAsync();

                var signal = await WebhookDeserializer.DeserializeAsync(json);
                if (signal == null)
                {
                    context.Response.StatusCode = 400;
                    await Logger.LogErrorAsync("❌ Некорректный WebHook от TradingView.");
                    return;
                }

                await ExchangeService.ProcessTradingSignalAsync(signal);
                context.Response.StatusCode = 200;
                await Logger.LogSuccessAsync($"✅ WebHook обработан: {signal.Symbol}");
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await Logger.LogErrorAsync("❌ Ошибка обработки WebHook", ex);
            }
        }

        static async Task GlobalExceptionHandler(UnhandledExceptionEventArgs e)
        {
            Exception? ex = e.ExceptionObject as Exception;
            string errorMessage = ex != null ? ex.Message : "Неизвестная критическая ошибка!";

            await Logger.LogErrorAsync($"🚨 Критический сбой приложения: {errorMessage}", ex);
            await TelegramNotifier.SendMessageAsync($"🚨 Программа упала! Ошибка: {errorMessage}");

            await RestartApplicationAsync();
        }

        static async Task RestartApplicationAsync()
        {
            Console.WriteLine("🔄 Перезапуск WebHooksBridge...");
            Process.Start(Environment.ProcessPath);
            await Task.CompletedTask;
            Environment.Exit(1);
        }

        static string GetWebhookUrl(int port)
        {
            string localIp = GetLocalIPAddress();
            return $"http://{localIp}:{port}/api/tradingview/signal";
        }

        static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            return ip != null ? ip.ToString() : "localhost";
        }


    }
}