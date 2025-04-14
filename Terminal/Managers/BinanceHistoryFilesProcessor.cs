using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using MessagePack;
using Terminal.Models;

namespace Terminal.Managers
{
    public class BinanceHistoryFilesProcessor
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<DateTime, HistoryKline>> Cache = new();
        private ConcurrentDictionary<DateTime, HistoryKline> allKlines = new ConcurrentDictionary<DateTime, HistoryKline>();
        private ConcurrentBag<string> logResults = new ConcurrentBag<string>();
        private SemaphoreSlim semaphore = new SemaphoreSlim(10); // Ограничение на 10 параллельных задач

        //Log("Введите название монеты (например, 1000PEPEUSDT):");
        //string coinName = Console.ReadLine()?.Trim() ?? "1000PEPEUSDT";
        //Log("Введите количество последних лет для обработки (0, если хотите указать месяцы):");
        //int years = int.Parse(Console.ReadLine() ?? "0");
        //Log("Введите количество последних месяцев для обработки (0, если указаны годы):");
        //int months = int.Parse(Console.ReadLine() ?? "0");

        //public List<MyKline> AggregateKlines(List<MyKline> minuteKlines, int interval)
        //{
        //    try
        //    {
        //        // Загрузка минутных свечей
        //        Console.WriteLine($"Загружено {minuteKlines.Count} минутных свечей.");

        //        var lastKline1 = minuteKlines[^1]; // Получаем последнюю минутную свечу
        //        var lastKline2 = minuteKlines[^2]; // Получаем последнюю минутную свечу
        //        var lastKline3 = minuteKlines[^3]; // Получаем последнюю минутную свечу
        //        var lastKline4 = minuteKlines[^4]; // Получаем последнюю минутную свечу
        //        var lastKline5 = minuteKlines[^5]; // Получаем последнюю минутную свечу

        //        Console.WriteLine($"1: openTime:{lastKline1.OpenTime}, open: {lastKline1.OpenPrice}, close: {lastKline1.ClosePrice}");
        //        Console.WriteLine($"2: openTime:{lastKline2.OpenTime}, open: {lastKline2.OpenPrice}, close: {lastKline2.ClosePrice}");
        //        Console.WriteLine($"3: openTime:{lastKline3.OpenTime}, open: {lastKline3.OpenPrice}, close: {lastKline3.ClosePrice}");
        //        Console.WriteLine($"4: openTime:{lastKline4.OpenTime}, open: {lastKline4.OpenPrice}, close: {lastKline4.ClosePrice}");
        //        Console.WriteLine($"5: openTime:{lastKline5.OpenTime}, open: {lastKline5.OpenPrice}, close: {lastKline5.ClosePrice}");
        //        Console.WriteLine();
        //        // Агрегация свечей
        //        var aggregatedKlines = AggregateKlines(minuteKlines, interval);
        //        var newLast = aggregatedKlines[^1];
        //        var newLast2 = aggregatedKlines[^2];
        //        Console.WriteLine($"1: openTime:{newLast.OpenTime}, open: {newLast.OpenPrice}, close: {newLast.ClosePrice}");
        //        Console.WriteLine($"2: openTime:{newLast2.OpenTime}, open: {newLast2.OpenPrice}, close: {newLast2.ClosePrice}");

        //        Console.WriteLine();

        //        Console.WriteLine($"Сформировано {aggregatedKlines.Count} свечей с интервалом {interval} минут.");

        //        // Вывод примера
        //        foreach (var kline in aggregatedKlines.Take(5)) // Вывод первых 5 свечей
        //        {
        //            Console.WriteLine($"OpenTime: {kline.OpenTime}, CloseTime: {kline.CloseTime}, OpenPrice: {kline.OpenPrice}, ClosePrice: {kline.ClosePrice}, HighPrice: {kline.HighPrice}, LowPrice: {kline.LowPrice}, Volume: {kline.Volume}");
        //        }

        //        return aggregatedKlines;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Ошибка: {ex.Message}");
        //        return new List<MyKline>();
        //    }
        //}


        public async Task<List<HistoryKline>> LoadKlinesFromPathAsync(string filePath)
        {
            // Загрузка минутных свечей
            var result = await LoadAllKlinesAsync(filePath);
            var minuteKlines = result.Values.OrderByDescending(x => x.OpenTime).ToList();
            Console.WriteLine($"Загружено {minuteKlines.Count} минутных свечей.");
            return minuteKlines;
        }

