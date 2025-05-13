using Binance.Net.Enums;
using Terminal;
using System;
using System.Threading.Tasks;
using Terminal.Managers;
using Terminal.Enums;
using Terminal.Static;

namespace TerminalConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {

            await PackageUpdateAsyncTest();
        }

        static async Task PackageUpdateAsyncTest()
        {
            // Указываем идентификаторы пакетов для проверки обновлений.
            string[] packageIds = { "Binance.Net", "CryptoExchange.Net" };

            // Создаем экземпляр класса PackageUpdateProcessor.
            var processor = new PackageUpdateProcessor(packageIds);

            // Асинхронно запускаем проверку обновлений.
            Task checkingTask = processor.StartAsync();

            Console.WriteLine("Проверка обновлений запущена. Она будет выполняться раз в сутки.");
            Console.WriteLine("Для завершения работы приложения нажмите любую клавишу...");

            Console.ReadKey();
            processor.Stop();

            // Ждем завершения текущего цикла проверки.
            await checkingTask;
        }

        static void LogTest()
        {
            Log.Message("Приложение запущено.");
            Log.Ok("Подключение выполнено успешно.");
            Log.Error("Обнаружена ошибка при выполнении операции.");

            Console.WriteLine("\nВсе логи:");
            foreach (var log in Storage.GetLogsSnapshot())
            {
                Console.WriteLine($"[{log.Index}] {log.Timestamp} {log.Level}: {log.Text}");
            }

            Console.WriteLine("\nТолько ошибки:");
            foreach (var log in Storage.GetErrorLogsSnapshot())
            {
                Console.WriteLine($"[{log.Index}] {log.Timestamp} {log.Level}: {log.Text}");
            }

        }

        static async Task RestAsyncTest()
        {
            using var binanceProcessor = new BinanceRestProcessor(marketType: BinanceMarketType.Spot);

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


        static async Task HistoryAsyncTest()
        {
            Console.WriteLine("Добро пожаловать в тестирование BinanceHistoryFilesProcessor!");

            // Выбор рынка
            BinanceMarketType marketType;
            while (true)
            {
                Console.WriteLine("Выберите рынок (1 - Spot, 2 - Futures):");
                var marketInput = Console.ReadLine();

                if (marketInput == "1")
                {
                    marketType = BinanceMarketType.Spot;
                    break;
                }
                else if (marketInput == "2")
                {
                    marketType = BinanceMarketType.Futures;
                    break;
                }
                else
                {
                    Console.WriteLine("Неверный выбор. Повторите попытку.");
                }
            }

            // Ввод монеты
            Console.WriteLine("Введите название монеты (например, BTCUSDT):");
            var coinName = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(coinName))
            {
                Console.WriteLine("Название монеты не может быть пустым.");
                return;
            }

            // Ввод периода времени
            Console.WriteLine("Укажите период времени:");
            Console.WriteLine("1 - Задать количество месяцев");
            Console.WriteLine("2 - Задать количество лет");

            int months = 0, years = 0;
            var periodChoice = Console.ReadLine();

            if (periodChoice == "1")
            {
                Console.WriteLine("Введите количество месяцев:");
                if (!int.TryParse(Console.ReadLine(), out months) || months <= 0)
                {
                    Console.WriteLine("Некорректное значение для месяцев.");
                    return;
                }
            }
            else if (periodChoice == "2")
            {
                Console.WriteLine("Введите количество лет:");
                if (!int.TryParse(Console.ReadLine(), out years) || years <= 0)
                {
                    Console.WriteLine("Некорректное значение для лет.");
                    return;
                }
            }
            else
            {
                Console.WriteLine("Неверный выбор периода времени.");
                return;
            }

            // Инициализация BinanceHistoryFilesProcessor
            var processor = new BinanceHistoryFilesProcessor(marketType);

            // Обработка файлов
            try
            {
                Console.WriteLine("Начинаем обработку файлов...");
                await processor.ProcessFilesAsync(coinName, years, months);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка во время выполнения: {ex.Message}");
            }

        }
    }


}
