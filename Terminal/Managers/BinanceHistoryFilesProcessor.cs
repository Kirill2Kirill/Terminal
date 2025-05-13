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
using Terminal.Enums;
using Terminal.Models;

namespace Terminal.Managers
{
    public class BinanceHistoryFilesProcessor
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<DateTime, HistoryKline>> Cache = new();
        private ConcurrentDictionary<DateTime, HistoryKline> allKlines = new ConcurrentDictionary<DateTime, HistoryKline>();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(10); // Ограничение на 10 параллельных задач
        private readonly BinanceMarketType _marketType;
        private readonly string _baseUrl;
        private readonly string _dataFolder;

        public BinanceHistoryFilesProcessor(BinanceMarketType marketType)
        {
            _marketType = marketType;

            // Устанавливаем базовый URL в зависимости от типа рынка
            _baseUrl = marketType switch
            {
                BinanceMarketType.Spot => "https://data.binance.vision/data/spot/monthly/klines/",
                BinanceMarketType.Futures => "https://data.binance.vision/data/futures/um/monthly/klines/",
                _ => throw new ArgumentException("Неверный тип рынка.")
            };

            // Устанавливаем папку для хранения данных
            _dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DATA", marketType.ToString());
            CreateDirectoryIfNotExists(_dataFolder);
        }

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

                // Агрегация свечей
                var aggregatedKlines = AggregateKlines(minuteKlines, interval);
                Console.WriteLine($"Сформировано {aggregatedKlines.Count} свечей с интервалом {interval} минут.");
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

            for (int i = 0; i < minuteKlines.Count; i += interval)
            {
                var group = minuteKlines.Skip(i).Take(interval).ToList();

                var aggregatedKline = new HistoryKline
                {
                    OpenTime = group.Last().OpenTime,
                    OpenPrice = group.Last().OpenPrice,
                    CloseTime = group.First().CloseTime,
                    ClosePrice = group.First().ClosePrice,
                    HighPrice = group.Max(k => k.HighPrice),
                    LowPrice = group.Min(k => k.LowPrice),
                    Volume = group.Sum(k => k.Volume),
                    QuoteVolume = group.Sum(k => k.QuoteVolume),
                    TradeCount = group.Sum(k => k.TradeCount),
                    TakerBuyBaseVolume = group.Sum(k => k.TakerBuyBaseVolume),
                    TakerBuyQuoteVolume = group.Sum(k => k.TakerBuyQuoteVolume)
                };

                aggregatedKlines.Add(aggregatedKline);
            }