        public async Task<List<HistoryKline>> LoadAndAggregateKlinesAsync(string filePath, int interval)
        {
            try
            {
                // Загрузка минутных свечей
                var result = await LoadAllKlinesAsync(filePath);
                var minuteKlines = result.Values.OrderByDescending(x => x.OpenTime).ToList();
                Console.WriteLine($"Загружено {minuteKlines.Count} минутных свечей.");

                var lastKline1 = minuteKlines[^1]; // Получаем последнюю минутную свечу
                var lastKline2 = minuteKlines[^2]; // Получаем последнюю минутную свечу
                var lastKline3 = minuteKlines[^3]; // Получаем последнюю минутную свечу
                var lastKline4 = minuteKlines[^4]; // Получаем последнюю минутную свечу
                var lastKline5 = minuteKlines[^5]; // Получаем последнюю минутную свечу

                Console.WriteLine($"1: openTime:{lastKline1.OpenTime}, open: {lastKline1.OpenPrice}, close: {lastKline1.ClosePrice}");
                Console.WriteLine($"2: openTime:{lastKline2.OpenTime}, open: {lastKline2.OpenPrice}, close: {lastKline2.ClosePrice}");
                Console.WriteLine($"3: openTime:{lastKline3.OpenTime}, open: {lastKline3.OpenPrice}, close: {lastKline3.ClosePrice}");
                Console.WriteLine($"4: openTime:{lastKline4.OpenTime}, open: {lastKline4.OpenPrice}, close: {lastKline4.ClosePrice}");
                Console.WriteLine($"5: openTime:{lastKline5.OpenTime}, open: {lastKline5.OpenPrice}, close: {lastKline5.ClosePrice}");
                Console.WriteLine();
                // Агрегация свечей
                var aggregatedKlines = AggregateKlines(minuteKlines, interval);
                var newLast = aggregatedKlines[^1];
                var newLast2 = aggregatedKlines[^2];
                Console.WriteLine($"1: openTime:{newLast.OpenTime}, open: {newLast.OpenPrice}, close: {newLast.ClosePrice}");
                Console.WriteLine($"2: openTime:{newLast2.OpenTime}, open: {newLast2.OpenPrice}, close: {newLast2.ClosePrice}");

                Console.WriteLine();

                Console.WriteLine($"Сформировано {aggregatedKlines.Count} свечей с интервалом {interval} минут.");

                // Вывод примера
                foreach (var kline in aggregatedKlines.Take(5)) // Вывод первых 5 свечей
                {
                    Console.WriteLine($"OpenTime: {kline.OpenTime}, CloseTime: {kline.CloseTime}, OpenPrice: {kline.OpenPrice}, ClosePrice: {kline.ClosePrice}, HighPrice: {kline.HighPrice}, LowPrice: {kline.LowPrice}, Volume: {kline.Volume}");
                }

                return aggregatedKlines;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return new List<HistoryKline>();
            }
        }

        public List<HistoryKline> AggregateKlines(List<HistoryKline> minuteKlines, int interval)
        {
            if (minuteKlines == null || minuteKlines.Count == 0)
            {
                throw new ArgumentException("Список минутных свечей пуст.");
            }

            if (interval <= 0)
            {
                throw new ArgumentException("Интервал должен быть больше 0.");
            }

            var aggregatedKlines = new List<HistoryKline>();

            // Разворачиваем список минутных свечей для обработки с самых дальних по дате
            //var reversedKlines = minuteKlines.OrderByDescending(k => k.OpenTime).ToList();
            var reversedKlines = minuteKlines;

            for (int i = 0; i < reversedKlines.Count; i += interval)
            {
                // Берем текущую группу свечей
                var group = reversedKlines.Skip(i).Take(interval).ToList();

                // Формируем новую свечу даже если группа меньше интервала
                var aggregatedKline = new HistoryKline
                {
                    OpenTime = group.Last().OpenTime, // Время открытия первой свечи в группе (самая дальняя по дате)
                    OpenPrice = group.Last().OpenPrice, // Цена открытия первой свечи в группе
                    CloseTime = group.First().CloseTime, // Время закрытия последней свечи в группе (самая ближняя по дате)
                    ClosePrice = group.First().ClosePrice, // Цена закрытия последней свечи в группе
                    HighPrice = group.Max(k => k.HighPrice), // Максимальная цена среди группы
                    LowPrice = group.Min(k => k.LowPrice), // Минимальная цена среди группы
                    Volume = group.Sum(k => k.Volume), // Суммарный объем
                    QuoteVolume = group.Sum(k => k.QuoteVolume), // Суммарный объем в котировочной валюте
                    TradeCount = group.Sum(k => k.TradeCount), // Суммарное количество сделок
                    TakerBuyBaseVolume = group.Sum(k => k.TakerBuyBaseVolume), // Суммарный объем покупок
                    TakerBuyQuoteVolume = group.Sum(k => k.TakerBuyQuoteVolume) // Суммарный объем покупок в котировочной валюте
                };

                aggregatedKlines.Add(aggregatedKline);
            }

            return aggregatedKlines;
        }

