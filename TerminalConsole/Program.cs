using Binance.Net.Enums;
using Terminal;
using System;
using System.Threading.Tasks;
using Terminal.Managers;

namespace TerminalConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var binanceProcessor = new BinanceRestProcessor();

            try
            {
                // Параметры для получения свечей
                string symbol = "BTCUSDT";
                KlineInterval interval = KlineInterval.OneMinute;
                DateTime startTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                DateTime endTime = DateTime.UtcNow;

                // Получаем свечи
                var klines = await binanceProcessor.GetKlinesAsync(symbol, interval, startTime, endTime);

                Console.WriteLine($"Получено {klines.Count} свечей для {symbol}.");
                Console.WriteLine($"Первая свеча {klines[0].OpenTime}.");
                Console.WriteLine($"Средняя свеча {klines[^(klines.Count / 2)].OpenTime}.");
                Console.WriteLine($"Последняя свеча {klines[^1].OpenTime}.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Операция была отменена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            // Отмена операций (например, по требованию пользователя)
            binanceProcessor.CancelOperations();
        }
    }
}
