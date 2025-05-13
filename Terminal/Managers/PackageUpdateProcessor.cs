using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Terminal.Static;

namespace Terminal.Managers
{
    // Класс для периодической проверки обновлений пакетов.
    // Теперь он самостоятельно получает текущую версию для известных библиотек.
    public class PackageUpdateProcessor
    {
        private readonly string[] _packageIds;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly HttpClient _httpClient = new HttpClient();

        public PackageUpdateProcessor(string[] packageIds)
        {
            _packageIds = packageIds;
        }

        /// <summary>
        /// Асинхронно запускает проверку обновлений: первый запуск происходит сразу, затем – раз в сутки.
        /// </summary>
        public async Task StartAsync()
        {
            Console.WriteLine($"[{DateTime.Now}] Запуск проверки обновлений пакетов...");
            // Первый запуск – немедленно
            await CheckUpdatesAsync();

            // Затем цикл с задержкой 24 часа между проверками.
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromDays(1), _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                if (!_cts.Token.IsCancellationRequested)
                {
                    await CheckUpdatesAsync();
                }
            }
        }

        /// <summary>
        /// Останавливает цикл проверки обновлений.
        /// </summary>
        public void Stop()
        {
            _cts.Cancel();
            Console.WriteLine("Проверка обновлений остановлена.");
        }

        /// <summary>
        /// Для каждого пакета получает последнюю опубликованную версию с NuGet,
        /// получает текущую версию (определяемую внутри класса),
        /// сравнивает их и выводит результат.
        /// </summary>
        public async Task CheckUpdatesAsync()
        {
            Console.WriteLine($"[{DateTime.Now}] Начало проверки обновлений пакетов...");
            foreach (var packageId in _packageIds)
            {
                try
                {
                    // Получаем последнюю опубликованную версию пакета с NuGet.
                    string latestVersion = await GetLatestVersion(packageId);

                    // Получаем текущую (установленную) версию посредством внутреннего метода.
                    string currentVersion = GetCurrentVersion(packageId);

                    Console.WriteLine($"Пакет {packageId}: текущая версия: {currentVersion}, " +
                                      $"последняя версия на NuGet: {latestVersion}");

                    int cmp = CompareVersion(latestVersion, currentVersion);
                    if (cmp > 0)
                    {
                        Console.WriteLine($"Обнаружена новая версия пакета {packageId}!");
                    }
                    else if (cmp == 0)
                    {
                        Console.WriteLine($"Версия пакета {packageId} актуальна.");
                    }
                    else // в редких случаях более новая версия установлена.
                    {
                        Console.WriteLine($"Установлена более новая версия пакета {packageId}, чем опубликованная на NuGet.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при проверке пакета {packageId}: {ex.Message}");
                }
            }
            Console.WriteLine($"[{DateTime.Now}] Проверка обновлений завершена.\n");
        }

        /// <summary>
        /// Внутренний метод для получения текущей версии пакета по его идентификатору.
        /// Для известных пакетов используется рефлексия через AssemblyVersionProvider.
        /// Если пакет не известен, возвращается "unknown".
        /// </summary>
        private string GetCurrentVersion(string packageId)
        {
            switch (packageId)
            {
                case "Binance.Net":
                    // Получаем версию сборки по типу из Binance.Net.
                    return AssemblyVersionProvider.GetVersion<Binance.Net.Clients.BinanceRestClient>();
                case "CryptoExchange.Net":
                    // Получаем версию сборки по типу из CryptoExchange.Net.
                    return AssemblyVersionProvider.GetVersion<CryptoExchange.Net.Authentication.ApiCredentials>();
                default:
                    return "unknown";
            }
        }

        /// <summary>
        /// Получает последнюю версию указанного пакета, запрашивая регистрационный индекс NuGet.
        /// </summary>
        public async Task<string> GetLatestVersion(string packageId)
        {
            // NuGet API требует идентификатор пакета в нижнем регистре.
            string lowerPackageId = packageId.ToLowerInvariant();
            string url = $"https://api.nuget.org/v3/registration5-semver1/{lowerPackageId}/index.json";

            using (var response = await _httpClient.GetAsync(url, _cts.Token))
            {
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var registration = JsonSerializer.Deserialize<RegistrationIndex>(json, options);

                if (registration?.Items == null || registration.Items.Count == 0)
                    return "unknown";

                string maxVersion = null;
                // Проходим по каждой странице.
                // Если у страницы есть вложенные элементы (leaf) – сравниваем их версии.
                // Если их нет – используем значение свойства Upper.
                foreach (var page in registration.Items)
                {
                    if (page.Items != null && page.Items.Count > 0)
                    {
                        foreach (var leaf in page.Items)
                        {
                            string ver = leaf?.CatalogEntry?.Version;
                            if (string.IsNullOrWhiteSpace(ver))
                                continue;
                            if (maxVersion == null || CompareVersion(ver, maxVersion) > 0)
                            {
                                maxVersion = ver;
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(page.Upper))
                    {
                        if (maxVersion == null || CompareVersion(page.Upper, maxVersion) > 0)
                        {
                            maxVersion = page.Upper;
                        }
                    }
                }
                return maxVersion ?? "unknown";
            }
        }

        /// <summary>
        /// Сравнивает две версии. Если версии можно распарсить через System.Version – используется сравнение версий,
        /// иначе выполняется строковое сравнение (без учёта регистра).
        /// </summary>
        public static int CompareVersion(string v1, string v2)
        {
            if (Version.TryParse(v1, out var ver1) && Version.TryParse(v2, out var ver2))
            {
                return ver1.CompareTo(ver2);
            }
            return string.Compare(v1, v2, StringComparison.OrdinalIgnoreCase);
        }

        #region Классы для десериализации JSON NuGet
        public class RegistrationIndex
        {
            public List<RegistrationPage> Items { get; set; }
        }

        public class RegistrationPage
        {
            // Если leaf-элементы отсутствуют, версии могут храниться здесь:
            public string Lower { get; set; }
            public string Upper { get; set; }
            // Иногда страница содержит листовые элементы.
            public List<RegistrationLeaf> Items { get; set; }
        }

        public class RegistrationLeaf
        {
            public CatalogEntry CatalogEntry { get; set; }
        }

        public class CatalogEntry
        {
            public string Version { get; set; }
        }
        #endregion
    }
}