        private async Task<ConcurrentDictionary<DateTime, HistoryKline>> LoadAllKlinesAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл {filePath} не найден.");
            }

            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            var allKlines = await MessagePack.MessagePackSerializer.DeserializeAsync<ConcurrentDictionary<DateTime, HistoryKline>>(fileStream);
            return allKlines;
        }

        public async Task ProcessFilesAsync(string coinName, int years = 0, int months = 0)
        {
            //Путь к папке программы
            string baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DATA");
            string coinFolder = Path.Combine(baseFolder, coinName);
            string downloadFolder = Path.Combine(coinFolder, "Downloads");
            string extractFolder = Path.Combine(coinFolder, "Extracts");
            string dataFolder = Path.Combine(coinFolder, "MessagePack");

            // Загрузка существующего MessagePack-файла, если он есть
            var stopwatch = Stopwatch.StartNew();
            allKlines = await LoadFromMessagePackAsync(dataFolder, coinName);
            Log($"Загрузка MessagePack-файла заняла: {stopwatch.ElapsedMilliseconds} мс");

            // Сохраняем начальное количество записей
            int initialCount = allKlines.Count;

            DateTime currentDate = DateTime.UtcNow;
            DateTime startDate = years > 0 ? currentDate.AddYears(-years) : currentDate.AddMonths(-months);

            // Устанавливаем конец диапазона на последний день предыдущего месяца
            DateTime endDate = new DateTime(currentDate.Year, currentDate.Month, 1).AddDays(-1);

            // Список задач для параллельной обработки файлов
            List<Task> tasks = new List<Task>();
            Dictionary<string, string> results = new Dictionary<string, string>();

            for (DateTime date = startDate; date <= endDate; date = date.AddMonths(1))
            {
                string year = date.Year.ToString();
                string month = date.Month.ToString("D2");
                string fileName = $"{coinName}-1m-{year}-{month}.zip";
                string downloadUrl = $"https://data.binance.vision/data/futures/um/monthly/klines/{coinName}/1m/{fileName}";
                string checksumUrl = $"{downloadUrl}.CHECKSUM";

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        // Формирование пути для скачивания zip файла
                        string zipFilePath = Path.Combine(downloadFolder, Path.GetFileName(downloadUrl));

                        // Проверка существования zip файла
                        if (File.Exists(zipFilePath))
                        {
                            results[$"{month}.{year}"] = "имелись ранее";
                            return;
                        }

                        string result = await ProcessFileAsync(downloadUrl, checksumUrl, downloadFolder, extractFolder);
                        results[$"{month}.{year}"] = result.Contains("успешно") ? "загружены" : "отсутствуют";
                        Log($"Скачивание zip файла за {month}.{year}...{result}");
                    }
                    catch (Exception ex)
                    {
                        //Log($"Ошибка при обработке файла за {month}.{year}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            // Ожидание завершения всех задач
            stopwatch.Restart();
            await Task.WhenAll(tasks);
            Log($"Ожидание завершения всех задач заняло: {stopwatch.ElapsedMilliseconds} мс");

            // Проверяем, изменились ли данные
            if (allKlines.Count > initialCount)
            {
                //Log($"Всего записей для сохранения: {allKlines.Count - initialCount}. Сохранение в MessagePack...");
                await SaveToMessagePackAsync(allKlines, dataFolder, coinName);
            }

            // Сортировка и вывод сообщений
            Log("\nИтоги проверки:");
            foreach (var result in results.OrderByDescending(r =>
            {
                var dateParts = r.Key.Split('.');
                if (int.TryParse(dateParts[1], out int year) && int.TryParse(dateParts[0], out int month))
                {
                    return new DateTime(year, month, 1);
                }
                return DateTime.MinValue;
            }))
            {
                Log($"Данные за {result.Key} - {result.Value}");
            }
        }

        private async Task<string> ProcessFileAsync(string downloadUrl, string checksumUrl, string downloadFolder, string extractFolder)
        {
            // Создание папок, если они отсутствуют
            CreateDirectoryIfNotExists(downloadFolder);
            CreateDirectoryIfNotExists(extractFolder);

            // Формирование пути для скачивания zip файла
            string zipFilePath = Path.Combine(downloadFolder, Path.GetFileName(downloadUrl));

            try
            {
                // Скачивание zip файла
                await DownloadFileAsync(downloadUrl, zipFilePath);

                // Скачивание файла чексуммы
                string checksumContent = await DownloadChecksumAsync(checksumUrl);

                // Проверка чексуммы
                try
                {
                    ValidateChecksum(zipFilePath, checksumContent);
                }
                catch (InvalidOperationException)
                {
                    return "успешно...хеш ОШИБКА";
                }

                // Распаковка zip файла
                await ExtractZipFileAsync(zipFilePath, extractFolder);

                // Ожидаемое название файла
                string expectedFileName = Path.GetFileNameWithoutExtension(zipFilePath) + ".csv";
                string csvFilePath = Path.Combine(extractFolder, expectedFileName);

                // Проверка существования файла
                if (!File.Exists(csvFilePath))
                {
                    return "успешно...файл отсутствует";
                }

                // Преобразование CSV в коллекцию объектов и добавление в словарь
                List<HistoryKline> klines = await ParseCsvToKlinesAsync(csvFilePath);

                foreach (var kline in klines)
                {
                    allKlines.AddOrUpdate(kline.OpenTime, kline, (key, existing) => kline);
                }

                return "успешно...хеш ок";
            }
            catch (FileNotFoundException)
            {
                return "файл отсутствует";
            }
            catch (Exception ex)
            {
                //Log($"Ошибка при обработке файла: {ex.Message}");
                return "ошибка";
            }
        }

        private async Task ExtractZipFileAsync(string zipPath, string extractPath)
        {
            await Task.Run(() =>
            {
                using (var zipInputStream = new ZipInputStream(File.OpenRead(zipPath)))
                {
                    ZipEntry entry;
                    while ((entry = zipInputStream.GetNextEntry()) != null)
                    {
                        string filePath = Path.Combine(extractPath, entry.Name);
                        using (var fileStream = File.Create(filePath))
                        {
                            zipInputStream.CopyTo(fileStream);
                        }
                    }
                }
            });
        }

        private async Task<List<HistoryKline>> ParseCsvToKlinesAsync(string csvFilePath)
        {
            var klines = new ConcurrentBag<HistoryKline>();

            // Чтение файла с использованием буферизации
            var lines = await File.ReadAllLinesAsync(csvFilePath);

            // Параллельная обработка строк
            Parallel.ForEach(lines.Skip(1), line =>
            {
                try
                {
                    var columns = line.Split(',');

                    // Проверяем, что строка содержит достаточное количество столбцов
                    if (columns.Length < 11)
                    {
                        return;
                    }

                    // Преобразуем данные в объект Kline
                    var kline = new HistoryKline
                    {
                        OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(columns[0])).DateTime,
                        OpenPrice = decimal.Parse(columns[1], CultureInfo.InvariantCulture),
                        HighPrice = decimal.Parse(columns[2], CultureInfo.InvariantCulture),
                        LowPrice = decimal.Parse(columns[3], CultureInfo.InvariantCulture),
                        ClosePrice = decimal.Parse(columns[4], CultureInfo.InvariantCulture),
                        Volume = decimal.Parse(columns[5], CultureInfo.InvariantCulture),
                        CloseTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(columns[6])).DateTime,
                        QuoteVolume = decimal.Parse(columns[7], CultureInfo.InvariantCulture),
                        TradeCount = int.Parse(columns[8], CultureInfo.InvariantCulture),
                        TakerBuyBaseVolume = decimal.Parse(columns[9], CultureInfo.InvariantCulture),
                        TakerBuyQuoteVolume = decimal.Parse(columns[10], CultureInfo.InvariantCulture)
                    };

                    klines.Add(kline);
                }
                catch
                {
                    // Игнорируем строки с ошибками
                }
            });

            return klines.ToList();
        }

        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private async Task DownloadFileAsync(string url, string outputPath)
        {
            // Проверка существования файла
            if (File.Exists(outputPath))
            {
                Log($"Файл уже существует: {outputPath}. Скачивание пропущено.");
                return;
            }

            using (HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }))
            {
                client.Timeout = TimeSpan.FromMinutes(5); // Увеличиваем таймаут для больших файлов

                try
                {
                    // Скачивание файла с использованием потоковой записи
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        await using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
                        {
                            await using (var httpStream = await response.Content.ReadAsStreamAsync())
                            {
                                await httpStream.CopyToAsync(fileStream);
                            }
                        }
                    }

                    //Log($"Файл успешно скачан: {outputPath}");
                }
                catch (Exception ex)
                {
                    //Log($"Ошибка при скачивании файла {url}: {ex.Message}");
                    throw;
                }
            }
        }

        private async Task<string> DownloadChecksumAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException($"Файл чексуммы {url} не найден.");
                }
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        private void ValidateChecksum(string filePath, string checksumContent)
        {
            string fileName = Path.GetFileName(filePath);
            string expectedChecksum = null;
            string[] lines = checksumContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains(fileName))
                {
                    expectedChecksum = line.Split(' ')[0].Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(expectedChecksum))
            {
                throw new InvalidOperationException("Не удалось найти ожидаемую чексумму для файла.");
            }

            using (var sha256 = SHA256.Create())
            using (var fileStream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(fileStream);
                string actualChecksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                if (actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase))
                {
                    //Log("Чексумма файла успешно проверена: совпадает.");
                }
                else
                {
                    Log("Чексумма файла не совпадает!");
                    throw new InvalidOperationException("Чексумма файла не совпадает с ожидаемой.");
                }
            }
        }

        private async Task<ConcurrentDictionary<DateTime, HistoryKline>> LoadFromMessagePackAsync(string folderPath, string coinName)
        {
            var stopwatch = Stopwatch.StartNew();

            string filePath = Path.Combine(folderPath, $"{coinName}_all_data.msgpack");

            // Проверка кэша
            if (Cache.TryGetValue(filePath, out var cachedData))
            {
                Log("Данные загружены из кэша.");
                stopwatch.Stop();
                Log($"Загрузка из кэша заняла: {stopwatch.ElapsedMilliseconds} мс");
                return cachedData;
            }

            // Проверка существования файла
            if (!File.Exists(filePath))
            {
                stopwatch.Stop();
                Log($"Файл не найден. Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
                return new ConcurrentDictionary<DateTime, HistoryKline>();
            }

            try
            {
                // Чтение файла с увеличенным буфером
                await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 131072, useAsync: true);
                var data = await MessagePack.MessagePackSerializer.DeserializeAsync<ConcurrentDictionary<DateTime, HistoryKline>>(fileStream);

                // Кэширование данных
                Cache[filePath] = data;

                stopwatch.Stop();
                Log($"Десериализация MessagePack-файла заняла: {stopwatch.ElapsedMilliseconds} мс");
                return data;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log($"Ошибка при загрузке MessagePack-файла: {ex.Message}. Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
                return new ConcurrentDictionary<DateTime, HistoryKline>();
            }
        }
        private async Task SaveToMessagePackAsync(ConcurrentDictionary<DateTime, HistoryKline> klines, string folderPath, string coinName)
        {
            string filePath = Path.Combine(folderPath, $"{coinName}_all_data.msgpack");

            // Создание папки, если она не существует
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Сериализация данных в MessagePack и запись в файл
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await MessagePack.MessagePackSerializer.SerializeAsync(fileStream, klines);
            //Log($"Данные успешно сохранены в файл: {filePath}");
            Log($"Данные успешно сохранены.");
        }
        private void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

}