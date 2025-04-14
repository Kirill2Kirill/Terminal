using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Enums;

namespace Terminal.Managers
{
    public class BinanceSocketProcessor : IDisposable
    {
        private readonly BinanceSocketClient _binanceSocketClient;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Timer _pingTimer;
        private KlineInterval _lastInterval;
        private string _lastSymbol;


        private BinanceMarketType _marketType;
        private UpdateSubscription _subscription;

        public BinanceMarketType MarketType => _marketType; // Только для чтения

        public event Action<object> OnKlineUpdated; // Событие для передачи обновленных свечей

        public BinanceSocketProcessor(BinanceMarketType marketType)
        {
            _marketType = marketType;

            // Устанавливаем глобальные настройки для BinanceSocketClient
            BinanceSocketClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = new ApiCredentials("APIKEY", "APISECRET"); // Укажите свои API-ключи
            });

            // Инициализация клиента BinanceSocketClient
            _binanceSocketClient = new BinanceSocketClient();

            //_binanceSocketClient.SpotApi.Account.StartUserStreamAsync( r => r.);
            !!!!!!!!!!!!!!!!!!!!_binanceSocketClient.GetSubscriptionsState();

            _binanceSocketClient.UsdFuturesApi.Trading.

            // Создаем CancellationTokenSource для управления отменой операций
            _cancellationTokenSource = new CancellationTokenSource();

            // Настраиваем таймер для периодического Ping
            //_pingTimer = new Timer(async _ => await PingServerAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        // Асинхронный метод для изменения MarketType
        public async Task SetMarketTypeAsync(BinanceMarketType newMarketType)
        {
            if (_marketType != newMarketType)
            {
                Console.WriteLine($"Изменение MarketType с {_marketType} на {newMarketType}...");
                _marketType = newMarketType;

                // Выполняем переподключение
                await ReconnectAsync();
            }
        }

        // Метод для выполнения Ping
        private async Task PingServerAsync()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            cancellationToken.ThrowIfCancellationRequested();

            var result = await _binanceSocketClient.SpotApi.ExchangeData.PingAsync(ct: cancellationToken);

            if (result.Success)
            {
                Console.WriteLine($"Ping успешен (SpotApi): {DateTime.UtcNow}");
            }
            else
            {
                Console.WriteLine($"Ошибка Ping (SpotApi): {result.Error?.Message}");
            }
        }

        // Метод для подписки на обновления свечей
        public async Task SubscribeToKlinesAsync(string symbol, KlineInterval interval)
        {
            var cancellationToken = _cancellationTokenSource.Token;

            //// Сохраняем параметры подписки
            _lastSymbol = symbol;
            _lastInterval = interval;

            // Отменяем предыдущую подписку, если она существует
            if (_subscription != null)
            {
                Console.WriteLine("Отмена предыдущей подписки...");
                await _binanceSocketClient.UnsubscribeAsync(_subscription);
                _subscription = null;
            }

            // Подписываемся на обновления свечей
            CallResult<UpdateSubscription> subscriptionResult;

            if (_marketType == BinanceMarketType.Futures)
            {
                subscriptionResult = await _binanceSocketClient.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync(
                    symbol,
                    interval,
                    data => HandleKlineUpdate(data.Data, cancellationToken),
                    ct: cancellationToken
                );
            }
            else
            {
                subscriptionResult = await _binanceSocketClient.SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(
                    symbol,
                    interval,
                    data => HandleKlineUpdate(data.Data, cancellationToken),
                    cancellationToken
                );
            }

            if (!subscriptionResult.Success)
            {
                throw new Exception($"Ошибка подписки на обновления свечей: {subscriptionResult.Error?.Message}");
            }

            _subscription = subscriptionResult.Data;

            // Настраиваем обработку событий подписки
            HandleSubscriptionEvents(_subscription);

            Console.WriteLine($"Подписка на обновления свечей для {symbol} с интервалом {interval} успешно выполнена.");
        }

        // Метод для обработки обновлений свечей
        private void HandleKlineUpdate<T>(T data, CancellationToken cancellationToken) where T : IBinanceStreamKlineData
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Получена новая свеча: Open: {data.Data.OpenTime}, Close: {data.Data.CloseTime}");
            OnKlineUpdated?.Invoke(data);
        }

        //Метод для обработки событий и методов UpdateSubscription
        private void HandleSubscriptionEvents(UpdateSubscription subscription)
        {
            if (subscription == null)
            {
                Console.WriteLine("Подписка отсутствует.");
                return;
            }

            // Обработка события потери соединения
            subscription.ConnectionLost += () =>
            {
                Console.WriteLine($"Соединение потеряно. SocketId: {subscription.SocketId}, Id: {subscription.Id}");
            };

            // Обработка события восстановления соединения
            subscription.ConnectionRestored += (time) =>
            {
                Console.WriteLine($"Соединение восстановлено в {time}. SocketId: {subscription.SocketId}, Id: {subscription.Id}");
            };

            // Обработка события закрытия соединения
            subscription.ConnectionClosed += () =>
            {
                Console.WriteLine($"Соединение закрыто. SocketId: {subscription.SocketId}, Id: {subscription.Id}");
            };

            // Обработка исключений
            subscription.Exception += (ex) =>
            {
                Console.WriteLine($"Исключение в подписке. SocketId: {subscription.SocketId}, Id: {subscription.Id}, Ошибка: {ex.Message}");
            };

            // Обработка события возобновления активности
            subscription.ActivityUnpaused += () =>
            {
                Console.WriteLine($"Активность возобновлена. SocketId: {subscription.SocketId}, Id: {subscription.Id}");
            };

            // Обработка события паузы активности
            subscription.ActivityPaused += () =>
            {
                Console.WriteLine($"Активность приостановлена. SocketId: {subscription.SocketId}, Id: {subscription.Id}");
            };

            // Вывод информации о подписке
            Console.WriteLine($"Подписка настроена. SocketId: {subscription.SocketId}, Id: {subscription.Id}");
        }
        private async Task ReconnectAsync()
        {
            Console.WriteLine("Переподключение...");
            //await _binanceSocketClient.ReconnectAsync();

            // Используем встроенный метод ReconnectAsync для переподключения
            //var reconnectResult = await _binanceSocketClient.ReconnectAsync();

            //if (!reconnectResult.Success)
            //{
            //    throw new Exception($"Ошибка переподключения: {reconnectResult.Error?.Message}");
            //}

            //Console.WriteLine("Переподключение выполнено успешно.");

            // Восстанавливаем подписку, если параметры известны
            if (!string.IsNullOrEmpty(_lastSymbol) && _lastInterval != null)
            {
                Console.WriteLine($"Восстановление подписки для {_lastSymbol} с интервалом {_lastInterval}...");
                await SubscribeToKlinesAsync(_lastSymbol, _lastInterval);
            }
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
            _binanceSocketClient?.Dispose();
        }
    }
}