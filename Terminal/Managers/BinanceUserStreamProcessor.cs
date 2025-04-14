using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Terminal.Managers
{
    public class BinanceUserStreamProcessor : IDisposable
    {
        private readonly BinanceSocketClient _binanceSocketClient;
        private readonly BinanceRestClient _binanceRestClient;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private UpdateSubscription _subscription;
        private string _listenKey;
        private readonly Timer _listenKeyTimer;

        public event Action<BinanceStreamOrderUpdate> OnOrderUpdate;
        public event Action<BinanceStreamOrderList> OnOcoOrderUpdate;
        public event Action<BinanceStreamPositionsUpdate> OnAccountPositionUpdate;
        public event Action<BinanceStreamBalanceUpdate> OnAccountBalanceUpdate;
        public event Action<BinanceStreamBalanceLockUpdate> OnBalanceLockUpdate;
        public event Action OnUserStreamTerminated;
        public event Action OnListenKeyExpired; 

        public BinanceUserStreamProcessor(string apiKey, string apiSecret)
        {
            // Устанавливаем глобальные настройки для BinanceSocketClient и BinanceRestClient
            BinanceSocketClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
            });

            BinanceRestClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
            });

            _binanceSocketClient = new BinanceSocketClient();
            _binanceRestClient = new BinanceRestClient();
            _cancellationTokenSource = new CancellationTokenSource();

            // Таймер для обновления ListenKey каждые 30 минут
            _listenKeyTimer = new Timer(async _ => await KeepAliveListenKeyAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        }

        // Метод для запуска пользовательского потока
        public async Task StartUserStreamAsync()
        {
            // Получаем ListenKey
            var listenKeyResult = await _binanceRestClient.SpotApi.Account.StartUserStreamAsync();
            if (!listenKeyResult.Success)
            {
                throw new Exception($"Ошибка получения ListenKey: {listenKeyResult.Error?.Message}");
            }

            _listenKey = listenKeyResult.Data;
            Console.WriteLine($"ListenKey получен: {_listenKey}");

            // Подписываемся на пользовательский поток
            var subscriptionResult = await _binanceSocketClient.SpotApi.Account.SubscribeToUserDataUpdatesAsync(
                _listenKey,
                onOrderUpdateMessage: data =>
                {
                    Console.WriteLine($"Обновление ордера: {data.Data}");
                    OnOrderUpdate?.Invoke(data.Data);
                },
                onOcoOrderUpdateMessage: data =>
                {
                    Console.WriteLine($"Обновление OCO-ордера: {data.Data}");
                    OnOcoOrderUpdate?.Invoke(data.Data);
                },
                onAccountPositionMessage: data =>
                {
                    Console.WriteLine($"Обновление позиций: {data.Data}");
                    OnAccountPositionUpdate?.Invoke(data.Data);
                },
                onAccountBalanceUpdate: data =>
                {
                    Console.WriteLine($"Обновление баланса: {data.Data}");
                    OnAccountBalanceUpdate?.Invoke(data.Data);
                },                
                onListenKeyExpired: data =>
                {
                    Console.WriteLine("ListenKey истек.");
                    OnListenKeyExpired?.Invoke();
                },
                onUserDataStreamTerminated: data =>
                {
                    Console.WriteLine("Пользовательский поток завершен.");
                    OnUserStreamTerminated?.Invoke();
                },
                onBalanceLockUpdate: data =>
                {
                    Console.WriteLine($"Обновление заблокированного баланса: {data.Data}");
                    OnBalanceLockUpdate?.Invoke(data.Data);
                },
                ct: _cancellationTokenSource.Token
            );

            if (!subscriptionResult.Success)
            {
                throw new Exception($"Ошибка подписки на пользовательский поток: {subscriptionResult.Error?.Message}");
            }

            _subscription = subscriptionResult.Data;
            Console.WriteLine("Подписка на пользовательский поток успешно выполнена.");
        }

        // Метод для обновления ListenKey
        private async Task KeepAliveListenKeyAsync()
        {
            if (string.IsNullOrEmpty(_listenKey))
            {
                Console.WriteLine("ListenKey отсутствует. Пропуск обновления.");
                return;
            }

            var keepAliveResult = await _binanceRestClient.SpotApi.Account.KeepAliveUserStreamAsync(_listenKey);
            if (!keepAliveResult.Success)
            {
                Console.WriteLine($"Ошибка обновления ListenKey: {keepAliveResult.Error?.Message}");
            }
            else
            {
                Console.WriteLine("ListenKey успешно обновлен.");
            }
        }

        // Метод для остановки пользовательского потока
        public async Task StopUserStreamAsync()
        {
            if (_subscription != null)
            {
                await _binanceSocketClient.UnsubscribeAsync(_subscription);
                _subscription = null;
                Console.WriteLine("Подписка на пользовательский поток отменена.");
            }

            if (!string.IsNullOrEmpty(_listenKey))
            {
                var closeResult = await _binanceRestClient.SpotApi.Account.StopUserStreamAsync(_listenKey);
                if (!closeResult.Success)
                {
                    Console.WriteLine($"Ошибка закрытия ListenKey: {closeResult.Error?.Message}");
                }
                else
                {
                    Console.WriteLine("ListenKey успешно закрыт.");
                }

                _listenKey = null;
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
            _listenKeyTimer?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _binanceSocketClient?.Dispose();
            _binanceRestClient?.Dispose();
        }
    }
}