            return aggregatedKlines;
        }

        public async Task ProcessFilesAsync(string coinName, int years = 0, int months = 0)
        {
            string coinFolder = Path.Combine(_dataFolder, coinName);
            string downloadFolder = Path.Combine(coinFolder, "Downloads");
            string extractFolder = Path.Combine(coinFolder, "Extracts");
            string messagePackFolder = Path.Combine(coinFolder, "MessagePack");

            CreateDirectoryIfNotExists(downloadFolder);
            CreateDirectoryIfNotExists(extractFolder);
            CreateDirectoryIfNotExists(messagePackFolder);

            var stopwatch = Stopwatch.StartNew();
            allKlines = await LoadFromMessagePackAsync(messagePackFolder, coinName);
            Log($"Загрузка MessagePack-файла заняла: {stopwatch.ElapsedMilliseconds} мс");

            int initialCount = allKlines.Count;

            DateTime currentDate = DateTime.UtcNow;
            DateTime startDate = years > 0 ? currentDate.AddYears(-years) : currentDate.AddMonths(-months);
            DateTime endDate = new DateTime(currentDate.Year, currentDate.Month, 1).AddDays(-1);

            List<Task> tasks = new List<Task>();
            Dictionary<string, string> results = new Dictionary<string, string>();

            for (DateTime date = startDate; date <= endDate; date = date.AddMonths(1))
            {
                string year = date.Year.ToString();
                string month = date.Month.ToString("D2");
                string fileName = $"{coinName}-1m-{year}-{month}.zip";
                string downloadUrl = $"{_baseUrl}{coinName}/1m/{fileName}";
                string checksumUrl = $"{downloadUrl}.CHECKSUM";

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        string zipFilePath = Path.Combine(downloadFolder, Path.GetFileName(downloadUrl));

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
                        Log($"Ошибка при обработке файла за {month}.{year}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            stopwatch.Restart();
            await Task.WhenAll(tasks);
            Log($"Ожидание завершения всех задач заняло: {stopwatch.ElapsedMilliseconds} мс");

            if (allKlines.Count > initialCount)
            {
                Log($"Всего записей для сохранения: {allKlines.Count - initialCount}. Сохранение в MessagePack...");
                await SaveToMessagePackAsync(allKlines, messagePackFolder, coinName);
            }

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
            CreateDirectoryIfNotExists(downloadFolder);
            CreateDirectoryIfNotExists(extractFolder);

            string zipFilePath = Path.Combine(downloadFolder, Path.GetFileName(downloadUrl));

            try
            {
                await DownloadFileAsync(downloadUrl, zipFilePath);
                string checksumContent = await DownloadChecksumAsync(checksumUrl);

                try
                {
                    ValidateChecksum(zipFilePath, checksumContent);
                }
                catch (InvalidOperationException)
                {
                    return "успешно...хеш ОШИБКА";
                }

                await ExtractZipFileAsync(zipFilePath, extractFolder);

                string expectedFileName = Path.GetFileNameWithoutExtension(zipFilePath) + ".csv";
                string csvFilePath = Path.Combine(extractFolder, expectedFileName);

                if (!File.Exists(csvFilePath))
                {
                    return "успешно...файл отсутствует";
                }

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
                Log($"Ошибка при обработке файла: {ex.Message}");
                return "ошибка";
            }
        }

        private async Task<ConcurrentDictionary<DateTime, HistoryKline>> LoadAllKlinesAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл {filePath} не найден.");
            }

            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            return await MessagePack.MessagePackSerializer.DeserializeAsync<ConcurrentDictionary<DateTime, HistoryKline>>(fileStream);
        }

        private async Task<ConcurrentDictionary<DateTime, HistoryKline>> LoadFromMessagePackAsync(string folderPath, string coinName)
        {
            string filePath = Path.Combine(folderPath, $"{coinName}_all_data.msgpack");

            if (Cache.TryGetValue(filePath, out var cachedData))
            {
                Log("Данные загружены из кэша.");
                return cachedData;
            }

            if (!File.Exists(filePath))
            {
                return new ConcurrentDictionary<DateTime, HistoryKline>();
            }

            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 131072, useAsync: true);
            var data = await MessagePack.MessagePackSerializer.DeserializeAsync<ConcurrentDictionary<DateTime, HistoryKline>>(fileStream);
            Cache[filePath] = data;
            return data;
        }

        private async Task SaveToMessagePackAsync(ConcurrentDictionary<DateTime, HistoryKline> klines, string folderPath, string coinName)
        {
            string filePath = Path.Combine(folderPath, $"{coinName}_all_data.msgpack");

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await MessagePack.MessagePackSerializer.SerializeAsync(fileStream, klines);
            Log($"Данные успешно сохранены.");
        }

        private async Task ExtractZipFileAsync(string zipPath, string extractPath)
        {
            await Task.Run(() =>
            {
                using var zipInputStream = new ZipInputStream(File.OpenRead(zipPath));
                ZipEntry entry;
                while ((entry = zipInputStream.GetNextEntry()) != null)
                {
                    string filePath = Path.Combine(extractPath, entry.Name);
                    using var fileStream = File.Create(filePath);
                    zipInputStream.CopyTo(fileStream);
                }
            });
        }

        private async Task<List<HistoryKline>> ParseCsvToKlinesAsync(string csvFilePath)
        {
            var klines = new ConcurrentBag<HistoryKline>();
            var lines = await File.ReadAllLinesAsync(csvFilePath);

            Parallel.ForEach(lines.Skip(1), line =>
            {
                try
                {
                    var columns = line.Split(',');

                    if (columns.Length < 11)
                    {
                        return;
                    }

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

        private async Task DownloadFileAsync(string url, string outputPath)
        {
            if (File.Exists(outputPath))
            {
                Log($"Файл уже существует: {outputPath}. Скачивание пропущено.");
                return;
            }

            using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
            client.Timeout = TimeSpan.FromMinutes(5);

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
            await using var httpStream = await response.Content.ReadAsStreamAsync();
            await httpStream.CopyToAsync(fileStream);
        }

        private async Task<string> DownloadChecksumAsync(string url)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException($"Файл чексуммы {url} не найден.");
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
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

            using var sha256 = SHA256.Create();
            using var fileStream = File.OpenRead(filePath);
            byte[] hashBytes = sha256.ComputeHash(fileStream);
            string actualChecksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            if (!actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase))
            {
                Log("Чексумма файла не совпадает!");
                throw new InvalidOperationException("Чексумма файла не совпадает с ожидаемой.");
            }
        }

        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}