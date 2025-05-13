using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using System;
using Terminal.Enums;

namespace Terminal.Managers
{
    public class BinanceManager : IDisposable
    {
        private readonly BinanceSocketClient _socketClient;
        private readonly ApiCredentials _apiCredentials;

        public BinanceHistoryFilesProcessor HistoryFilesProcessor { get; }
        public BinanceRestProcessor RestProcessor { get; }
        public BinanceSocketProcessor SocketProcessor { get; }
        public BinanceTradingProcessor TradingProcessor { get; }
        public BinanceUserStreamProcessor UserStreamProcessor { get; }

        public BinanceManager(string apiKey, string apiSecret)
        {
            // Создаем общие ApiCredentials
            _apiCredentials = new ApiCredentials(apiKey, apiSecret);

            // Создаем общий экземпляр BinanceSocketClient
            _socketClient = new BinanceSocketClient();
            BinanceSocketClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = _apiCredentials;
            });

            // Инициализируем процессоры
            HistoryFilesProcessor = new BinanceHistoryFilesProcessor(BinanceMarketType.Futures);
            RestProcessor = new BinanceRestProcessor(BinanceMarketType.Futures); // По умолчанию Futures
            //SocketProcessor = new BinanceSocketProcessor(_socketClient, BinanceMarketType.Futures); // По умолчанию Futures
            //TradingProcessor = new BinanceTradingProcessor(_socketClient);
            //UserStreamProcessor = new BinanceUserStreamProcessor(_socketClient);
        }

        public void Dispose()
        {
            _socketClient?.Dispose();
            RestProcessor?.Dispose();
            SocketProcessor?.Dispose();
            UserStreamProcessor?.Dispose();
        }
    }
}