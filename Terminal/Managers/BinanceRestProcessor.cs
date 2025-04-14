using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Options;
using CryptoExchange.Net.Authentication;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Enums;

namespace Terminal.Managers
{
    public class BinanceRestProcessor : IDisposable
    {
        private readonly BinanceRestClient _binanceRestClient;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Timer _pingTimer;
        private BinanceMarketType _marketType;

        public BinanceMarketType MarketType
        {
            get => _marketType;
            set
            {
                if (_marketType != value)
                {
                    Console.WriteLine($"Изменение MarketType с {_marketType} на {value}...");
                    _marketType = value;
                }
            }
        }

        public BinanceRestProcessor(BinanceMarketType marketType)
        {
            _marketType = marketType;

            // Устанавливаем глобальные настройки для BinanceRestClient
            BinanceRestClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = new ApiCredentials("APIKEY", "APISECRET"); // Укажите свои API-ключи
            });

            // Инициализация клиента BinanceRestClient
            _binanceRestClient = new BinanceRestClient();

            // Создаем CancellationTokenSource для управления отменой операций
            _cancellationTokenSource = new CancellationTokenSource();

            // Настраиваем таймер для периодического Ping
            _pingTimer = new Timer(async _ => await PingServerAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        // Метод для выполнения Ping
        private async Task PingServerAsync()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            cancellationToken.ThrowIfCancellationRequested();

            var result = _marketType == BinanceMarketType.Futures
                ? await _binanceRestClient.UsdFuturesApi.ExchangeData.PingAsync(ct: cancellationToken)
                : await _binanceRestClient.SpotApi.ExchangeData.PingAsync(ct: cancellationToken);

            if (result.Success)
            {
                Console.WriteLine($"Ping успешен: {DateTime.UtcNow}");
            }
            else
            {
                Console.WriteLine($"Ошибка Ping: {result.Error?.Message}");
            }
        }

        // Метод для получения свечей с выбором таймфрейма и диапазона дат
        public async Task<List<object>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startTime, DateTime endTime)
        {
            var allKlines = new List<object>();
            DateTime currentStartTime = startTime;
            var cancellationToken = _cancellationTokenSource.Token;

            while (currentStartTime < endTime)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = _marketType == BinanceMarketType.Futures
                    ? await _binanceRestClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(
                        symbol: symbol,
                        interval: interval,
                        startTime: currentStartTime,
                        endTime: endTime,
                        limit: 1000,
                        ct: cancellationToken)
                    : await _binanceRestClient.SpotApi.ExchangeData.GetKlinesAsync(
                        symbol: symbol,
                        interval: interval,
                        startTime: currentStartTime,
                        endTime: endTime,
                        limit: 1000,
                        ct: cancellationToken);

                if (!result.Success)
                {
                    throw new Exception($"Ошибка при запросе данных свечей: {result.Error?.Message}");
                }

                // Добавляем данные в общий список
                allKlines.AddRange(result.Data);

                if (result.Data.Count() < 1000)
                {
                    break;
                }

                currentStartTime = result.Data.Last().CloseTime.AddMilliseconds(1);
            }

            return allKlines;
        }

        // Метод для отмены всех операций
        public void CancelOperations()
        {
            _cancellationTokenSource.Cancel();
            Console.WriteLine("Операции отменены.");
        }

        // Метод для завершения работы клиента
        public void Dispose()
        {
            _pingTimer?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _binanceRestClient?.Dispose();
        }
    }
}